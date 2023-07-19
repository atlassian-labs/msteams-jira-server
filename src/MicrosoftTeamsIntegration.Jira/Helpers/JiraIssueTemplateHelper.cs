using System.Collections.Generic;
using System.Text;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Schema.Teams;
using MicrosoftTeamsIntegration.Artifacts.Extensions;
using MicrosoftTeamsIntegration.Jira.Dialogs;
using MicrosoftTeamsIntegration.Jira.Models;
using MicrosoftTeamsIntegration.Jira.Models.Jira.Issue;

namespace MicrosoftTeamsIntegration.Jira.Helpers
{
    public static class JiraIssueTemplateHelper
    {
        public static void AppendAssigneeAndUpdatedFields(StringBuilder text, JiraIssue jiraIssue)
        {
            var assignee = "Unassigned";
            if (!string.IsNullOrEmpty(jiraIssue.Fields.Assignee?.DisplayName))
            {
                assignee = jiraIssue.Fields.Assignee.DisplayName;
            }

            text.Append("<div style=\"margin-bottom: 6px;\">" +
                        "<span style=\"font-size: 12px; font-weight: 600;\">");
            text.Append(assignee);
            text.Append($" | {jiraIssue.Fields.Created.ToShortDateString()}");
            text.Append("</span></div>");
        }

        public static void AppendEpicField(StringBuilder text, string epicFieldName, JiraIssue jiraIssue)
        {
            var epic = GetEpicFieldValue(epicFieldName, jiraIssue);
            if (epic.HasValue())
            {
                text.Append("<div style=\"margin-bottom: 6px;\">" +
                            "<span style=\" " +
                            "font-size: 14px; " +
                            "font-weight: 600; padding: 0 3px 0 3px; " +
                            "color:#fff; " +
                            "background-color:#ed823b; " +
                            "border-radius: 3px;\">" +
                            $"{epic}" +
                            "</span></div>");
            }
        }

        public static void AppendIssueTypeField(StringBuilder text, JiraIssue jiraIssue)
        {
            if (jiraIssue.Fields.Type == null)
            {
                return;
            }

            var iconUrl = PrepareIconUrl(jiraIssue.Fields.Type.IconUrl);
            text.Append("<span style=\"margin: 2px;\">" +
                        $"<img src=\"{iconUrl}\" style=\"width: 16px; height: 16px;\">" +
                        "</span>");
        }

        public static void AppendPriorityField(StringBuilder text, JiraIssue jiraIssue)
        {
            if (jiraIssue.Fields.Priority == null)
            {
                return;
            }

            var iconUrl = PrepareIconUrl(jiraIssue.Fields.Priority.IconUrl);
            text.Append("<span style=\"margin: 2px;\">" +
                        $"<img style=\"display:block;\" src=\"{iconUrl}\" width=\"16\" height=\"16\">" +
                        "</span>");
        }

        public static void AppendStatusField(StringBuilder text, JiraIssue jiraIssue, bool isQueryLinkRequest)
        {
            if (jiraIssue.Fields.Status == null)
            {
                return;
            }

            // status will not be placed on a separate line in case of queryLink request,
            // therefore, we need to display it with an appropriate divider
            var status = isQueryLinkRequest ?
                $" | {jiraIssue.Fields.Status.Name}"
                : jiraIssue.Fields.Status.Name;

            text.Append("<span>" +
                        "<strong style=\"" +
                        "font-size:12px;" +
                        "white-space: nowrap;" +
                        "overflow: hidden;" +
                        "text-overflow: ellipsis;" +
                        "width: 100px;" +
                        "display: inline-block;" +
                        "vertical-align: middle;\">" +
                        status +
                        "</strong>" +
                        "</span>");
        }

        public static string GetTitle(string jiraInstanceUrl, JiraIssue jiraIssue)
        {
            return "<a style=\"font-size: 14px; font-weight: 600;\" " +
                   $"href=\"{jiraInstanceUrl}/browse/{jiraIssue.Key}\" " +
                   $"target=\"_blank\">{jiraIssue.Key}: {jiraIssue.Fields.Summary}</a>";
        }

