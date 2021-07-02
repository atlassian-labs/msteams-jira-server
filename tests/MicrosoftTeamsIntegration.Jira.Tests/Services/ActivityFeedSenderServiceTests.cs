using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FakeItEasy;
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.IdentityModel.Tokens;
using MicrosoftTeamsIntegration.Artifacts.Models;
using MicrosoftTeamsIntegration.Artifacts.Models.GraphApi;
using MicrosoftTeamsIntegration.Artifacts.Services.Interfaces;
using MicrosoftTeamsIntegration.Artifacts.Services.Interfaces.Refit;
using MicrosoftTeamsIntegration.Jira.Models.Notifications;
using MicrosoftTeamsIntegration.Jira.Services;
using MicrosoftTeamsIntegration.Jira.Settings;
using Xunit;

namespace MicrosoftTeamsIntegration.Jira.Tests.Services
{
    public class ActivityFeedSenderServiceTests
    {
        private const string TeamsActivitySendRole = "TeamsActivity.Send";

        private readonly IGraphApiService _fakeGraphApiService = A.Fake<IGraphApiService>();
        private readonly IGraphSdkHelper _fakeGraphSdkHelper = A.Fake<IGraphSdkHelper>();
        private readonly IOAuthV2Service _fakeOAuthV2Service = A.Fake<IOAuthV2Service>();
        private readonly ILogger<ActivityFeedSenderService> _logger = A.Fake<ILogger<ActivityFeedSenderService>>();
        private readonly IOptions<AppSettings> _appSettings = new OptionsManager<AppSettings>(
            new OptionsFactory<AppSettings>(new List<IConfigureOptions<AppSettings>>(), new List<IPostConfigureOptions<AppSettings>>()));
        private readonly TelemetryClient _telemetry = new TelemetryClient();

        [Fact]
        public async Task GenerateActivityNotification_ThrowsArgumentNullException_WhenNotificationNull()
        {
            var service = CreateActivityFeedSenderService();
            Assert.ThrowsAsync<ArgumentNullException>(() => service.GenerateActivityNotification(null, null));
        }

        [Fact]
        public async Task GenerateActivityNotification_ThrowsArgumentNullException_WhenUserNull()
        {
            var service = CreateActivityFeedSenderService();
            Assert.ThrowsAsync<ArgumentNullException>(() => service.GenerateActivityNotification(null, new NotificationFeedEvent()));
        }

        [Fact]
        public async Task TryGetOAuthTokenAsync_ReturnsNull_WhenException()
        {
            _appSettings.Value.MicrosoftAppId = "appID";
            _appSettings.Value.MicrosoftAppPassword = "password";

            var user = JiraDataGenerator.GenerateUser();
            user.MsTeamsTenantId = "teamsId";

            A.CallTo(() => _fakeOAuthV2Service.GetToken(A<OAuthRequest>._, A<string>._)).Throws<Exception>();

            A.CallTo(() => _fakeGraphSdkHelper.GetAuthenticatedClient(A<string>._)).Returns(new GraphServiceClient(new HttpClient()));

            var service = CreateActivityFeedSenderService();
            await service.GenerateActivityNotification(user, new NotificationFeedEvent());

            A.CallTo(() => _fakeGraphSdkHelper.GetAuthenticatedClient(A<string>._)).MustNotHaveHappened();
        }

        [Fact]
        public async Task TryGetOAuthTokenAsync_ReturnsNull_WhenMsTeamsIdNull()
        {
            _appSettings.Value.MicrosoftAppId = "appID";
            _appSettings.Value.MicrosoftAppPassword = "password";

            var user = JiraDataGenerator.GenerateUser();
            user.MsTeamsTenantId = null;

            A.CallTo(() => _fakeOAuthV2Service.GetToken(A<OAuthRequest>._, A<string>._)).Returns(new OAuthToken());

            A.CallTo(() => _fakeGraphSdkHelper.GetAuthenticatedClient(A<string>._)).Returns(new GraphServiceClient(new HttpClient()));

            var service = CreateActivityFeedSenderService();
            await service.GenerateActivityNotification(user, new NotificationFeedEvent());

            A.CallTo(() => _fakeGraphSdkHelper.GetAuthenticatedClient(A<string>._)).MustNotHaveHappened();
        }

        [Fact]
        public async Task GenerateActivityNotification()
        {
            _appSettings.Value.MicrosoftAppId = "appID";
            _appSettings.Value.MicrosoftAppPassword = "password";

            var user = JiraDataGenerator.GenerateUser();
            user.MsTeamsTenantId = "msTeamsId";

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Expires = DateTime.UtcNow.AddHours(3),
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim("roles", TeamsActivitySendRole),
                }),
                SigningCredentials = new SigningCredentials(key: new SymmetricSecurityKey(Encoding.UTF8.GetBytes("this is my custom Secret key for authnetication")), algorithm: SecurityAlgorithms.HmacSha256Signature)
            };

            var securityToken = new JwtSecurityTokenHandler().CreateToken(tokenDescriptor);
            var tokenString = new JwtSecurityTokenHandler().WriteToken(securityToken);

            A.CallTo(() => _fakeOAuthV2Service.GetToken(A<OAuthRequest>._, A<string>._)).Returns(new OAuthToken()
            {
                AccessToken = tokenString
            });

            A.CallTo(() => _fakeGraphSdkHelper.GetAuthenticatedClient(A<string>._)).Returns(new GraphServiceClient(new HttpClient()));

            A.CallTo(() => _fakeGraphApiService.SendActivityNotificationAsync(A<GraphServiceClient>._, A<ActivityNotification>._, A<string>._, CancellationToken.None))
                .Returns(Task.Delay(1));

            var service = CreateActivityFeedSenderService();
            await service.GenerateActivityNotification(user, new NotificationFeedEvent(){ FeedEventType = FeedEventType.FieldUpdated, IssueId = "id"});

            A.CallTo(() => _fakeGraphSdkHelper.GetAuthenticatedClient(A<string>._)).MustHaveHappened();
        }

        private ActivityFeedSenderService CreateActivityFeedSenderService()
        {
            return new ActivityFeedSenderService(_fakeGraphApiService, _fakeGraphSdkHelper, _fakeOAuthV2Service, _appSettings, _logger, _telemetry);
        }
    }
}
