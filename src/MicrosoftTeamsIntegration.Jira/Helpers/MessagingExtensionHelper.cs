using System;
using System.Collections.Generic;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Schema.Teams;

namespace MicrosoftTeamsIntegration.Jira.Helpers
{
    public static class MessagingExtensionHelper
    {
        public static MessagingExtensionResponse BuildMessageResponse(string message)
        {
            return new MessagingExtensionResponse
            {
                ComposeExtension = new MessagingExtensionResult
                {
                    Type = ActivityTypes.Message,
                    Text = message
                }
            };
        }

        public static MessagingExtensionResponse BuildMessagingExtensionQueryResult(List<MessagingExtensionAttachment> attachments)
        {
            var messagingExtensionResult = new MessagingExtensionResult
            {
                Type = "result",
                Attachments = attachments,
                AttachmentLayout = "list"
            };

            return new MessagingExtensionResponse
            {
                ComposeExtension = messagingExtensionResult
            };
        }

        public static MessagingExtensionResponse BuildCardActionResponse(string type, string title, string url)
        {
            var cardAction = new CardAction(ActionTypes.OpenUrl, title, value: url);
            var cardActions = new List<CardAction> { cardAction };
            var suggestedAction = new MessagingExtensionSuggestedAction
            {
                Actions = cardActions
            };

            var composeExtensionResult = new MessagingExtensionResult
            {
                SuggestedActions = suggestedAction,
                Type = type
            };

            var composeExtensionResponse = new MessagingExtensionResponse
            {
                ComposeExtension = composeExtensionResult
            };

            return composeExtensionResponse;
        }

        public static string GetQueryParameterByName(MessagingExtensionQuery query, string name)
        {
            if (query?.Parameters == null || query.Parameters.Count == 0)
            {
                return string.Empty;
            }

            var parameter = query.Parameters[0];
            if (!string.Equals(parameter.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                return string.Empty;
            }

            return parameter.Value != null ? parameter.Value.ToString() : string.Empty;
        }
    }
}
