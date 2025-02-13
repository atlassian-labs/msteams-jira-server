using MicrosoftTeamsIntegration.Jira.Models.Dto;
using Xunit;

namespace MicrosoftTeamsIntegration.Jira.Tests.Models.Dto;

public class JiraIssueDtoTests
{
    [Fact]
    public void JiraIssueDto_ShouldInitializeProperties()
    {
        // Arrange
        var id = "test-id";
        var key = "test-key";
        var self = "https://example.com/self";

        // Act
        var issueDto = new JiraIssueDto
        {
            Id = id,
            Key = key,
            Self = self
        };

        // Assert
        Assert.Equal(id, issueDto.Id);
        Assert.Equal(key, issueDto.Key);
        Assert.Equal(self, issueDto.Self);
    }
}
