using System;
using System.Net;
using AutoMapper;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SpaServices.AngularCli;
using Microsoft.Azure.SignalR;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Azure.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using MicrosoftTeamsIntegration.Artifacts.Extensions;
using MicrosoftTeamsIntegration.Artifacts.Infrastructure;
using MicrosoftTeamsIntegration.Artifacts.Models;
using MicrosoftTeamsIntegration.Artifacts.Services;
using MicrosoftTeamsIntegration.Artifacts.Services.Interfaces;
using MicrosoftTeamsIntegration.Jira.Exceptions;
using MicrosoftTeamsIntegration.Jira.Helpers;
using MicrosoftTeamsIntegration.Jira.Jobs;
using MicrosoftTeamsIntegration.Jira.Services;
using MicrosoftTeamsIntegration.Jira.Services.Interfaces;
using MicrosoftTeamsIntegration.Jira.Services.SignalR;
using MicrosoftTeamsIntegration.Jira.Services.SignalR.Interfaces;
using MicrosoftTeamsIntegration.Jira.Settings;
using Newtonsoft.Json;
using Polly.Contrib.WaitAndRetry;
using Quartz;
using Refit;

namespace MicrosoftTeamsIntegration.Jira
{
    public class Startup
    {
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _env;

        public Startup(IConfiguration configuration, IWebHostEnvironment env)
        {
            _configuration = configuration;
            _env = env;
            CurrentDirectoryHelpers.SetCurrentDirectory();
        }

        [UsedImplicitly]
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<AppSettings>(_configuration);
            var appSettings = _configuration.Get<AppSettings>();

            services.AddTeamsIntegrationDefaultServices(_configuration);

            services.Configure<ClientAppOptions>(_configuration.GetSection("ClientAppOptions"));

            Backoff.DecorrelatedJitterBackoffV2(TimeSpan.FromSeconds(1), 5);

            services.AddSingleton<IDatabaseService, DatabaseService>();
            services.AddSingleton<INotificationsDatabaseService, NotificationDatabaseService>();

            services.AddTransient<IMessagingExtensionService, MessagingExtensionService>();
            services.AddTransient<IActionableMessageService, ActionableMessageService>();
            services.AddTransient<IBotMessagesService, BotMessagesService>();
            services.AddTransient<IActivityFeedSenderService, ActivityFeedSenderService>();
            services.AddSingleton<IDistributedCacheService, DistributedCacheService>();
            services.AddSingleton<IMongoDBContext, MongoDBContext>();
            services.AddSingleton<IUserTokenService, UserTokenService>();
            services.AddSingleton<ICommandDialogReferenceService, CommandDialogReferenceService>();
            services.AddSingleton<IBotFrameworkAdapterService, BotFrameworkAdapterService>();
            services.AddSingleton<IAnalyticsService, AnalyticsService>();
            services.AddSingleton<IUserService, UserService>();
            services.AddSingleton<INotificationProcessorService, NotificationProcessorService>();
            services.AddSingleton<INotificationQueueService, NotificationQueueService>();

            // This can be removed after https://github.com/aspnet/IISIntegration/issues/371
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer("Bearer API", options =>
                {
                    options.Audience = appSettings.MicrosoftAppId;
                    options.Authority = $"{appSettings.MicrosoftLoginBaseUrl}/organizations/v2.0/";
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = false // as we support multi-tenant
                    };
                });

            if (_env.IsDevelopment())
            {
                services.AddDistributedMemoryCache();
            }
            else
            {
                services.AddStackExchangeRedisCache(options =>
                {
                    options.Configuration = appSettings.CacheConnectionString;
                });
            }

            services.AddTransient<IJiraAuthService, JiraAuthService>();
            services.AddSingleton<ISignalRService, SignalRService>();
            services.AddTransient<IJiraService, JiraService>();
            services.AddTransient<INotificationSubscriptionService, NotificationSubscriptionService>();
            services
                .AddSignalR(options =>
                {
                    options.EnableDetailedErrors = true;
                    options.MaximumReceiveMessageSize = appSettings.JiraServerMaximumReceiveMessageSize;
                })
                .AddAzureSignalR(options =>
                {
                    options.Endpoints =
                    [

                        // Add additional endpoints here if needed, i.e.
                        // new ServiceEndpoint(_configuration["Azure:SignalR:ConnectionString:EU"]),
                        new ServiceEndpoint(_configuration["Azure:SignalR:ConnectionString"])
                    ];
                })
                .AddNewtonsoftJsonProtocol(options =>
                {
                    options.PayloadSerializerSettings.NullValueHandling = NullValueHandling.Ignore;
                    options.PayloadSerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                });

