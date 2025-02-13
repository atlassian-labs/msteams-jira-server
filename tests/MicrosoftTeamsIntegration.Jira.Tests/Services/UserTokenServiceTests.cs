using System.Threading;
using System.Threading.Tasks;
using FakeItEasy;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Options;
using MicrosoftTeamsIntegration.Jira.Services;
using MicrosoftTeamsIntegration.Jira.Settings;
using Newtonsoft.Json.Linq;
using Xunit;

namespace MicrosoftTeamsIntegration.Jira.Tests.Services;

public class UserTokenServiceTests
{
    private readonly IOptions<AppSettings> _appSettings;

    public UserTokenServiceTests()
    {
        _appSettings = A.Fake<IOptions<AppSettings>>();
        A.CallTo(() => _appSettings.Value).Returns(new AppSettings { OAuthConnectionName = "testConnectionName" });
    }

    [Fact]
    public async Task GetUserTokenAsync_ShouldReturnTokenResponse()
    {
        // Arrange
        var userTokenClient = A.Fake<UserTokenClient>();
        var connectionName = "testConnection";
        var magicCode = "testMagicCode";
        var cancellationToken = CancellationToken.None;
        var expectedTokenResponse = new TokenResponse { Token = "testToken" };

        var activity = new Activity
        {
            From = new ChannelAccount { Id = "testUserId" },
            ChannelId = "testChannelId"
        };
        var turnState = new TurnContextStateCollection();
        turnState.Add(userTokenClient);
        var turnContext = A.Fake<ITurnContext>();
        A.CallTo(() => turnContext.Activity).Returns(activity);
        A.CallTo(() => turnContext.TurnState).Returns(turnState);
        A.CallTo(() =>
                userTokenClient.GetUserTokenAsync(
                    "testUserId",
                    connectionName,
                    "testChannelId",
                    magicCode,
                    cancellationToken))
            .Returns(expectedTokenResponse);

        var service = new UserTokenService(_appSettings);

        // Act
        var result = await service.GetUserTokenAsync(turnContext, connectionName, magicCode, cancellationToken);

        // Assert
        Assert.Equal(expectedTokenResponse, result);
    }

    [Fact]
    public async Task GetUserTokenOverloadedAsync_ShouldCallGetUserTokenAsyncWithCorrectParameters()
    {
        // Arrange
        var userTokenClient = A.Fake<UserTokenClient>();
        var connectionName = "testConnection";
        var magicCode = "testMagicCode";
        var cancellationToken = CancellationToken.None;

        var activity = new Activity
        {
            From = new ChannelAccount { Id = "testUserId" },
            ChannelId = "testChannelId"
        };
        var turnState = new TurnContextStateCollection();
        turnState.Add(userTokenClient);
        var turnContext = A.Fake<ITurnContext>();
        A.CallTo(() => turnContext.Activity).Returns(activity);
        A.CallTo(() => turnContext.TurnState).Returns(turnState);

        var service = new UserTokenService(_appSettings);

        // Act
        await service.GetUserTokenAsync(turnContext, connectionName, magicCode, cancellationToken);

        // Assert
        A.CallTo(() =>
                userTokenClient.GetUserTokenAsync(
                    "testUserId",
                    connectionName,
                    "testChannelId",
                    magicCode,
                    cancellationToken))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task GetSignInLink_ShouldReturnSignInLink()
    {
        // Arrange
        var userTokenClient = A.Fake<UserTokenClient>();
        var expectedSignInLink = "https://signinlink";
        var cancellationToken = CancellationToken.None;

        var activity = new Activity
        {
            From = new ChannelAccount { Id = "testUserId" },
            ChannelId = "testChannelId"
        };
        var turnState = new TurnContextStateCollection();
        turnState.Add(userTokenClient);
        var turnContext = A.Fake<ITurnContext>();
        A.CallTo(() => turnContext.Activity).Returns(activity);
        A.CallTo(() => turnContext.TurnState).Returns(turnState);
        A.CallTo(() =>
                userTokenClient.GetSignInResourceAsync(
                    _appSettings.Value.OAuthConnectionName,
                    activity,
                    null,
                    cancellationToken))
            .Returns(Task.FromResult(new SignInResource { SignInLink = expectedSignInLink }));

        var service = new UserTokenService(_appSettings);

        // Act
        var result = await service.GetSignInLink(turnContext, cancellationToken);

        // Assert
        Assert.Equal(expectedSignInLink, result);
    }

    [Fact]
    public async Task GetUserTokenOverloadedAsync_ShouldReturnTokenResponse()
    {
        // Arrange
        var userTokenClient = A.Fake<UserTokenClient>();
        var cancellationToken = CancellationToken.None;
        var expectedTokenResponse = new TokenResponse { Token = "testToken" };

        var activity = new Activity
        {
            From = new ChannelAccount { Id = "testUserId" },
            ChannelId = "testChannelId",
            Value = JObject.FromObject(new { state = "testMagicCode" })
        };
        var turnState = new TurnContextStateCollection();
        turnState.Add(userTokenClient);
        var turnContext = A.Fake<ITurnContext>();
        A.CallTo(() => turnContext.Activity).Returns(activity);
        A.CallTo(() => turnContext.TurnState).Returns(turnState);
        A.CallTo(() => userTokenClient.GetUserTokenAsync(
                "testUserId",
                "testConnectionName",
                "testChannelId",
                "testMagicCode",
                cancellationToken))
            .Returns(expectedTokenResponse);

        var service = new UserTokenService(_appSettings);

        // Act
        var result = await service.GetUserTokenAsync(turnContext, cancellationToken);

        // Assert
        Assert.Equal(expectedTokenResponse, result);
    }

    [Fact]
    public async Task GetUserTokenAsync_ShouldCallGetUserTokenAsyncWithCorrectParameters()
    {
        // Arrange
        var userTokenClient = A.Fake<UserTokenClient>();
        var cancellationToken = CancellationToken.None;

        var activity = new Activity
        {
            From = new ChannelAccount { Id = "testUserId" },
            ChannelId = "testChannelId",
            Value = JObject.FromObject(new { state = "testMagicCode" })
        };
        var turnState = new TurnContextStateCollection();
        turnState.Add(userTokenClient);
        var turnContext = A.Fake<ITurnContext>();
        A.CallTo(() => turnContext.Activity).Returns(activity);
        A.CallTo(() => turnContext.TurnState).Returns(turnState);

        var service = new UserTokenService(_appSettings);

        // Act
        await service.GetUserTokenAsync(turnContext, cancellationToken);

        // Assert
        A.CallTo(() => userTokenClient.GetUserTokenAsync(
                "testUserId",
                "testConnectionName",
                "testChannelId",
                "testMagicCode",
                cancellationToken))
            .MustHaveHappenedOnceExactly();
    }
}
