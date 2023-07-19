using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Bot.Schema;

namespace MicrosoftTeamsIntegration.Artifacts.Services.Interfaces
{
    [PublicAPI]
    public interface IProactiveMessagesService
    {
        Task SendActivity(IActivity activity, ConversationReference conversationReference, CancellationToken cancellationToken = default);
    }
}
