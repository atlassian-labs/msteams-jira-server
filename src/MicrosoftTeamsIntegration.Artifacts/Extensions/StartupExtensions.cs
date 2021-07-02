using System;
using System.Net;
using JetBrains.Annotations;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.ApplicationInsights;
using Microsoft.Bot.Builder.BotFramework;
using Microsoft.Bot.Builder.Integration.ApplicationInsights.Core;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MicrosoftTeamsIntegration.Artifacts.Bots;
using MicrosoftTeamsIntegration.Artifacts.Bots.DialogRouter;
using MicrosoftTeamsIntegration.Artifacts.Infrastructure.Telemetry;
using MicrosoftTeamsIntegration.Artifacts.Services;
using MicrosoftTeamsIntegration.Artifacts.Services.GraphApi;
using MicrosoftTeamsIntegration.Artifacts.Services.Interfaces;
using MicrosoftTeamsIntegration.Artifacts.Services.Interfaces.Refit;
using Polly;
using Polly.Contrib.WaitAndRetry;
using Polly.Extensions.Http;
using Refit;

namespace MicrosoftTeamsIntegration.Artifacts.Extensions
{
    [PublicAPI]
    public static class StartupExtensions
    {
        public static IServiceCollection AddTeamsIntegrationDefaultServices(this IServiceCollection services, IConfiguration configuration)
        {
            var delay = Backoff.DecorrelatedJitterBackoffV2(TimeSpan.FromSeconds(1), 5);
            services.AddHttpClient(nameof(TeamsBotHttpAdapter))
                .AddPolicyHandler(message => HttpPolicyExtensions
                                                     .HandleTransientHttpError()
                                                     .OrResult(msg => msg.StatusCode == (HttpStatusCode)429) // TooManyRequests
                                                     .WaitAndRetryAsync(delay));

            // Configure credentials
            services.AddSingleton<ICredentialProvider, ConfigurationCredentialProvider>();
            services.AddSingleton(new MicrosoftAppCredentials(
                configuration.GetValue<string>("MicrosoftAppId"),
                configuration.GetValue<string>("MicrosoftAppPassword")));

            // Configure telemetry
            services.AddApplicationInsightsTelemetry(configuration);
            services.AddApplicationInsightsTelemetryProcessor<CustomTelemetryProcessor>();

            services.AddSingleton<IBotTelemetryClient, BotTelemetryClient>();

            // Add telemetry initializer that will set the correlation context for all telemetry items.
            services.AddSingleton<ITelemetryInitializer, OperationCorrelationTelemetryInitializer>();

            // Add telemetry initializer that sets the user ID and session ID (in addition to other bot-specific properties such as activity ID)
            services.AddSingleton<ITelemetryInitializer, TelemetryBotIdInitializer>();

            // Create the telemetry middleware to initialize telemetry gathering
            services.AddSingleton<TelemetryInitializerMiddleware>();

            // Create the telemetry middleware (used by the telemetry initializer) to track conversation events
            services.AddSingleton<TelemetryLoggerMiddleware>();

            // Configure adapters
            services.AddSingleton<IBotFrameworkHttpAdapter, TeamsBotHttpAdapter>();

            // Configure storage
            services.AddSingleton<IStorage, MemoryStorage>();
            services.AddSingleton<UserState>();
            services.AddSingleton<ConversationState>();

            // Create the debug middleware
            services.AddSingleton<InspectionMiddleware>();
            services.AddSingleton<InspectionState>();

            // Register service to send proactive messages to users
            services.AddSingleton<IProactiveMessagesService, ProactiveMessagesService>();

            // Configure Graph API
            services.AddSingleton<IGraphApiService, GraphApiService>();
            services.AddSingleton<IGraphSdkHelper, GraphSdkHelper>();

            // Configure service for getting AAD access token
            services.AddRefitClient<IOAuthV2Service>()
                .ConfigureHttpClient(c => c.BaseAddress = new Uri("https://login.microsoftonline.com"));

            return services;
        }

        public static IServiceCollection AddDialogRouter(this IServiceCollection services, params DialogRoute[] dialogRoutes)
        {
            foreach (var dialogRoute in dialogRoutes)
            {
                services.AddTransient(dialogRoute.DialogType);
            }

            services.AddTransient<IDialogRouteService>(sp => new DialogRouteService(sp, dialogRoutes));
            return services;
        }

        public static IApplicationBuilder UseTeamsIntegrationDefaultHealthCheck(this IApplicationBuilder app)
        {
            // health check
            return app.Use((context, next) => context.Request.Path.StartsWithSegments("/hc", StringComparison.OrdinalIgnoreCase)
                ? context.Response.WriteAsync(string.Empty)
                : next());
        }
    }
}
