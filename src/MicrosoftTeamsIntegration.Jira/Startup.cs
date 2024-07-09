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
using MicrosoftTeamsIntegration.Jira.Services;
using MicrosoftTeamsIntegration.Jira.Services.Interfaces;
using MicrosoftTeamsIntegration.Jira.Services.SignalR;
using MicrosoftTeamsIntegration.Jira.Services.SignalR.Interfaces;
using MicrosoftTeamsIntegration.Jira.Settings;
using Newtonsoft.Json;
using Polly.Contrib.WaitAndRetry;
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

            var delay = Backoff.DecorrelatedJitterBackoffV2(TimeSpan.FromSeconds(1), 5);

            services.AddSingleton<IDatabaseService, DatabaseService>();

            services.AddTransient<IMessagingExtensionService, MessagingExtensionService>();
            services.AddTransient<IActionableMessageService, ActionableMessageService>();
            services.AddTransient<IBotMessagesService, BotMessagesService>();
            services.AddTransient<IActivityFeedSenderService, ActivityFeedSenderService>();
            services.AddSingleton<IDistributedCacheService, DistributedCacheService>();
            services.AddSingleton<IMongoDBContext, MongoDBContext>();
            services.AddSingleton<IUserTokenService, UserTokenService>();
            services.AddSingleton<ICommandDialogReferenceService, CommandDialogReferenceService>();
            services.AddSingleton<IBotFrameworkAdapterService, BotFrameworkAdapterService>();

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
            services
                .AddSignalR(options =>
                {
                    options.EnableDetailedErrors = true;
                    options.MaximumReceiveMessageSize = appSettings.JiraServerMaximumReceiveMessageSize;
                })
                .AddAzureSignalR(_configuration["Azure:SignalR:ConnectionString"])
                .AddNewtonsoftJsonProtocol(options =>
                {
                    options.PayloadSerializerSettings.NullValueHandling = NullValueHandling.Ignore;
                    options.PayloadSerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                });

            // Auto Mapper Configurations
            var mappingConfig = new MapperConfiguration(mc => { mc.AddProfile(new JiraMappingProfile(appSettings)); });
            var mapper = mappingConfig.CreateMapper();
            services.AddSingleton(mapper);

            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.Unspecified;
                options.OnAppendCookie = cookieContext =>
                    CheckSameSite(cookieContext.Context, cookieContext.CookieOptions);
                options.OnDeleteCookie = cookieContext =>
                    CheckSameSite(cookieContext.Context, cookieContext.CookieOptions);
            });

            if (!_env.IsDevelopment())
            {
                IStorage dataStore = new BlobsStorage(appSettings.StorageConnectionString, appSettings.BotDataStoreContainer);
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

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHttpsRedirection();
                app.UseHsts();
            }

            app.UseExceptionHandler(appError =>
            {
                appError.Run(async context =>
                {
                    var contextFeature = context.Features.Get<IExceptionHandlerFeature>();
                    if (contextFeature is null)
                    {
                        return;
                    }

                    var exception = contextFeature.Error;
                    var apiError = new ApiError("An unhandled error occurred. We are working on it!");
                    switch (exception)
                    {
                        case ApiException ex:
                            // handle explicit 'known' API errors
                            apiError = new ApiError(ex.Content ?? ex.Message);
                            context.Response.StatusCode = (int)ex.StatusCode;
                            break;
                        case UnauthorizedException e:
                            apiError = new ApiError(e.Message);
                            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                            break;
                        case MethodAccessException e:
                            // handle explicit 'known' errors for invalid_scope
                            apiError = new ApiError(e.Message);
                            context.Response.StatusCode = (int)HttpStatusCode.UpgradeRequired;
                            break;
                        case InvalidOperationException invalidOperation:
                            // handle customers that configured their webhooks posting to Jira Data Center instead of Connector
                            if (context.Request.Method.Equals("post", StringComparison.OrdinalIgnoreCase) && invalidOperation.Message.Contains("index.html"))
                            {
                                apiError = new ApiError("You are sending request to an incorrect route. Please check your configuration.");
                                context.Response.StatusCode = (int)HttpStatusCode.OK;
                            }
                            else
                            {
                                apiError = new ApiError("Invalid operation performed.");
                                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                            }

                            break;
                        case BadRequestException e:
                            apiError = new ApiError(e.Message);
                            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                            break;
                        default:
                            {
                                // Unhandled errors
                                if (env.IsDevelopment())
                                {
                                    apiError = new ApiError(exception.ToString());
                                }

                                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

                                logger.LogCritical(exception, exception.Message);
                                break;
                            }
                    }

                    // return a JSON result
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync(JsonConvert.SerializeObject(apiError));
                });
            });

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
                        .Self();

                    frameAncestors
                        .From("*.jira.com")
                        .From("*.atlassian.net")
                        .From("teams.microsoft.com")
                        .From("*.teams.microsoft.com")
                        .From("*.teams.microsoft.us")
                        .From("*.skype.com")
                        .From("*.msteams-atlassian.com")
                        .From("*.office.com");

                    if (!string.IsNullOrEmpty(appSettings.CspValidDomains))
                    {
                        var validDomains = appSettings.CspValidDomains.Split();
                        foreach (var domain in validDomains)
                        {
                            frameAncestors
                                .From(domain.Trim());
                        }
                    }
                });

            app.UseSecurityHeaders(policyCollection);

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

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHub<GatewayHub>("/JiraGateway");
            });

            app.UseSpa(spa =>
            {
                spa.Options.SourcePath = "ClientApp";

                if (env.IsDevelopment() && !_configuration.GetValue<bool>("DisableAngularCliServer"))
                {
                    spa.UseAngularCliServer(npmScript: "start");
                }
            });
        }

        private void CheckSameSite(HttpContext httpContext, CookieOptions options)
        {
            if (options.SameSite == Microsoft.AspNetCore.Http.SameSiteMode.None)
            {
                var userAgent = httpContext.Request.Headers["User-Agent"].ToString();
                if (AgentDetectionHelper.DisallowsSameSiteNone(userAgent))
                {
                    options.SameSite = SameSiteMode.Unspecified;
                }
            }
        }
    }
}
