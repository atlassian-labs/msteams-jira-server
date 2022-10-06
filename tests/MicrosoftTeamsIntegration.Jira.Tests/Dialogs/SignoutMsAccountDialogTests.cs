using System.Threading;
using System.Threading.Tasks;
using FakeItEasy;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Testing;
using Microsoft.Bot.Builder.Testing.XUnit;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;
using MicrosoftTeamsIntegration.Jira.Dialogs;
using MicrosoftTeamsIntegration.Jira.Models;
using MicrosoftTeamsIntegration.Jira.Models.Bot;
using MicrosoftTeamsIntegration.Jira.Services.Interfaces;
using MicrosoftTeamsIntegration.Jira.Settings;
using Xunit;
using Xunit.Abstractions;

namespace MicrosoftTeamsIntegration.Jira.Tests.Dialogs
{
    public class SignoutMsAccountDialogTests
    {
        private readonly IMiddleware[] _middleware;
        private readonly JiraBotAccessors _fakeAccessors;
        private readonly TelemetryClient _telemetry;
        private readonly IBotFrameworkAdapterService _fakeBotFrameworkAdapterService;
        private readonly AppSettings _appSettings;

        public SignoutMsAccountDialogTests(ITestOutputHelper output)
        {
            _middleware = new IMiddleware[] {new XUnitDialogTestLogger(output)};
            _fakeAccessors = A.Fake<JiraBotAccessors>();
            _fakeAccessors.User = A.Fake<IStatePropertyAccessor<IntegratedUser>>();
            _fakeAccessors.JiraIssueState = A.Fake<IStatePropertyAccessor<JiraIssueState>>();
            _fakeBotFrameworkAdapterService = A.Fake<IBotFrameworkAdapterService>();
            _appSettings = new AppSettings();
            _telemetry = new TelemetryClient(TelemetryConfiguration.CreateDefault());
        }

        [Fact]
        public async Task SignOutMSAccount()
        {
            var sut = new SignoutMsAccountDialog(_fakeAccessors, _appSettings, _telemetry, _fakeBotFrameworkAdapterService);
            var testClient = new DialogTestClient(Channels.Test, sut, middlewares: _middleware);

            A.CallTo(() => _fakeBotFrameworkAdapterService.SignOutUserAsync(A<ITurnContext>._, A<string>._, CancellationToken.None))
                .Returns(Task.Delay(1));

            await testClient.SendActivityAsync<IMessageActivity>("Signout");

            A.CallTo(() => _fakeBotFrameworkAdapterService.SignOutUserAsync(A<ITurnContext>._, A<string>._, CancellationToken.None))
                .MustHaveHappened();
        }
    }
}
