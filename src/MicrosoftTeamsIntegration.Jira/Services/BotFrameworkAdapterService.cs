using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Connector.Authentication;
using MicrosoftTeamsIntegration.Jira.Services.Interfaces;

namespace MicrosoftTeamsIntegration.Jira.Services
{
    public class BotFrameworkAdapterService : IBotFrameworkAdapterService
    {
        public Task SignOutUserAsync(ITurnContext turnContext, string connectionName, CancellationToken cancellationToken)
        {
            var userTokenClient = turnContext.TurnState.Get<UserTokenClient>();
            return userTokenClient.SignOutUserAsync(turnContext.Activity.From.Id, connectionName, turnContext.Activity.ChannelId, cancellationToken);
        }
    }
}
