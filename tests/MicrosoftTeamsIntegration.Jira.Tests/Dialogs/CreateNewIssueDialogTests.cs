using System.Linq;
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
using MicrosoftTeamsIntegration.Jira.Models.Bot;
using MicrosoftTeamsIntegration.Jira.Models.Jira;
using MicrosoftTeamsIntegration.Jira.Services.Interfaces;
using MicrosoftTeamsIntegration.Jira.Settings;
using Xunit;
using Xunit.Abstractions;

namespace MicrosoftTeamsIntegration.Jira.Tests.Dialogs
{
    public class CreateNewIssueDialogTests
    {
        private readonly IMiddleware[] _middleware;
        private readonly JiraBotAccessors _fakeAccessors;
        private readonly TelemetryClient _telemetry;
        private readonly IJiraService _fakeJiraService;
        private readonly AppSettings _appSettings;
        private const string DontHaveAccessMessage = "You don't have permissions to create issues. " +
                                                  "For more information contact your project administrator.";

        public CreateNewIssueDialogTests(ITestOutputHelper output)
        {
            _middleware = new IMiddleware[] {new XUnitDialogTestLogger(output)};
            _fakeAccessors = A.Fake<JiraBotAccessors>();
            _fakeAccessors.User = A.Fake<IStatePropertyAccessor<IntegratedUser>>();
            _fakeAccessors.JiraIssueState = A.Fake<IStatePropertyAccessor<JiraIssueState>>();
            _fakeJiraService = A.Fake<IJiraService>();
            _appSettings = new AppSettings();
            _telemetry = new TelemetryClient(TelemetryConfiguration.CreateDefault());
        }

        [Fact]
        public async Task CreateNewIssueDialog_WhenUserDontHaveAccess()
        {
            var sut = new CreateNewIssueDialog(_fakeAccessors, _fakeJiraService, _appSettings, _telemetry);
            var testClient = new DialogTestClient(Channels.Test, sut, middlewares: _middleware);

            A.CallTo(() => _fakeJiraService.GetMyPermissions(A<IntegratedUser>._, A<string>._, A<string>._, A<string>._))
                .Returns(new JiraPermissionsResponse()
                {
                    Permissions = new JiraPermissions()
                    {
                        CreateIssues = new JiraPermission()
                        {
                            Id = 1
                        }
                    }
                });

            var reply = await testClient.SendActivityAsync<IMessageActivity>("create");

            Assert.Equal(DontHaveAccessMessage, reply.Text);
            Assert.Equal(DialogTurnStatus.Complete, testClient.DialogTurnResult.Status);
        }

        [Fact]
        public async Task CreateNewIssueDialog_Check()
        {
            var sut = new CreateNewIssueDialog(_fakeAccessors, _fakeJiraService, _appSettings, _telemetry);
            var testClient = new DialogTestClient(Channels.Test, sut, middlewares: _middleware);

            A.CallTo(() => _fakeJiraService.GetMyPermissions(A<IntegratedUser>._, A<string>._, A<string>._, A<string>._))
                .Returns(new JiraPermissionsResponse()
                {
                    Permissions = new JiraPermissions()
                    {
                        CreateIssues = new JiraPermission()
                        {
                            Id = 1,
                            HavePermission = true
                        }
                    }
                });

            var reply = await testClient.SendActivityAsync<IMessageActivity>("create");

            Assert.NotNull(reply.Attachments.FirstOrDefault());
            Assert.IsType<AdaptiveCards.AdaptiveCard>(reply.Attachments.FirstOrDefault().Content);
            Assert.Equal(DialogTurnStatus.Complete, testClient.DialogTurnResult.Status);
        }

    }
}