        public static O365ConnectorCard BuildO365ConnectorCard(IntegratedUser user, JiraIssue jiraIssue, string epicFieldName, List<JiraIssuePriority> priorities)
        {
            var sections = BuildCardSections(jiraIssue, epicFieldName);
            var actions = new List<O365ConnectorCardActionBase>();

            if (jiraIssue.Fields.Priority != null)
            {
                actions.Add(CreatePriorityActionCard(jiraIssue, priorities));
            }

            actions.Add(CreateSummaryActionCard(jiraIssue));
            actions.Add(CreateDescriptionActionCard(jiraIssue));
            actions.Add(new O365ConnectorCardViewAction(O365ConnectorCardViewAction.Type)
            {
                Name = "View in Jira",
                Target = new List<string>
                {
                    $"{user.JiraInstanceUrl}/browse/{jiraIssue.Key}"
                }
            });

            var card = new O365ConnectorCard
            {
                Sections = sections,
                PotentialAction = actions
            };

            return card;
        }

        private static List<O365ConnectorCardSection> BuildCardSections(JiraIssue jiraIssue, string epicFieldName)
        {
            var assignee = "Unassigned";
            if (!string.IsNullOrEmpty(jiraIssue.Fields.Assignee?.DisplayName))
            {
                assignee = jiraIssue.Fields.Assignee.DisplayName;
            }

            var activityImage = jiraIssue.Fields.Type?.IconUrl.AddOrUpdateGetParameter("format", "png").AddOrUpdateGetParameter("size", "large");
            var mainSection = new O365ConnectorCardSection(
                activityTitle: jiraIssue.Key,
                activitySubtitle: $"**{assignee}**",
                activityImage: activityImage,
                activityImageType: "avatar",
                markdown: true,
                facts: BuildListOfFacts(jiraIssue, epicFieldName));

            var sections = new List<O365ConnectorCardSection> { mainSection };

            if (jiraIssue.Fields.Summary.HasValue())
            {
                var summarySection = new O365ConnectorCardSection(
                    text: jiraIssue.Fields.Summary,
                    markdown: true);
                sections.Add(summarySection);
            }

            if (jiraIssue.Fields.Description.HasValue())
            {
                var descriptionSection = new O365ConnectorCardSection(
                    text: $"**Description:**<br/>{jiraIssue.Fields.Description}",
                    markdown: true);
                sections.Add(descriptionSection);
            }

            return sections;
        }

        private static List<O365ConnectorCardFact> BuildListOfFacts(JiraIssue jiraIssue, string epicFieldName)
        {
            void AddFieldIfAvailable<T>(List<O365ConnectorCardFact> factsList, string name, T field, string fieldName)
                where T : class
            {
                if (field != null && !string.IsNullOrEmpty(fieldName))
                {
                    factsList.Add(new O365ConnectorCardFact(name, fieldName));
                }
            }

            var epic = GetEpicFieldValue(epicFieldName, jiraIssue);
            var facts = new List<O365ConnectorCardFact>();
            AddFieldIfAvailable(facts, "Status:", jiraIssue.Fields.Status, jiraIssue.Fields.Status?.Name);
            AddFieldIfAvailable(facts, "Priority:", jiraIssue.Fields.Priority, jiraIssue.Fields.Priority?.Name);
            AddFieldIfAvailable(facts, "Type:", jiraIssue.Fields.Type, jiraIssue.Fields.Type?.Name);
            if (epic.HasValue())
            {
                facts.Add(new O365ConnectorCardFact("Epic:", epic));
            }

            return facts;
        }

