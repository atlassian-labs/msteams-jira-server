using System;
using System.Linq;
using AdaptiveCards;
using FakeItEasy;
using MicrosoftTeamsIntegration.Jira.Models.Interfaces;
using MicrosoftTeamsIntegration.Jira.Models.Notifications;
using MicrosoftTeamsIntegration.Jira.TypeConverters;
using Xunit;

namespace MicrosoftTeamsIntegration.Jira.Tests.TypeConverters
{
    public class NotificationMessageToAdaptiveCardConverterTests
    {
        private readonly NotificationMessageToAdaptiveCardConverter _converter;
        private readonly IResolutionContext _resolutionContext;
        private readonly NotificationMessage _message;

        public NotificationMessageToAdaptiveCardConverterTests()
        {
            _converter = new NotificationMessageToAdaptiveCardConverter();
            _resolutionContext = A.Fake<IResolutionContext>();

            _message = new NotificationMessage
            {
                EventType = "issue_created",
                User = new NotificationUser { Name = "Test User", AvatarUrl = new Uri("https://test.com/avatar.png") },
                Issue = new NotificationIssue
                {
                    Key = "TEST-123",
                    Summary = "Test Issue",
                    Type = "Bug",
                    Status = "To Do",
                    Self = new Uri("https://test.com/browse/TEST-123")
                }
            };
        }

        [Fact]
        public void Convert_IssueCreated_ReturnsCorrectCard()
        {
            // Arrange
            var payload = new NotificationMessageCardPayload(_message)
            {
                EventType = "issue_created",
                User = new NotificationUser { Name = "Test User", AvatarUrl = new Uri("https://test.com/avatar.png") },
                Issue = new NotificationIssue
                {
                    Key = "TEST-123",
                    Summary = "Test Issue",
                    Type = "Bug",
                    Status = "To Do",
                    Self = new Uri("https://test.com/browse/TEST-123")
                }
            };

            // Act
            var result = _converter.Convert(payload, null, _resolutionContext);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(new AdaptiveSchemaVersion(1, 4), result.Version);
            Assert.Contains("Test User **created** this issue:", GetTextBlockText(result));
            Assert.Contains("Open in Jira", GetActionTitle(result, "Open in Jira"));
            Assert.Contains("Comment", GetActionTitle(result, "Comment"));
            Assert.Contains("Edit", GetActionTitle(result, "Edit"));
        }

        [Fact]
        public void Convert_CommentCreated_ReturnsCorrectCard()
        {
            // Arrange
            var payload = new NotificationMessageCardPayload(_message)
            {
                EventType = "comment_created",
                User = new NotificationUser { Name = "Test User", AvatarUrl = new Uri("https://test.com/avatar.png") },
                Issue = new NotificationIssue
                {
                    Key = "TEST-123",
                    Summary = "Test Issue",
                    Type = "Bug",
                    Status = "In Progress",
                    Self = new Uri("https://test.com/browse/TEST-123")
                },
                Comment = new NotificationComment { Content = "Test comment content" }
            };

            // Act
            var result = _converter.Convert(payload, null, _resolutionContext);

            // Assert
            Assert.NotNull(result);
            Assert.Contains("Test User **commented** on this issue:", GetTextBlockText(result));
        }

        [Fact]
        public void Convert_CommentMention_ReturnsCorrectCard()
        {
            // Arrange
            var payload = new NotificationMessageCardPayload(_message)
            {
                EventType = "comment_created",
                IsMention = true,
                User = new NotificationUser { Name = "Test User", AvatarUrl = new Uri("https://test.com/avatar.png") },
                Issue = new NotificationIssue
                {
                    Key = "TEST-123",
                    Summary = "Test Issue",
                    Type = "Bug",
                    Status = "In Progress",
                    Self = new Uri("https://test.com/browse/TEST-123")
                },
                Comment = new NotificationComment { Content = "Test mention content" }
            };

            // Act
            var result = _converter.Convert(payload, null, _resolutionContext);

            // Assert
            Assert.NotNull(result);
            Assert.Contains("Test User **mentioned** you in a comment:", GetTextBlockText(result));
        }

