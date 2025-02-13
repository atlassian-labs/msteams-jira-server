using MicrosoftTeamsIntegration.Jira.Models.Jira;
using Xunit;

namespace MicrosoftTeamsIntegration.Jira.Tests.Models.Jira;

public class JiraAuthorTests
{
    [Fact]
    public void JiraAuthor_ShouldInitializeProperties()
    {
        // Arrange
        var self = "https://jira.example.com/author/1";
        var name = "Test Name";
        var displayName = "Test Display Name";
        var active = true;

        // Act
        var author = new JiraAuthor
        {
            Self = self,
            Name = name,
            DisplayName = displayName,
            Active = active
        };

        // Assert
        Assert.Equal(self, author.Self);
        Assert.Equal(name, author.Name);
        Assert.Equal(displayName, author.DisplayName);
        Assert.Equal(active, author.Active);
    }
}
