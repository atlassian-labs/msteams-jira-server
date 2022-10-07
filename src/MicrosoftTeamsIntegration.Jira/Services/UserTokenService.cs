using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using MicrosoftTeamsIntegration.Jira.Services.Interfaces;

namespace MicrosoftTeamsIntegration.Jira.Services
{
    public class UserTokenService : IUserTokenService
    {
        public Task<TokenResponse> GetUserTokenAsync(ITurnContext context, string connectionName, string magicCode, CancellationToken cancellationToken)
        {
            var userTokenClient = context.TurnState.Get<UserTokenClient>();
            return userTokenClient.GetUserTokenAsync(context.Activity.From.Id, connectionName, context.Activity.ChannelId, magicCode, cancellationToken);
        }
    }
}
