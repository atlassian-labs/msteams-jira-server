using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using MicrosoftTeamsIntegration.Jira.Exceptions;
using MicrosoftTeamsIntegration.Jira.Models;
using MicrosoftTeamsIntegration.Jira.Models.Jira;
using MicrosoftTeamsIntegration.Jira.Services;
using MicrosoftTeamsIntegration.Jira.Services.Interfaces;
using MicrosoftTeamsIntegration.Jira.Services.SignalR;
using MicrosoftTeamsIntegration.Jira.Services.SignalR.Interfaces;
using Newtonsoft.Json;
using Xunit;

namespace MicrosoftTeamsIntegration.Jira.Tests.Services
{
    public class JiraServerAuthServiceTests
    {
        private readonly ISignalRService _fakeSignalRService = A.Fake<ISignalRService>();
        private readonly IDatabaseService _fakeDatabaseService = A.Fake<IDatabaseService>();
        private readonly ILogger<JiraAuthService> _logger = A.Fake<ILogger<JiraAuthService>>();

        [Fact]
        public async Task SubmitOauthLoginInfo_WhenMsTeamsIdNull()
        {
            var service = CreateJiraServerAuthService();
            var result = await service.SubmitOauthLoginInfo(string.Empty, string.Empty, string.Empty, string.Empty,
                string.Empty, string.Empty);

            Assert.IsType<JiraAuthResponse>(result);
        }

        [Fact]
        public async Task SubmitOauthLoginInfo_ReturnsNull_WhenAccessTokenEmpty()
        {
            var service = CreateJiraServerAuthService();
            var result = await service.SubmitOauthLoginInfo("msTeamsId", string.Empty, string.Empty, "jiraId",
                string.Empty, string.Empty);

            Assert.Null(result);
        }

        [Fact]
        public async Task SubmitOauthLoginInfo_ReturnFalse_WhenReceivedFalse()
        {
            var service = CreateJiraServerAuthService();

            A.CallTo(() =>
                    _fakeSignalRService.SendRequestAndWaitForResponse(A<string>._, A<string>._, A<CancellationToken>._))
                .Returns(new SignalRResponse()
                {
                    Message = "String",
                    Received = false
                });

            var result = await service.SubmitOauthLoginInfo("msTeamsId", string.Empty, "token", "jiraId",
                string.Empty, string.Empty);

            Assert.False(result.IsSuccess);
        }

        [Fact]
        public async Task SubmitOauthLoginInfo_ThrowsAnException()
        {
            var response = new JiraResponse<JiraAuthResponse>()
            {
                Response = new JiraAuthResponse()
                {
                    IsSuccess = true
                },
                ResponseCode = (int)HttpStatusCode.Forbidden,
                Message = "Test"
            };

            var service = CreateJiraServerAuthService();

            A.CallTo(() =>
                    _fakeSignalRService.SendRequestAndWaitForResponse(A<string>._, A<string>._, A<CancellationToken>._))
                .Returns(new SignalRResponse()
                {
                    Message = JsonConvert.SerializeObject(response),
                    Received = true
                });

            var result =  service.SubmitOauthLoginInfo("msTeamsId", string.Empty, "token", "jiraId",
                string.Empty, string.Empty);

            Assert.ThrowsAsync<ForbiddenException>(() => result);
        }

        [Fact]
        public async Task SubmitOauthLoginInfo_ReturnTrue()
        {
            var service = CreateJiraServerAuthService();

            var response = new JiraResponse<JiraAuthResponse>()
            {
                Response = new JiraAuthResponse()
                {
                    IsSuccess = true
                },
                ResponseCode = (int)HttpStatusCode.OK,
                Message = "Test"
            };

            A.CallTo(() =>
                    _fakeSignalRService.SendRequestAndWaitForResponse(A<string>._, A<string>._, A<CancellationToken>._))
                .Returns(new SignalRResponse()
                {
                    Message = JsonConvert.SerializeObject(response),
                    Received = true
                });

            var result = await service.SubmitOauthLoginInfo("msTeamsId", string.Empty, "token", "jiraId",
                string.Empty, string.Empty);

            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task SubmitOauthLoginInfo_ReturnsTrueAndDBMethodShouldHappen()
        {
            var service = CreateJiraServerAuthService();

            var response = new JiraResponse<JiraAuthResponse>()
            {
                Response = new JiraAuthResponse()
                {
                    IsSuccess = true
                },
                ResponseCode = (int)HttpStatusCode.OK,
                Message = "Test"
            };

            A.CallTo(() =>
                    _fakeSignalRService.SendRequestAndWaitForResponse(A<string>._, A<string>._, A<CancellationToken>._))
                .Returns(new SignalRResponse()
                {
                    Message = JsonConvert.SerializeObject(response),
                    Received = true
                });

            A.CallTo(() => _fakeDatabaseService.GetOrCreateJiraServerUser(A<string>._, A<string>._, A<string>._))
                .Returns(new IntegratedUser());

            var result = await service.SubmitOauthLoginInfo("msTeamsId", string.Empty, "token", "jiraId",
                string.Empty, "token");

            Assert.True(result.IsSuccess);

            A.CallTo(() => _fakeDatabaseService.GetOrCreateJiraServerUser(A<string>._, A<string>._, A<string>._))
                .MustHaveHappened();
        }

        [Fact]
        public async Task Logout()
        {
            var service = CreateJiraServerAuthService();

            var response = new JiraResponse<JiraAuthResponse>()
            {
                Response = new JiraAuthResponse()
                {
                    IsSuccess = true
                },
                ResponseCode = (int)HttpStatusCode.OK,
                Message = "Test"
            };

            A.CallTo(() =>
                    _fakeSignalRService.SendRequestAndWaitForResponse(A<string>._, A<string>._, A<CancellationToken>._))
                .Returns(new SignalRResponse()
                {
                    Message = JsonConvert.SerializeObject(response),
                    Received = true
                });

            A.CallTo(() => _fakeDatabaseService.DeleteJiraServerUser(A<string>._, A<string>._))
                .Returns(Task.Delay(1));

            var result = await service.Logout(JiraDataGenerator.GenerateUser());

            Assert.True(result.IsSuccess);

            A.CallTo(() => _fakeDatabaseService.DeleteJiraServerUser(A<string>._, A<string>._))
                .MustHaveHappened();
        }

        [Fact]
        public async Task IsJiraConnected_ReturnsTrue_WhenAddonIsInstalled()
        {
            var service = CreateJiraServerAuthService();
            var user = JiraDataGenerator.GenerateUser();
            user.JiraServerId = "id";

            A.CallTo(() => _fakeDatabaseService.GetJiraServerAddonStatus(A<string>._, A<string>._)).Returns(
                new AtlassianAddonStatus()
                {
                    AddonIsInstalled = true
                });

            var result = await service.IsJiraConnected(user);

            Assert.True(result);
        }

        [Fact]
        public async Task IsJiraConnected_ReturnsFalse_WhenAddonIsNotInstalled()
        {
            var service = CreateJiraServerAuthService();
            var result = await service.IsJiraConnected(JiraDataGenerator.GenerateUser());

            A.CallTo(() => _fakeDatabaseService.GetJiraServerAddonStatus(A<string>._, A<string>._)).Returns(
                new AtlassianAddonStatus()
                {
                    JiraInstanceUrl = "url"
                });

            Assert.False(result);
        }

        private JiraAuthService CreateJiraServerAuthService()
        {
            var service = new JiraAuthService(_fakeSignalRService, _fakeDatabaseService, _logger);
            return service;
        }

    }
}
