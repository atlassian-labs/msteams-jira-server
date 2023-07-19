using System;
using System.Text;
using MicrosoftTeamsIntegration.Jira.Helpers;
using MicrosoftTeamsIntegration.Jira.Models.Jira;
using MicrosoftTeamsIntegration.Jira.Models.Jira.Issue;
using Newtonsoft.Json.Linq;
using Xunit;

namespace MicrosoftTeamsIntegration.Jira.Tests.Helpers
{
    public class JiraIssueTemplateHelperTests
    {
        [Fact]
        public void AppendAssigneeAndUpdatedFields_ContainsUnassigned_WhenAssigneeIsEmpty()
        {
            StringBuilder text = new StringBuilder(string.Empty);
            var jiraIssue = new JiraIssue()
            {
                Fields = new JiraIssueFields()
                {
                    Assignee = new JiraUser()
                }
            };
            JiraIssueTemplateHelper.AppendAssigneeAndUpdatedFields(text, jiraIssue);
            Assert.Contains("Unassigned", text.ToString());
        }

        [Fact]
        public void AppendAssigneeAndUpdatedFields_ContainsCorrectUserName()
        {
            StringBuilder text = new StringBuilder(string.Empty);
            var userName = "Test User";
            var jiraIssue = new JiraIssue()
            {
                Fields = new JiraIssueFields()
                {
                    Assignee = new JiraUser()
                    {
                        DisplayName = userName
                    }
                }
            };
            JiraIssueTemplateHelper.AppendAssigneeAndUpdatedFields(text, jiraIssue);
            Assert.Contains(userName, text.ToString());
        }

        [Fact]
        public void AppendAssigneeAndUpdatedFields_ContainsCorrectDateFormat()
        {
            StringBuilder text = new StringBuilder(string.Empty);
            var jiraIssue = new JiraIssue()
            {
                Fields = new JiraIssueFields()
                {
                    Created = DateTime.Now
                }
            };
            JiraIssueTemplateHelper.AppendAssigneeAndUpdatedFields(text, jiraIssue);
            Assert.Contains(DateTime.Now.ToShortDateString(), text.ToString());
        }

        [Fact]
        public void AppendEpicField_DoesNotAddEpic_WhenEmpty()
        {
            StringBuilder text = new StringBuilder(string.Empty);
            JiraIssueTemplateHelper.AppendEpicField(text, string.Empty, new JiraIssue() { FieldsRaw = new JObject() });
            Assert.Empty(text.ToString());
        }

        [Fact]
        public void AppendEpicField_AddsEpic_WhenNotEmpty()
        {
            string json = @"{
                EpicName: 'Token'
            }";

            JObject fieldRow = JObject.Parse(json);
            StringBuilder text = new StringBuilder(string.Empty);
            JiraIssueTemplateHelper.AppendEpicField(text, "EpicName", new JiraIssue() { FieldsRaw = fieldRow });
            Assert.NotEmpty(text.ToString());
        }
    }
}
