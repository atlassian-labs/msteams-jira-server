using System.Collections.Generic;
using System.Text;
using AutoMapper;
using Microsoft.Bot.Schema;
using MicrosoftTeamsIntegration.Jira.Extensions;
using MicrosoftTeamsIntegration.Jira.Helpers;
using MicrosoftTeamsIntegration.Jira.Models;
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

        public ThumbnailCard Convert(BotAndMessagingExtensionJiraIssue model, ThumbnailCard card, ResolutionContext context)
        {
            if (model?.JiraIssue is null)
            {
                return null;
            }

            if (card is null)
            {
                card = new ThumbnailCard();
            }

            model.JiraIssue.SetJiraIssueIconUrl(_appSettings.BaseUrl, JiraIconSize.Small);
            model.JiraIssue.SetJiraIssuePriorityIconUrl(_appSettings.BaseUrl);

            var mappingOptions = context.ExtractMappingOptions();
            card.Title = JiraIssueTemplateHelper.GetTitle(model.JiraInstanceUrl, model.JiraIssue);
            card.Text = GetPreviewText(model.JiraIssue, model.EpicFieldName, mappingOptions.IsQueryLinkRequest);

            if (mappingOptions.IsQueryLinkRequest && !string.IsNullOrWhiteSpace(mappingOptions.PreviewIconPath))
            {
                card.Images = new List<CardImage>
                {
                    new CardImage(mappingOptions.PreviewIconPath)
                };
            }

            return card;
        }

        private static string GetPreviewText(JiraIssue jiraIssue, string epicFieldName, bool isQueryLinkRequest)
        {
            var text = new StringBuilder();
            JiraIssueTemplateHelper.AppendAssigneeAndUpdatedFields(text, jiraIssue);
            JiraIssueTemplateHelper.AppendEpicField(text, epicFieldName, jiraIssue);
            JiraIssueTemplateHelper.AppendIssueTypeField(text, jiraIssue);
            JiraIssueTemplateHelper.AppendPriorityField(text, jiraIssue);
            JiraIssueTemplateHelper.AppendStatusField(text, jiraIssue, isQueryLinkRequest);

            return text.ToString();
        }
    }
}
