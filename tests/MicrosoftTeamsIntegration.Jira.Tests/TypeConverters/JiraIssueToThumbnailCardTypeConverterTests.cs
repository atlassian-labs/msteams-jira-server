using System;
using AutoMapper;
using FakeItEasy;
using Microsoft.Bot.Schema;
using MicrosoftTeamsIntegration.Jira.Models;
using MicrosoftTeamsIntegration.Jira.Models.Jira;
using MicrosoftTeamsIntegration.Jira.Models.Jira.Issue;
using MicrosoftTeamsIntegration.Jira.Settings;
using MicrosoftTeamsIntegration.Jira.TypeConverters;
using Newtonsoft.Json.Linq;
using Xunit;

namespace MicrosoftTeamsIntegration.Jira.Tests.TypeConverters
{
    public class JiraIssueToThumbnailCardTypeConverterTests
    {
        private readonly AppSettings _appSettings = new AppSettings();
        private readonly ResolutionContext _resolutionContext = A.Fake<ResolutionContext>();

        [Fact]
        public void Convert_ReturnsNull_WhenJiraIssueNull()
        {
            var model = new BotAndMessagingExtensionJiraIssue()
            {
                JiraIssue = null
            };

            var converter = new JiraIssueToThumbnailCardTypeConverter(_appSettings);

            var result = converter.Convert(model, null, _resolutionContext);

            Assert.Null(result);
        }

        [Fact]
        public void Convert()
        {
            _appSettings.BaseUrl = "https://test.com";

            var expectedText =
                "<div style=\"margin-bottom: 6px;\"><span style=\"font-size: 12px; font-weight: 600;\">Assignee User | TEST_DATE</span></div><span style=\"margin: 2px;\"><img src=\"https://test.com/assets/issue-type-icons-small/unknown.png?format=png&size=xsmall\" style=\"width: 16px; height: 16px;\"></span><span style=\"margin: 2px;\"><img style=\"display:block;\" src=\"https://test.com/assets/priority-icons/medium.png?format=png&size=xsmall\" width=\"16\" height=\"16\"></span><span><strong style=\"font-size:12px;white-space: nowrap;overflow: hidden;text-overflow: ellipsis;width: 100px;display: inline-block;vertical-align: middle;\"> | Status</strong></span>".Replace("TEST_DATE", new DateTime(0001, 1, 1).ToShortDateString());

            var expectedTitle =
                "<a style=\"font-size: 14px; font-weight: 600;\" href=\"https://test.atlassian.net/browse/Key\" target=\"_blank\">Key: Summary</a>";

            var isQueryLinkRequest = new object();
            var previewIconPath = new object();

            A.CallTo(() => _resolutionContext.Options.Items.TryGetValue("isQueryLinkRequest", out isQueryLinkRequest))
                .Returns(true)
                .AssignsOutAndRefParameters("true");

            A.CallTo(() => _resolutionContext.Options.Items.TryGetValue("previewIconPath", out previewIconPath))
                .Returns(true)
                .AssignsOutAndRefParameters("iconUrl");

            var model = new BotAndMessagingExtensionJiraIssue()
            {
                JiraInstanceUrl = "https://test.atlassian.net",
                JiraIssue = new JiraIssue()
                {
                    Key = "Key",
                    FieldsRaw = new JObject(),
                    Fields = new JiraIssueFields()
                    {
                        Summary = "Summary",
                        Assignee = new JiraUser() { DisplayName = "Assignee User" },
                        Type = new JiraIssueType() { Name = "TypeName"},
                        Priority = new JiraIssuePriority() { Name = "PriorityName"},
                        Status = new JiraIssueStatus() { Name = "Status"},
                    }
                }
            };

            var converter = new JiraIssueToThumbnailCardTypeConverter(_appSettings);

            var result = converter.Convert(model, null, _resolutionContext);

            Assert.NotNull(result);
            Assert.IsType<ThumbnailCard>(result);
            Assert.IsType<CardImage>(result.Images[0]);
            Assert.Equal(expectedText, result.Text);
            Assert.Equal(expectedTitle, result.Title);
        }
    }
}
