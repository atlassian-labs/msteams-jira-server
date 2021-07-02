using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;

namespace MicrosoftTeamsIntegration.Jira.Services.Interfaces
{
    public interface IBotFrameworkAdapterService
    {
        Task SignOutUserAsync(ITurnContext turnContext, string connectionName, string userId, CancellationToken cancellationToken);
    }
}