        private static O365ConnectorCardActionCard CreateSummaryActionCard(JiraIssue jiraIssue)
        {
            const string summaryCardId = DialogMatchesAndCommands.O365ConnectorCardPrefix + DialogMatchesAndCommands.SummaryDialogCommand;

            return new O365ConnectorCardActionCard(
                type: O365ConnectorCardActionCard.Type,
                name: DialogTitles.SummaryTitle,
                id: summaryCardId,
                inputs: new List<O365ConnectorCardInputBase>
                {
                    new O365ConnectorCardTextInput(
                        type: O365ConnectorCardTextInput.Type,
                        id: "summary",
                        isRequired: true,
                        title: "Please enter summary",
                        value: jiraIssue.Fields.Summary,
                        isMultiline: false,
                        maxLength: 254)
                },
                actions: new List<O365ConnectorCardActionBase>
                {
                    new O365ConnectorCardHttpPOST(
                        type: O365ConnectorCardHttpPOST.Type,
                        name: "Update",
                        id: DialogMatchesAndCommands.O365ConnectorCardPostActionPrefix + DialogMatchesAndCommands.SummaryDialogCommand,
                        body: new O365ConnectorCardHttpPostBody(jiraIssue.Key, "{{summary.value}}").ToString())
                });
        }

        private static string PrepareIconUrl(string iconUrl) =>
            iconUrl
                .AddOrUpdateGetParameter("format", "png")
                .AddOrUpdateGetParameter("size", "xsmall");

        private static O365ConnectorCardActionCard CreateDescriptionActionCard(JiraIssue jiraIssue)
        {
            const string descriptionCardId = DialogMatchesAndCommands.O365ConnectorCardPrefix + DialogMatchesAndCommands.SummaryDialogCommand;

            return new O365ConnectorCardActionCard(
                type: O365ConnectorCardActionCard.Type,
                name: DialogTitles.DescriptionTitle,
                id: descriptionCardId,
                inputs: new List<O365ConnectorCardInputBase>
                {
                    new O365ConnectorCardTextInput(
                        type: O365ConnectorCardTextInput.Type,
                        id: "description",
                        isRequired: true,
                        title: "Please enter description",
                        value: jiraIssue.Fields.Description,
                        isMultiline: true)
                },
                actions: new List<O365ConnectorCardActionBase>
                {
                    new O365ConnectorCardHttpPOST(
                        type: O365ConnectorCardHttpPOST.Type,
                        name: "Update",
                        id: DialogMatchesAndCommands.O365ConnectorCardPostActionPrefix + DialogMatchesAndCommands.DescriptionDialogCommand,
                        body: new O365ConnectorCardHttpPostBody(jiraIssue.Key, "{{description.value}}").ToString())
                });
        }

        private static O365ConnectorCardActionCard CreatePriorityActionCard(JiraIssue jiraIssue, List<JiraIssuePriority> priorities)
        {
            const string priorityCardId = DialogMatchesAndCommands.O365ConnectorCardPrefix + DialogMatchesAndCommands.PriorityDialogCommand;

            var choices = new List<O365ConnectorCardMultichoiceInputChoice>();
            foreach (var priority in priorities)
            {
                choices.Add(new O365ConnectorCardMultichoiceInputChoice(priority.Name, priority.Id));
            }

            return new O365ConnectorCardActionCard(
                type: O365ConnectorCardActionCard.Type,
                name: DialogTitles.PriorityTitle,
                id: priorityCardId,
                inputs: new List<O365ConnectorCardInputBase>
                {
                    new O365ConnectorCardMultichoiceInput(
                        type: O365ConnectorCardMultichoiceInput.Type,
                        id: "priority",
                        isRequired: true,
                        title: "Choose a new level of priority",
                        value: jiraIssue.Fields.Priority.Id,
                        choices: choices,
                        style: "compact",
                        isMultiSelect: false)
                },
                actions: new List<O365ConnectorCardActionBase>
                {
                    new O365ConnectorCardHttpPOST(
                        type: O365ConnectorCardHttpPOST.Type,
                        name: "Update Priority",
                        id: DialogMatchesAndCommands.O365ConnectorCardPostActionPrefix + DialogMatchesAndCommands.PriorityDialogCommand,
                        body: new O365ConnectorCardHttpPostBody(jiraIssueKey: jiraIssue.Key, value: "{{priority.value}}").ToString())
                });
        }

        private static string GetEpicFieldValue(string epicFieldName, JiraIssue jiraIssue)
        {
            return jiraIssue.FieldsRaw.TryGetValue(epicFieldName, out var epicValueJToken) ? epicValueJToken.ToString() : string.Empty;
        }
    }
}
