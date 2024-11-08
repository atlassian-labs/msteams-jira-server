using System;
using System.Linq;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Schema.Teams;
using MicrosoftTeamsIntegration.Artifacts.Models;

namespace MicrosoftTeamsIntegration.Artifacts.Extensions
{
    [PublicAPI]
    public static class TeamsExtensions
    {
        public static Task<ResourceResponse?> SendToDirectConversationAsync(this ITurnContext turnContext, string message, CancellationToken cancellationToken = default)
        {
            var messageActivity = MessageFactory.Text(message);
            return SendToDirectConversationAsync(turnContext, messageActivity, cancellationToken);
        }

        public static async Task<ResourceResponse?> SendToDirectConversationAsync(this ITurnContext turnContext, IMessageActivity message, CancellationToken cancellationToken = default)
        {
            var isSuccess = true;
            ResourceResponse? resourceResponse = default;
            try
            {
                var conversationParameters = new ConversationParameters
                {
                    Bot = turnContext.Activity.Recipient,
                    Members = new[] { turnContext.Activity.From },
                    ChannelData = new TeamsChannelData
                        {
                            Tenant = new TenantInfo
                            {
                                Id = turnContext.Activity.Conversation.TenantId
                            }
                        }
                };

                var connector = turnContext.TurnState.Get<IConnectorClient>();

                var conversationResourceResponse = await connector.Conversations.CreateConversationAsync(conversationParameters, cancellationToken);

                message.Conversation = new ConversationAccount(id: conversationResourceResponse.Id);
                resourceResponse = await connector.Conversations.SendToConversationAsync((Activity)message, cancellationToken);
            }
            catch
            {
                isSuccess = false;
            }

            if (!isSuccess && turnContext.Activity.IsGroupConversation())
            {
                resourceResponse = await turnContext.SendActivityAsync(message, cancellationToken);
            }

            return resourceResponse;
        }

        public static bool ContainsHtmlAttachment(this Activity activity)
        {
            var activityData = false;
            if (activity.Attachments != null)
            {
                activityData = activity.Attachments.Any(x => x.ContentType.Equals(MediaTypeNames.Text.Html, StringComparison.OrdinalIgnoreCase));
            }

            return activityData;
        }

        public static ClientInfo? GetClientInfo(this Activity activity)
        {
            if (activity.Entities is null || activity.Entities?.Count == 0)
            {
                return null;
            }

            var clientInfo = activity.Entities!.Where(entity => entity.Type.Equals("clientInfo", StringComparison.OrdinalIgnoreCase)).ToList();
            return !clientInfo.Any() ? null : clientInfo.First().GetAs<ClientInfo>();
        }

        public static string GetCountryCode(this Activity activity)
            => GetClientInfo(activity)?.Country ?? string.Empty;

        public static bool IsGroupConversation(this IActivity activity) => activity?.Conversation?.IsGroup == true;
    }
}
