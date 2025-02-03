using System;
using MicrosoftTeamsIntegration.Jira.Models.Dto;
using MicrosoftTeamsIntegration.Jira.Models.Jira;
using MicrosoftTeamsIntegration.Jira.Models.Jira.Issue;
using Xunit;

namespace MicrosoftTeamsIntegration.Jira.Tests.Models.Dto;

public class JiraIssueFieldsDtoTests
{
    [Fact]
    public void JiraIssueFieldsDto_ShouldInitializeProperties()
    {
        // Arrange
        var issueType = new JiraIssueType { Name = "Bug" };
        var created = DateTime.UtcNow;
        var priority = new JiraIssuePriority { Name = "High" };
        var assignee = new JiraUser { DisplayName = "John Doe" };
        var updated = DateTime.UtcNow;
        var status = new JiraIssueStatus { Name = "Open" };
        var summary = "Issue summary";
        var creator = new JiraUser { DisplayName = "Jane Doe" };
        var reporter = new JiraUser { DisplayName = "Reporter Name" };

        // Act
        var issueFieldsDto = new JiraIssueFieldsDto
        {
            Type = issueType,
            Created = created,
            Priority = priority,
            Assignee = assignee,
            Updated = updated,
            Status = status,
            Summary = summary,
            Creator = creator,
            Reporter = reporter
        };

        // Assert
        Assert.Equal(issueType, issueFieldsDto.Type);
        Assert.Equal(created, issueFieldsDto.Created);
        Assert.Equal(priority, issueFieldsDto.Priority);
        Assert.Equal(assignee, issueFieldsDto.Assignee);
        Assert.Equal(updated, issueFieldsDto.Updated);
        Assert.Equal(status, issueFieldsDto.Status);
        Assert.Equal(summary, issueFieldsDto.Summary);
        Assert.Equal(creator, issueFieldsDto.Creator);
        Assert.Equal(reporter, issueFieldsDto.Reporter);
    }
}
