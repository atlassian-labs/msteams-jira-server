using System;
using MicrosoftTeamsIntegration.Jira.Extensions;
using MicrosoftTeamsIntegration.Jira.Models.Jira;
using MicrosoftTeamsIntegration.Jira.Models.Jira.Issue;
using Xunit;

namespace MicrosoftTeamsIntegration.Jira.Tests.Extensions
{
    public class JiraIssueExtensionsTests
    {
        [Fact]
        public void AllowsToVote_ShouldReturnTrue_WhenUserIsNotReporterAndIssueIsNotResolved()
        {
            // Arrange
            var jiraIssue = new JiraIssue
            {
                Fields = new JiraIssueFields
                {
                    Reporter = new JiraUser { Name = "reporter" },
                    ResolutionDate = null
                }
            };

            // Act
            var result = jiraIssue.AllowsToVote("user");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void AllowsToVote_ShouldReturnFalse_WhenUserIsReporter()
        {
            // Arrange
            var jiraIssue = new JiraIssue
            {
                Fields = new JiraIssueFields
                {
                    Reporter = new JiraUser { Name = "user" },
                    ResolutionDate = null
                }
            };

            // Act
            var result = jiraIssue.AllowsToVote("user");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void AllowsToVote_ShouldReturnFalse_WhenIssueIsResolved()
        {
            // Arrange
            var jiraIssue = new JiraIssue
            {
                Fields = new JiraIssueFields
                {
                    Reporter = new JiraUser { Name = "reporter" },
                    ResolutionDate = DateTime.Now
                }
            };

            // Act
            var result = jiraIssue.AllowsToVote("user");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsResolved_ShouldReturnTrue_WhenResolutionDateIsNotNull()
        {
            // Arrange
            var jiraIssue = new JiraIssue
            {
                Fields = new JiraIssueFields
                {
                    ResolutionDate = DateTime.Now
                }
            };

            // Act
            var result = jiraIssue.IsResolved();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsResolved_ShouldReturnFalse_WhenResolutionDateIsNull()
        {
            // Arrange
            var jiraIssue = new JiraIssue
            {
                Fields = new JiraIssueFields
                {
                    ResolutionDate = null
                }
            };

            // Act
            var result = jiraIssue.IsResolved();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsUserReporter_ShouldReturnTrue_WhenUserIsReporter()
        {
            // Arrange
            var jiraIssue = new JiraIssue
            {
                Fields = new JiraIssueFields
                {
                    Reporter = new JiraUser { Name = "user" }
                }
            };

            // Act
            var result = jiraIssue.IsUserReporter("user");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsUserReporter_ShouldReturnFalse_WhenUserIsNotReporter()
        {
            // Arrange
            var jiraIssue = new JiraIssue
            {
                Fields = new JiraIssueFields
                {
                    Reporter = new JiraUser { Name = "reporter" }
                }
            };

            // Act
            var result = jiraIssue.IsUserReporter("user");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsAssignedToUser_ShouldReturnTrue_WhenUserIsAssignee()
        {
            // Arrange
            var jiraIssue = new JiraIssue
            {
                Fields = new JiraIssueFields
                {
                    Assignee = new JiraUser { Name = "user" }
                }
            };

            // Act
            var result = jiraIssue.IsAssignedToUser("user");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsAssignedToUser_ShouldReturnFalse_WhenUserIsNotAssignee()
        {
            // Arrange
            var jiraIssue = new JiraIssue
            {
                Fields = new JiraIssueFields
                {
                    Assignee = new JiraUser { Name = "assignee" }
                }
            };

            // Act
            var result = jiraIssue.IsAssignedToUser("user");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsVotedByUser_ShouldReturnTrue_WhenUserHasVoted()
        {
            // Arrange
            var jiraIssue = new JiraIssue
            {
                Fields = new JiraIssueFields
                {
                    Votes = new JiraIssueVotes { HasVoted = true }
                }
            };

            // Act
            var result = jiraIssue.IsVotedByUser();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsVotedByUser_ShouldReturnFalse_WhenUserHasNotVoted()
        {
            // Arrange
            var jiraIssue = new JiraIssue
            {
                Fields = new JiraIssueFields
                {
                    Votes = new JiraIssueVotes { HasVoted = false }
                }
            };

            // Act
            var result = jiraIssue.IsVotedByUser();

            // Assert
            Assert.False(result);
        }
    }
}
