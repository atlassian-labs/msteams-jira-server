using FakeItEasy;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using MicrosoftTeamsIntegration.Artifacts.Services;

namespace MicrosoftTeamsIntegration.Artifacts.Tests.Services;

public class ProactiveMessagesServiceTests
{
    private readonly CloudAdapter _fakeAdapter;
    private readonly MicrosoftAppCredentials _fakeAppCredentials;
    private readonly ProactiveMessagesService _proactiveMessagesService;

    public ProactiveMessagesServiceTests()
    {
        _fakeAdapter = A.Fake<CloudAdapter>();
        _fakeAppCredentials = A.Fake<MicrosoftAppCredentials>(options => 
            options.WithArgumentsForConstructor(() => new MicrosoftAppCredentials("appId", "password", null, null, null)));
        _proactiveMessagesService = new ProactiveMessagesService(_fakeAdapter, _fakeAppCredentials);
    }

    [Fact]
    public async Task SendActivity_ShouldSendActivity()
    {
        // Arrange
        var activity = new Activity { Type = ActivityTypes.Message, Text = "Hello, World!" };
        var conversationReference = new ConversationReference { Conversation = new ConversationAccount { Id = "conversationId" } };
        var cancellationToken = CancellationToken.None;

        A.CallTo(() => _fakeAdapter.ContinueConversationAsync(
                A<string>._,
                A<ConversationReference>._,
                A<BotCallbackHandler>._,
                A<CancellationToken>._))
            .Returns(Task.CompletedTask);

        // Act
        await _proactiveMessagesService.SendActivity(activity, conversationReference, cancellationToken);

        // Assert
        A.CallTo(() => _fakeAdapter.ContinueConversationAsync(
            _fakeAppCredentials.MicrosoftAppId,
            conversationReference,
            A<BotCallbackHandler>._,
            cancellationToken)).MustHaveHappenedOnceExactly();
    }
}
