using System;
using AdaptiveCards;
using AutoMapper;
using FakeItEasy;
using MicrosoftTeamsIntegration.Jira.Models;
using MicrosoftTeamsIntegration.Jira.Models.Jira;
using MicrosoftTeamsIntegration.Jira.Models.Jira.Issue;
using MicrosoftTeamsIntegration.Jira.Settings;
using MicrosoftTeamsIntegration.Jira.TypeConverters;
using Xunit;

namespace MicrosoftTeamsIntegration.Jira.Tests.TypeConverters
{
    public class JiraIssueToAdaptiveCardTypeConverterTests
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

            var converter = new JiraIssueToAdaptiveCardTypeConverter(_appSettings);

            var result = converter.Convert(model, new AdaptiveCard(), _resolutionContext);

            Assert.Null(result);
        }

        [Fact]
        public void Convert()
        {
            _appSettings.BaseUrl = "https://test.com";
            var model = new BotAndMessagingExtensionJiraIssue()
            {
                IsGroupConversation = false,
                JiraInstanceUrl = "https://test.atlassian.net",
                JiraIssue = new JiraIssue()
                {
                    Id = "id",
                    Key = "Key",
                    Fields = new JiraIssueFields()
                    {
                        Type = new JiraIssueType()
                        {
                            Name = "TypeName",
                        },
                        Status = new JiraIssueStatus()
                        {
                            Name = "Status Name"
                        },
                        Priority = new JiraIssuePriority()
                        {
                            Name = "Priority Name"
                        },
                        Updated = DateTime.Now,
                        Watches = new JiraIssueWatches()
                        {
                            IsWatching = true
                        },
                        Project = new JiraProject()
                        {
                            Name = "Test Project"
                        },
                        Reporter = new JiraUser()
                        {
                            DisplayName = "Reporter Name"
                        }
                    }
                }
            };

            var converter = new JiraIssueToAdaptiveCardTypeConverter(_appSettings);

            var result = converter.Convert(model, null, _resolutionContext);

            Assert.NotNull(result);
            Assert.IsType<AdaptiveCard>(result);
            Assert.IsType<AdaptiveShowCardAction>(result.Actions[0]);
            Assert.Equal("Comment", result.Actions[0].Title);

            Assert.IsType<AdaptiveSubmitAction>(result.Actions[1]);
            Assert.Equal("Edit", result.Actions[1].Title);

            Assert.IsType<AdaptiveSubmitAction>(result.Actions[2]);
            Assert.Equal("Vote", result.Actions[2].Title);

            Assert.IsType<AdaptiveSubmitAction>(result.Actions[3]);
            Assert.Equal("Log Work", result.Actions[3].Title);
        }

        [Fact]
        public void Convert_WhenIsMessagingExstension()
        {
            _appSettings.BaseUrl = "https://test.com";
            var model = new BotAndMessagingExtensionJiraIssue()
            {
                IsMessagingExtension = true,
                JiraInstanceUrl = "https://test.atlassian.net",
                JiraIssue = new JiraIssue()
                {
                    Id = "id",
                    Key = "Key",
                    Fields = new JiraIssueFields()
                    {
                        Type = new JiraIssueType()
                        {
                            Name = "TypeName",
                        },
                        Status = new JiraIssueStatus()
                        {
                            Name = "Status Name"
                        },
                        Priority = new JiraIssuePriority()
                        {
                            Name = "Priority Name"
                        },
                        Updated = DateTime.Now,
                        Watches = new JiraIssueWatches()
                        {
                            IsWatching = true
                        },
                        Project = new JiraProject()
                        {
                            Name = "Test Project"
                        },
                        Reporter = new JiraUser()
                        {
                            DisplayName = "Reporter Name"
                        }
                    }
                }
            };

            var converter = new JiraIssueToAdaptiveCardTypeConverter(_appSettings);

            var result = converter.Convert(model, null, _resolutionContext);

            Assert.NotNull(result);
            Assert.IsType<AdaptiveCard>(result);
            Assert.IsType<AdaptiveShowCardAction>(result.Actions[0]);
            Assert.Equal("Comment", result.Actions[0].Title);

            Assert.IsType<AdaptiveOpenUrlAction>(result.Actions[1]);
            Assert.Equal("View in Jira", result.Actions[1].Title);
        }
    }
}
