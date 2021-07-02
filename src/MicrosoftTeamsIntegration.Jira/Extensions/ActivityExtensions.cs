using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Teams;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Schema.Teams;
using MicrosoftTeamsIntegration.Artifacts.Extensions;
using Newtonsoft.Json.Linq;

namespace MicrosoftTeamsIntegration.Jira.Extensions
{
    public static class ActivityExtensions
    {
        private static readonly int _minAttachmentDimension = 1;

        public static string GetTextWithoutCommand(this IActivity activity, string commandMatch)
        {
            var query = string.Empty;
            if (activity is Activity act && act.Text.HasValue())
            {
                query = act.RemoveRecipientMention();
            }

            var regex = new Regex(commandMatch, RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);

            // replace only the fist match because the additional text can contain the word equals to the command name
            var result = regex.Replace(query, string.Empty, 1);
            return result.NormalizeUtterance();
        }

        public static async Task<string> GetBotUserAccessToken(this ITurnContext turnContext, string connectionName, string magicCode = null, CancellationToken cancellationToken = default)
        {
            var adapter = (IUserTokenProvider)turnContext.Adapter;
            var token = await adapter.GetUserTokenAsync(turnContext, connectionName, magicCode, cancellationToken).ConfigureAwait(false);
            return token?.Token;
        }

        public static async Task<string> GetMsTeamsUserIdFromMentions(this ITurnContext turnContext)
        {
            var mentions = turnContext.Activity.GetMentions();

            // bot itself is also mentioned
            if (mentions.Length < 2)
            {
                return string.Empty;
            }

            var mentionedUser = mentions[1].Mentioned;

            var user = await TeamsInfo.GetMemberAsync(turnContext, mentionedUser.Id);
            if (user == null)
            {
                return string.Empty;
            }

            return user.AadObjectId;
        }

        public static bool IsHtmlMessage(this Activity activity)
        {
            var activityData = false;
            if (activity.Attachments?.Count > _minAttachmentDimension)
            {
                activityData = activity.Attachments.Any(x => x.ContentType.Equals("text/html", StringComparison.OrdinalIgnoreCase));
            }

            return activityData;
        }

        public static bool IsO365ConnectorCardActionQuery(this IInvokeActivity activity)
        {
            return activity.Type == ActivityTypes.Invoke &&
                   !string.IsNullOrEmpty(activity.Name) &&
                   activity.Name.StartsWith("actionableMessage/executeAction", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Gets O365 connector card action query data.
        /// </summary>
        /// <param name="activity">The activity.</param>
        /// <returns>O365 connector card action query data.</returns>
        public static O365ConnectorCardActionQuery GetO365ConnectorCardActionQueryData(this IInvokeActivity activity)
        {
            return JObject.FromObject(activity.Value).ToObject<O365ConnectorCardActionQuery>();
        }

        /// <summary>
        /// Creates a new attachment from <see cref="O365ConnectorCard"/>.
        /// </summary>
        /// <param name="card"> The instance of <see cref="O365ConnectorCard"/>.</param>
        /// <returns> The generated attachment.</returns>
        public static Attachment ToAttachment(this O365ConnectorCard card)
        {
            return new Attachment
            {
                Content = card,
                ContentType = O365ConnectorCard.ContentType
            };
        }

        public static bool IsComposeExtensionFetchTask(this Activity activity)
        {
            return !string.IsNullOrEmpty(activity.Name) &&
                   activity.Name.Equals("composeExtension/fetchTask", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsBotFetchTask(this Activity activity)
        {
            return !string.IsNullOrEmpty(activity.Name) &&
                   activity.Name.Equals("task/fetch", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsComposeExtensionSubmitAction(this Activity activity)
        {
            return !string.IsNullOrEmpty(activity.Name) &&
                   activity.Name.Equals("composeExtension/submitAction", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsComposeExtensionQueryLink(this Activity activity)
        {
            return !string.IsNullOrEmpty(activity.Name) &&
                   activity.Name.Equals("composeExtension/queryLink", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsTaskSubmitAction(this Activity activity)
        {
            return !string.IsNullOrEmpty(activity.Name) &&
                   activity.Name.Equals("task/submit", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsRequestMessagingExtensionQuery(this Activity activity)
        {
            return !string.IsNullOrEmpty(activity.Name) &&
                   activity.Name.Equals("composeExtension/query", StringComparison.OrdinalIgnoreCase);
        }
    }
}
