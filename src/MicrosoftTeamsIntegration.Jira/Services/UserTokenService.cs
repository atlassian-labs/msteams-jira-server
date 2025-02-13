using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Options;
using MicrosoftTeamsIntegration.Jira.Services.Interfaces;
using MicrosoftTeamsIntegration.Jira.Settings;
using Newtonsoft.Json.Linq;

namespace MicrosoftTeamsIntegration.Jira.Services
{
    public class UserTokenService : IUserTokenService
    {
        private readonly AppSettings _appSettings;
        public UserTokenService(IOptions<AppSettings> appSettings)
        {
            _appSettings = appSettings.Value;
        }

        public Task<TokenResponse> GetUserTokenAsync(ITurnContext context, string connectionName, string magicCode, CancellationToken cancellationToken)
        {
            var userTokenClient = context.TurnState.Get<UserTokenClient>();
            return userTokenClient.GetUserTokenAsync(context.Activity.From.Id, connectionName, context.Activity.ChannelId, magicCode, cancellationToken);
        }

        public async Task<TokenResponse> GetUserTokenAsync(ITurnContext context, CancellationToken cancellationToken)
        {
            var userTokenClient = context.TurnState.Get<UserTokenClient>();
            var magicCodeObject = context.Activity.Value as JObject;
            var magicCode = magicCodeObject?.GetValue("state")?.ToString();
            var accessToken = await userTokenClient.GetUserTokenAsync(
                context.Activity.From.Id,
                _appSettings.OAuthConnectionName,
                context.Activity.ChannelId,
                magicCode,
                cancellationToken);

            return accessToken;
        }

        public async Task<string> GetSignInLink(ITurnContext context, CancellationToken cancellationToken)
        {
            var userTokenClient = context.TurnState.Get<UserTokenClient>();
            var link = (await userTokenClient
                .GetSignInResourceAsync(
                    _appSettings.OAuthConnectionName,
                    context.Activity,
                    null,
                    cancellationToken).ConfigureAwait(false)).SignInLink;

            return link;
        }
    }
}
