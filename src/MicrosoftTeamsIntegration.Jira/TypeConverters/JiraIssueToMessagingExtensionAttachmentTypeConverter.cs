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
            // pass context option items to child mappers
            void PassContextOptions(IMappingOperationOptions opts)
            {
                foreach (var item in context.Items)
                {
                    if (!opts.Items.Contains(item))
                    {
                        opts.Items.Add(item);
                    }
                }
            }

            var card = context.Mapper.Map<AdaptiveCard>(model, PassContextOptions);
            var preview = context.Mapper.Map<ThumbnailCard>(model, PassContextOptions);

            return card.ToAttachment().ToMessagingExtensionAttachment(preview.ToAttachment());
        }
    }
}
