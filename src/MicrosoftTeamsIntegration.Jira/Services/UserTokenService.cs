using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using MicrosoftTeamsIntegration.Jira.Services.Interfaces;

namespace MicrosoftTeamsIntegration.Jira.Services
{
    public class UserTokenService : IUserTokenService
    {
        public Task<TokenResponse> GetUserTokenAsync(ITurnContext context, string connectionName, string magicCode, CancellationToken cancellationToken)
        {
            var adapter = (IUserTokenProvider)context.Adapter;
            return adapter.GetUserTokenAsync(context, connectionName, magicCode, cancellationToken);
        }
    }
}
