using System.Threading;
using System.Threading.Tasks;
using FakeItEasy;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using MicrosoftTeamsIntegration.Jira.Services;
using Xunit;

namespace MicrosoftTeamsIntegration.Jira.Tests.Services;

public class BotFrameworkAdapterServiceTests
{
    [Fact]
    public async Task SignOutUserAsync_ShouldCallSignOutUserAsync()
    {
        // Arrange
        var userTokenClient = A.Fake<UserTokenClient>();
        var connectionName = "testConnection";
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
        var service = new BotFrameworkAdapterService();

        // Act
        await service.SignOutUserAsync(turnContext, connectionName, cancellationToken);

        // Assert
        A.CallTo(() => userTokenClient.SignOutUserAsync("testUserId", connectionName, "testChannelId", cancellationToken))
            .MustHaveHappenedOnceExactly();
    }
}
