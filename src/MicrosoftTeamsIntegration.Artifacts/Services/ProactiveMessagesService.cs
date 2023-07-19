using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using MicrosoftTeamsIntegration.Artifacts.Services.Interfaces;

namespace MicrosoftTeamsIntegration.Artifacts.Services
{
    [PublicAPI]
    public class ProactiveMessagesService : IProactiveMessagesService
    {
        private readonly IBotFrameworkHttpAdapter _adapter;
        private readonly string _botId;

        public ProactiveMessagesService(
            IBotFrameworkHttpAdapter adapter,
            MicrosoftAppCredentials appCredentials)
        {
            _adapter = adapter;
            _botId = appCredentials.MicrosoftAppId;
        }

        public Task SendActivity(IActivity activity, ConversationReference conversationReference, CancellationToken cancellationToken = default)
        {
            return ((BotAdapter)_adapter).ContinueConversationAsync(
                _botId,
                conversationReference,
                async (turnContext, token) =>
                {
                    await turnContext.SendActivityAsync(activity, token);
                },
                cancellationToken);
        }
    }
}
