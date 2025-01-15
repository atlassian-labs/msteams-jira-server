using System.Threading.Tasks;
using FakeItEasy;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Testing;
using Microsoft.Bot.Builder.Testing.XUnit;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;
using MicrosoftTeamsIntegration.Jira.Dialogs;
using MicrosoftTeamsIntegration.Jira.Models;
using MicrosoftTeamsIntegration.Jira.Services.Interfaces;
using MicrosoftTeamsIntegration.Jira.Settings;
using Xunit;
using Xunit.Abstractions;

namespace MicrosoftTeamsIntegration.Jira.Tests
{
    public class JiraBotTests
    {
        private readonly IMiddleware[] _middleware;
        private readonly JiraBotAccessors _mockAccessors;
        private readonly TelemetryClient _telemetry;
        private readonly IAnalyticsService _analyticsService;
        public JiraBotTests(ITestOutputHelper output)
        {
            _middleware = new IMiddleware[] { new XUnitDialogTestLogger(output) };
            _mockAccessors = A.Fake<JiraBotAccessors>();
            _mockAccessors.User = A.Fake<IStatePropertyAccessor<IntegratedUser>>();
            _telemetry = new TelemetryClient(TelemetryConfiguration.CreateDefault());
            _analyticsService = A.Fake<IAnalyticsService>();
        }

        [Fact]
        public async Task UserIsAllowedToStartHelpDialog()
        {
            var sut = new HelpDialog(_mockAccessors, new AppSettings(), _telemetry, _analyticsService);
            var testClient = new DialogTestClient(Channels.Test, sut, middlewares: _middleware);

            // Execute the test case
            var reply = await testClient.SendActivityAsync<IMessageActivity>("help");
            Assert.Contains("Here’s a list of the commands", reply.Text);
            Assert.Equal(DialogTurnStatus.Complete, testClient.DialogTurnResult.Status);
        }

        [Fact]
        public async Task UserGetsProperHelpForJiraServer()
        {
            var sut = new HelpDialog(_mockAccessors, new AppSettings(), _telemetry, _analyticsService);
            var testClient = new DialogTestClient(Channels.Test, sut, middlewares: _middleware);

            // Execute the test case
            var reply = await testClient.SendActivityAsync<IMessageActivity>("help");
            Assert.Contains("Jira Data Center instance", reply.Text);
        }
    }
}
