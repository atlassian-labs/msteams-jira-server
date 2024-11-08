using AdaptiveCards;
using AutoMapper;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Schema.Teams;
using MicrosoftTeamsIntegration.Artifacts.Extensions;
using MicrosoftTeamsIntegration.Jira.Models;

namespace MicrosoftTeamsIntegration.Jira.TypeConverters
{
    public class JiraIssueToMessagingExtensionAttachmentTypeConverter : ITypeConverter<BotAndMessagingExtensionJiraIssue, MessagingExtensionAttachment>
    {
        public MessagingExtensionAttachment Convert(BotAndMessagingExtensionJiraIssue model, MessagingExtensionAttachment attachment, ResolutionContext context)
        {
            var card = context.Mapper.Map<AdaptiveCard>(model);
            var preview = context.Mapper.Map<ThumbnailCard>(model);

            return card.ToAttachment().ToMessagingExtensionAttachment(preview.ToAttachment());
        }
    }
}
