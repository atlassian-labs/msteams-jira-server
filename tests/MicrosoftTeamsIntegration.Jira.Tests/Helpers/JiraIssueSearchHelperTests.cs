using System.Collections.Generic;
using Microsoft.Bot.Schema.Teams;
using MicrosoftTeamsIntegration.Jira.Helpers;
using MicrosoftTeamsIntegration.Jira.Models;
using MicrosoftTeamsIntegration.Jira.Models.Jira.Issue;
using Newtonsoft.Json.Linq;
using Xunit;

namespace MicrosoftTeamsIntegration.Jira.Tests.Helpers
{
    public class JiraIssueSearchHelperTests
    {
        [Fact]
        public void TwoEpicsInIssue_FirstEpic_Returned()
        {
            const string epicOneCustomFieldId = "123";
            const string epicTwoCustomFieldId = "456";
            var json = $"{{\"customfield_{epicOneCustomFieldId}\": {{\r\n    \"type\": \"string\",\r\n    \"custom\": \"com.pyxis.greenhopper.jira:gh-epic-label\",\r\n    \"customId\": {epicOneCustomFieldId}\r\n  }}, \"customfield_{epicTwoCustomFieldId}\": {{\r\n    \"type\": \"string\",\r\n    \"custom\": \"com.pyxis.greenhopper.jira:gh-epic-label\",\r\n    \"customId\": {epicTwoCustomFieldId}\r\n  }}}}";
            var schema = JToken.Parse(json);

            var result = JiraIssueSearchHelper.GetEpicFieldNameFromSchema(schema);

            Assert.Equal($"customfield_{epicOneCustomFieldId}", result);
        }

        [Fact]
        public void OneEpicInIssue_ItIs_Returned()
        {
            const string epicCustomField = "123";
            var json = $"{{\"customfield_{epicCustomField}\": {{\r\n    \"type\": \"string\",\r\n    \"custom\": \"com.pyxis.greenhopper.jira:gh-epic-label\",\r\n    \"customId\": {epicCustomField}\r\n  }} }}";
            var schema = JToken.Parse(json);

            var result = JiraIssueSearchHelper.GetEpicFieldNameFromSchema(schema);

            Assert.Equal($"customfield_{epicCustomField}", result);
        }

        [Fact]
        public void NoEpicsInIssue_NothingReturned()
        {
            const string epicCustomField = "123";
            var json = $"{{\"customfield_{epicCustomField}\": {{\r\n    \"type\": \"string\",\r\n    \"custom\": \"com1.pyxis2.greenhopper3.jira:gh-epic-label\",\r\n    \"customId\": {epicCustomField}\r\n  }} }}";
            var schema = JToken.Parse(json);

            var result = JiraIssueSearchHelper.GetEpicFieldNameFromSchema(schema);

            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void GetSearchJql_ReturnsEmpty_WhenIsNullOrEmpty()
        {
            var result = JiraIssueSearchHelper.GetSearchJql(null);
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void GetSearchJql_ReturnsString_WithProjectKeyAndIssueNumber()
        {
            var result = JiraIssueSearchHelper.GetSearchJql("TEST-3654645634");
            Assert.Equal("issuekey='test-3654645634'", result);
        }

        [Fact]
        public void GetSearchJql_ReturnsSummary_WithWildCard()
        {
            var result = JiraIssueSearchHelper.GetSearchJql("test");
            Assert.Equal("summary~'test*' order by lastViewed DESC, updated DESC", result);
        }

        [Fact]
        public void PrepareSearchParameter_ReturnsRequest_WhenInitialRunFalse()
        {
            var searchTerm = "TEST-3654645634";
            var query = new MessagingExtensionQuery();
            query.Parameters = new List<MessagingExtensionParameter>()
            {
                new MessagingExtensionParameter("search", searchTerm)
            };
            var result = JiraIssueSearchHelper.PrepareSearchParameter(query, false);
            Assert.Equal(JiraIssueSearchHelper.GetSearchJql(searchTerm), result.Jql);
            Assert.IsType<SearchForIssuesRequest>(result);
        }

        [Fact]
        public void PrepareSearchParameter_ReturnsDefaultJql_WhenInitialRunTrue()
        {
            var searchTerm = "TEST-3654645634";
            var query = new MessagingExtensionQuery();
            query.Parameters = new List<MessagingExtensionParameter>()
            {
                new MessagingExtensionParameter("search", searchTerm)
            };
            var result = JiraIssueSearchHelper.PrepareSearchParameter(query, true);

            Assert.NotEqual(JiraIssueSearchHelper.GetSearchJql(searchTerm), result.Jql);
            Assert.Equal("order by lastViewed DESC, updated DESC", result.Jql);
            Assert.IsType<SearchForIssuesRequest>(result);
        }

        [Fact]
        public void PrepareSearchParameter_ReturnsDefaultJql_WhenSearchValueEmpty()
        {
            var searchTerm = string.Empty;
            var query = new MessagingExtensionQuery();
            query.Parameters = new List<MessagingExtensionParameter>()
            {
                new MessagingExtensionParameter("search", searchTerm)
            };
            var result = JiraIssueSearchHelper.PrepareSearchParameter(query, true);

            Assert.NotEqual(JiraIssueSearchHelper.GetSearchJql(searchTerm), result.Jql);
            Assert.Equal("order by lastViewed DESC, updated DESC", result.Jql);
            Assert.IsType<SearchForIssuesRequest>(result);
        }

        [Fact]
        public void CreateO365ConnectorCardFromApiResponse_ReturnsNull_WhenApiNull()
        {
            var result = JiraIssueSearchHelper.CreateO365ConnectorCardFromApiResponse(new JiraIssueSearch(), new IntegratedUser(), new List<JiraIssuePriority>());
            Assert.Null(result);
        }
    }
}
