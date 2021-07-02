using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using MicrosoftTeamsIntegration.Jira.Services.Interfaces;

namespace MicrosoftTeamsIntegration.Jira.Services
{
    public class BotFrameworkAdapterService : IBotFrameworkAdapterService
    {
        public Task SignOutUserAsync(ITurnContext turnContext, string connectionName, string userId, CancellationToken cancellationToken)
        {
            BotFrameworkAdapter botAdapter = (BotFrameworkAdapter)turnContext.Adapter;
            return botAdapter.SignOutUserAsync(turnContext, connectionName, userId, cancellationToken);
        }
    }
}
