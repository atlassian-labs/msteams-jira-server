using System;
using System.Collections.Generic;
using AutoMapper;
using FakeItEasy;
using Microsoft.Bot.Schema;
using MicrosoftTeamsIntegration.Jira.Models;
using MicrosoftTeamsIntegration.Jira.Models.Interfaces;
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
        private readonly IResolutionContext _resolutionContext = A.Fake<IResolutionContext>();

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

            var expectedTitle = "Key: Summary";

            var expectedSubtitle = "Status | Assignee User";

            object isQueryLinkRequest = new object();
            object previewIconPath = new object();

            A.CallTo(() => _resolutionContext.Items.TryGetValue("isQueryLinkRequest", out isQueryLinkRequest))
                .Returns(true)
                .AssignsOutAndRefParameters("true");

            A.CallTo(() => _resolutionContext.Items.TryGetValue("previewIconPath", out previewIconPath))
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
                        Type = new JiraIssueType() { Name = "TypeName" },
                        Priority = new JiraIssuePriority() { Name = "PriorityName" },
                        Status = new JiraIssueStatus() { Name = "Status" },
                    }
                }
            };

            var converter = new JiraIssueToThumbnailCardTypeConverter(_appSettings);

            var result = converter.Convert(model, null, _resolutionContext);

            Assert.NotNull(result);
            Assert.IsType<ThumbnailCard>(result);
            Assert.IsType<CardImage>(result.Images[0]);
            Assert.Equal(expectedSubtitle, result.Subtitle, ignoreWhiteSpaceDifferences: true, ignoreLineEndingDifferences: true);
            Assert.Equal(expectedTitle, result.Title, ignoreWhiteSpaceDifferences: true, ignoreLineEndingDifferences: true);
        }
    }
}