            // Add Redis backplane for redis only for non-development environments
            if (!_env.IsDevelopment())
            {
                services.AddStackExchangeRedisCache(options =>
                {
                    options.Configuration = appSettings.CacheConnectionString;
                });
            }

            services.AddQuartz(q =>
            {
                var jobKey = new JobKey("NotificationJob");
                q.AddJob<NotificationJob>(options => options.WithIdentity(jobKey));
                q.AddTrigger(options => options
                    .ForJob(jobKey)
                    .WithIdentity("NotificationJob-trigger")
                    .WithCronSchedule(appSettings.NotificationJobSchedule));
            });
            services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);

            // Auto Mapper Configurations
            var mappingConfig = new MapperConfiguration(mc => { mc.AddProfile(new JiraMappingProfile(appSettings)); });
            var mapper = mappingConfig.CreateMapper();
            services.AddSingleton(mapper);

            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = _ => true;
                options.MinimumSameSitePolicy = SameSiteMode.Unspecified;
                options.OnAppendCookie = cookieContext =>
                    CheckSameSite(cookieContext.Context, cookieContext.CookieOptions);
                options.OnDeleteCookie = cookieContext =>
                    CheckSameSite(cookieContext.Context, cookieContext.CookieOptions);
            });

            if (!_env.IsDevelopment())
            {
                IStorage dataStore =
                    new BlobsStorage(appSettings.StorageConnectionString, appSettings.BotDataStoreContainer);
                services.AddSingleton(dataStore);
            }

            services.AddSingleton<JiraBotAccessors>();

            // Register bot
            services.AddTransient<IBot, JiraBot>();

            services.AddControllersWithViews()
                .AddNewtonsoftJson(options =>
                {
                    options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
                    options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                });

            services.AddSpaStaticFiles(configuration =>
            {
                configuration.RootPath = "ClientApp/dist";
            });
        }

        [UsedImplicitly]
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILogger<Startup> logger)
        {
            var appSettings = _configuration.Get<AppSettings>();

            app.UseTeamsIntegrationDefaultHealthCheck();

            ConfigureEnvironment(app, env);
            ConfigureExceptionHandler(app, env, logger);
            ConfigureSecurityHeaders(app, appSettings);
            ConfigureMiddleware(app);
            ConfigureEndpoints(app);
            ConfigureSpa(app, env);
        }

        private static void ConfigureEnvironment(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHttpsRedirection();
                app.UseHsts();
            }
        }

        private static void ConfigureExceptionHandler(
            IApplicationBuilder app,
            IWebHostEnvironment env,
            ILogger<Startup> logger)
        {
            app.UseExceptionHandler(appError =>
            {
                appError.Run(async context =>
                {
                    var contextFeature = context.Features.Get<IExceptionHandlerFeature>();
                    if (contextFeature == null)
                    {
                        return;
                    }

                    var apiError = CreateApiError(contextFeature.Error, env, context);
                    context.Response.StatusCode = PickStatusCode(contextFeature.Error, context, logger);

                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync(JsonConvert.SerializeObject(apiError));
                });
            });
        }

        private static void ConfigureSecurityHeaders(IApplicationBuilder app, AppSettings appSettings)
        {
            app.UseSecurityHeaders(BuildHeaderPolicyCollection(appSettings));
        }

        private static ApiError CreateApiError(Exception exception, IWebHostEnvironment env, HttpContext context)
        {
            ArgumentNullException.ThrowIfNull(exception);

            switch (exception)
            {
                case ApiException ex:
                    return new ApiError(ex.Content ?? ex.Message);

                case UnauthorizedException e:
                    return new ApiError(e.Message);

                case MethodAccessException e:
                    return new ApiError(e.Message);

                case InvalidOperationException invalidOp:
                    return HandleInvalidOperationException(invalidOp, context);

                case BadRequestException e:
                    return new ApiError(e.Message);

                default:
                    return env.IsDevelopment()
                        ? new ApiError(exception.ToString())
                        : new ApiError("An unhandled error occurred. We are working on it!");
            }
        }

        private static int PickStatusCode(Exception exception, HttpContext context, ILogger<Startup> logger)
        {
            ArgumentNullException.ThrowIfNull(exception);

            switch (exception)
            {
                case ApiException ex:
                    return (int)ex.StatusCode;

                case UnauthorizedException _:
                    return (int)HttpStatusCode.Unauthorized;

                case MethodAccessException _:
                    return (int)HttpStatusCode.UpgradeRequired;

                case InvalidOperationException invalidOp:
                    return GetInvalidOperationStatusCode(invalidOp, context);

                case BadRequestException _:
                    return (int)HttpStatusCode.BadRequest;

                default:
                    logger.LogError(exception, "An unhandled error occurred: {ErrorMessage}", exception.Message);
                    return (int)HttpStatusCode.InternalServerError;
            }
        }

        private static ApiError HandleInvalidOperationException(InvalidOperationException exception, HttpContext context)
        {
            if (context.Request.Method.Equals("POST", StringComparison.OrdinalIgnoreCase) &&
                exception.Message.Contains("index.html"))
            {
                return new ApiError("You are sending request to an incorrect route. Please check your configuration.");
            }

            return new ApiError("Invalid operation performed.");
        }

        private static int GetInvalidOperationStatusCode(InvalidOperationException exception, HttpContext context)
        {
            if (context.Request.Method.Equals("POST", StringComparison.OrdinalIgnoreCase) &&
                exception.Message.Contains("index.html"))
            {
                return (int)HttpStatusCode.OK;
            }

            return (int)HttpStatusCode.BadRequest;
        }

        private static HeaderPolicyCollection BuildHeaderPolicyCollection(AppSettings appSettings)
        {
            var policyCollection = new HeaderPolicyCollection()
                .AddFrameOptionsSameOrigin("https://teams.microsoft.com/")
                .AddContentSecurityPolicy(builder =>
                {
                    builder.AddScriptSrc()
                        .Self()
                        .UnsafeInline()
                        .UnsafeEval()
                        .From("*.jira.com")
                        .From("*.atlassian.net")
                        .From("secure.aadcdn.microsoftonline-p.com")
                        .From("statics.teams.cdn.office.net")
                        .From("res.cdn.office.net")
                        .From("cdnjs.cloudflare.com")
                        .From("statics.teams.microsoft.com")
                        .From("connect-cdn.atl-paas.net")
                        .From("*.vo.msecnd.net"); // used for AppInsights js lib

                    builder.AddStyleSrc()
                        .Self()
                        .UnsafeInline()
                        .From("aui-cdn.atlassian.com")
                        .From("cdnjs.cloudflare.com")
                        .From("fonts.googleapis.com");

                    builder.AddImgSrc()
                        .Self()
                        .Data()
                        .UnsafeInline()
                        .From("*");

                    var frameAncestors = builder.AddFrameAncestors()
                        .Self()
                        .From("*.jira.com")
                        .From("*.atlassian.net")
                        .From("teams.microsoft.com")
                        .From("*.teams.microsoft.com")
                        .From("*.teams.microsoft.us")
                        .From("teams.cloud.microsoft")
                        .From("*.skype.com")
                        .From("*.msteams-atlassian.com")
                        .From("*.office.com");

                    if (!string.IsNullOrEmpty(appSettings.CspValidDomains))
                    {
                        var validDomains = appSettings.CspValidDomains.Split();
                        foreach (var domain in validDomains)
                        {
                            frameAncestors.From(domain.Trim());
                        }
                    }
                });
            return policyCollection;
        }

        private static void ConfigureMiddleware(IApplicationBuilder app)
        {
            app.UseStaticFiles();
            app.UseSpaStaticFiles();
            app.UseRouting();
            app.UseCookiePolicy();
            app.UseAuthentication();
            app.UseAuthorization();

            app.Use(next => context =>
            {
                context.Request.EnableBuffering();
                return next(context);
            });
        }

        private static void ConfigureEndpoints(IApplicationBuilder app)
        {
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHub<GatewayHub>("/JiraGateway");
            });
        }

        private void ConfigureSpa(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseSpa(spa =>
            {
                const int port = 4200;
                spa.Options.SourcePath = "ClientApp";
                spa.Options.DevServerPort = port;
                spa.Options.PackageManagerCommand = "npm";

                if (env.IsDevelopment() && !_configuration.GetValue<bool>("DisableAngularCliServer"))
                {
                    spa.UseAngularCliServer("start");
                    spa.UseProxyToSpaDevelopmentServer($"http://localhost:{port}");
                }
            });
        }

        private static void CheckSameSite(HttpContext httpContext, CookieOptions options)
        {
            if (options.SameSite == SameSiteMode.None)
            {
                var userAgent = httpContext.Request.Headers.UserAgent.ToString();
                if (AgentDetectionHelper.DisallowsSameSiteNone(userAgent))
                {
                    options.SameSite = SameSiteMode.Unspecified;
                }
            }
        }
    }
}
