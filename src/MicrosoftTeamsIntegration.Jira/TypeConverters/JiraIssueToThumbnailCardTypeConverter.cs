using System.Collections.Generic;
using System.Text;
using AutoMapper;
using Microsoft.Bot.Schema;
using MicrosoftTeamsIntegration.Jira.Extensions;
using MicrosoftTeamsIntegration.Jira.Models;
using MicrosoftTeamsIntegration.Jira.Models.Interfaces;
using MicrosoftTeamsIntegration.Jira.Models.Jira.Issue;
using MicrosoftTeamsIntegration.Jira.Settings;

namespace MicrosoftTeamsIntegration.Jira.TypeConverters
{
    public class JiraIssueToThumbnailCardTypeConverter : ITypeConverter<BotAndMessagingExtensionJiraIssue, ThumbnailCard>
    {
        private readonly AppSettings _appSettings;
        public JiraIssueToThumbnailCardTypeConverter(AppSettings appSettings)
        {
            _appSettings = appSettings;
        }

        public ThumbnailCard Convert(BotAndMessagingExtensionJiraIssue model, ThumbnailCard card, IResolutionContext context)
        {
            if (model?.JiraIssue is null)
            {
                return null;
            }

            card ??= new ThumbnailCard();

            model.JiraIssue.SetJiraIssueIconUrl(JiraIconSize.Large);
            model.JiraIssue.SetJiraIssuePriorityIconUrl();

            var mappingOptions = context.ExtractMappingOptions();
            card.Title = $"{model.JiraIssue.Key}: {model.JiraIssue.Fields.Summary}";
            card.Subtitle = GetPreviewText(model?.JiraIssue);

            if (!string.IsNullOrEmpty(model?.JiraIssue?.Fields?.Type?.IconUrl))
            {
                card.Images = new List<CardImage>
                {
                    new CardImage(model.JiraIssue.Fields.Type.IconUrl)
                };
            }

            if (mappingOptions.IsQueryLinkRequest && !string.IsNullOrWhiteSpace(mappingOptions.PreviewIconPath))
            {
                card.Images = new List<CardImage>
                {
                    new CardImage(mappingOptions.PreviewIconPath)
                };
            }

            return card;
        }

        public ThumbnailCard Convert(BotAndMessagingExtensionJiraIssue model, ThumbnailCard card, ResolutionContext context)
        {
            return Convert(model, card, new ResolutionContextWrapper(context));
        }

        private static string GetPreviewText(JiraIssue jiraIssue)
        {
            var text = new StringBuilder();
            text.Append(jiraIssue.Fields.Status.Name);
            if (!string.IsNullOrEmpty(jiraIssue.Fields?.Assignee?.DisplayName))
            {
                text.Append(" | ");
                text.Append(jiraIssue.Fields.Assignee.DisplayName);
            }

            return text.ToString();
        }
    }
}