        [Fact]
        public void Convert_IssueUpdated_ReturnsCorrectCard()
        {
            // Arrange
            var payload = new NotificationMessageCardPayload(_message)
            {
                EventType = "issue_updated",
                User = new NotificationUser { Name = "Test User", AvatarUrl = new Uri("https://test.com/avatar.png") },
                Issue = new NotificationIssue
                {
                    Key = "TEST-123",
                    Summary = "Test Issue",
                    Type = "Bug",
                    Status = "Done",
                    Self = new Uri("https://test.com/browse/TEST-123")
                },
                Changelog =
                [
                    new NotificationChangelog
                    {
                        Field = "Status",
                        From = "In Progress",
                        To = "Done"
                    }

                ]
            };

            // Act
            var result = _converter.Convert(payload, null, _resolutionContext);

            // Assert
            Assert.NotNull(result);
            Assert.Contains("Test User updated the **status** on this issue:", GetTextBlockText(result));
        }

        [Fact]
        public void Convert_PersonalNotification_IncludesVoteAndLogTimeActions()
        {
            // Arrange
            var payload = new NotificationMessageCardPayload(_message)
            {
                EventType = "issue_updated",
                IsPersonalNotification = true,
                User = new NotificationUser { Name = "Test User", AvatarUrl = new Uri("https://test.com/avatar.png") },
                Issue = new NotificationIssue
                {
                    Key = "TEST-123",
                    Summary = "Test Issue",
                    Type = "Bug",
                    Status = "In Progress",
                    Self = new Uri("https://test.com/browse/TEST-123")
                }
            };

            // Act
            var result = _converter.Convert(payload, null, _resolutionContext);

            // Assert
            Assert.NotNull(result);
            Assert.Contains("Vote", GetActionTitle(result, "Vote"));
            Assert.Contains("Log Work", GetActionTitle(result, "Log Work"));
        }

        [Fact]
        public void Convert_TruncatesLongComment()
        {
            // Arrange
            var longComment = new string('a', 600); // Exceeds the 500-character limit
            var payload = new NotificationMessageCardPayload(_message)
            {
                EventType = "comment_created",
                User = new NotificationUser { Name = "Test User", AvatarUrl = new Uri("https://test.com/avatar.png") },
                Issue = new NotificationIssue
                {
                    Key = "TEST-123",
                    Summary = "Test Issue",
                    Type = "Bug",
                    Status = "In Progress",
                    Self = new Uri("https://test.com/browse/TEST-123")
                },
                Comment = new NotificationComment { Content = longComment }
            };

            // Act
            var result = _converter.Convert(payload, null, _resolutionContext);

            // Assert
            Assert.NotNull(result);
            var commentText = GetTextBodyText(result);
            Assert.Equal(500, commentText.Length);
        }

        private static string GetTextBlockText(AdaptiveCard card)
        {
            var titleContainer = card.Body[0] as AdaptiveContainer;
            var titleColumnSet = titleContainer?.Items[0] as AdaptiveColumnSet;
            var titleAdaptiveColumn = titleColumnSet?.Columns[1];
            var titleTextBlock = titleAdaptiveColumn?.Items[0] as AdaptiveTextBlock;

            return titleTextBlock?.Text;
        }

        private static string GetTextBodyText(AdaptiveCard card)
        {
            var bodyContainer = card.Body[2] as AdaptiveColumnSet;
            var titleColumn = bodyContainer?.Columns[0];
            var titleAdaptiveColumn = titleColumn?.Items[0] as AdaptiveTextBlock;

            return titleAdaptiveColumn?.Text;
        }

        private static string GetActionTitle(AdaptiveCard card, string expectedTitle)
        {
            var actions = card.Body[3] as AdaptiveActionSet;
            return (actions?.Actions)?.FirstOrDefault(a => a.Title == expectedTitle)?.Title;
        }
    }
}
