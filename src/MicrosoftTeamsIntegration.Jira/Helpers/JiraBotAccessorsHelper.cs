using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using MicrosoftTeamsIntegration.Jira.Extensions;
using MicrosoftTeamsIntegration.Jira.Models;
using MicrosoftTeamsIntegration.Jira.Settings;

namespace MicrosoftTeamsIntegration.Jira.Helpers
{
    public static class JiraBotAccessorsHelper
    {
        public static async Task<IntegratedUser> GetUser(JiraBotAccessors accessors, ITurnContext context, AppSettings appSettings, CancellationToken cancellationToken = default)
        {
            // get user form state
            var user = await accessors.User.GetAsync(context, () => new IntegratedUser(), cancellationToken);

            if (user != null)
            {
                user.AccessToken = await context.GetBotUserAccessToken(appSettings.OAuthConnectionName, cancellationToken: cancellationToken);
            }

            return user;
        }
    }
}
