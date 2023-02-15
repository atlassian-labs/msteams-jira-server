using System.Collections.Generic;
using System.Linq;
using FakeItEasy;
using Microsoft.ApplicationInsights.Extensibility;
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
using MicrosoftTeamsIntegration.Artifacts.Extensions;
using MicrosoftTeamsIntegration.Artifacts.Services;
using MicrosoftTeamsIntegration.Artifacts.Services.GraphApi;
using MicrosoftTeamsIntegration.Artifacts.Services.Interfaces;
using Xunit;

namespace MicrosoftTeamsIntegration.Jira.Tests.Extensions
{
    public class StartupExtensionsTests
    {
        private List<ServiceDescriptor> _serviceDescriptors;
        public StartupExtensionsTests()
        {
            _serviceDescriptors = new List<ServiceDescriptor>()
            {
                new ServiceDescriptor(typeof(BotFrameworkAuthentication), typeof(ConfigurationBotFrameworkAuthentication), ServiceLifetime.Singleton),
                new ServiceDescriptor(typeof(ITelemetryInitializer), typeof(OperationCorrelationTelemetryInitializer), ServiceLifetime.Singleton),
                new ServiceDescriptor(typeof(ITelemetryInitializer), typeof(TelemetryBotIdInitializer), ServiceLifetime.Singleton),
                new ServiceDescriptor(typeof(IBotFrameworkHttpAdapter), typeof(TeamsBotHttpAdapter), ServiceLifetime.Singleton),
                new ServiceDescriptor(typeof(IStorage), typeof(MemoryStorage), ServiceLifetime.Singleton),
                new ServiceDescriptor(typeof(IProactiveMessagesService), typeof(ProactiveMessagesService), ServiceLifetime.Singleton),
                new ServiceDescriptor(typeof(IGraphSdkHelper), typeof(GraphSdkHelper), ServiceLifetime.Singleton),
                new ServiceDescriptor(typeof(IGraphApiService), typeof(GraphApiService), ServiceLifetime.Singleton),
                new ServiceDescriptor(typeof(IBotTelemetryClient), typeof(BotTelemetryClient), ServiceLifetime.Singleton),
                new ServiceDescriptor(typeof(ConversationState), typeof(ConversationState), ServiceLifetime.Singleton),
                new ServiceDescriptor(typeof(UserState), typeof(UserState), ServiceLifetime.Singleton),
                new ServiceDescriptor(typeof(TelemetryLoggerMiddleware), typeof(TelemetryLoggerMiddleware), ServiceLifetime.Singleton),
                new ServiceDescriptor(typeof(TelemetryInitializerMiddleware), typeof(TelemetryInitializerMiddleware), ServiceLifetime.Singleton),
            };
        }

        [Fact]
        public void AddTeamsIntegrationDefaultServices()
        {
            var results = new List<bool>();

            var configuration = A.Fake<IConfiguration>();
            var service = new ServiceCollection();

            var createdService = service.AddTeamsIntegrationDefaultServices(configuration);

            foreach (var descriptor in _serviceDescriptors)
            {

                 results.Add(createdService.Any(x =>
                    x.ServiceType == descriptor.ServiceType &&
                    x.ImplementationType == descriptor.ImplementationType && 
                    x.Lifetime == descriptor.Lifetime));
            }

            Assert.True(results.All(v => v == true));
        }

        [Fact]
        public void AddDialogRouter()
        {
            var dialogRoutes = new DialogRoute[1] {new DialogRoute(typeof(string), "command")};

            var serviceDescriptors = new List<ServiceDescriptor>()
            {
                new ServiceDescriptor(typeof(string), typeof(string), ServiceLifetime.Transient),
                new ServiceDescriptor(typeof(IDialogRouteService), sp => new DialogRouteService(sp, dialogRoutes), ServiceLifetime.Transient),
            };

            var results = new List<bool>();

            var service = new ServiceCollection();

            var createdService = service.AddDialogRouter(dialogRoutes);

            foreach (var descriptor in serviceDescriptors)
            {

                results.Add(createdService.Any(x =>
                    x.ServiceType == descriptor.ServiceType &&
                    x.ImplementationType == descriptor.ImplementationType &&
                    x.Lifetime == descriptor.Lifetime));
            }

            Assert.True(results.All(v => v == true));
        }
    }

}
