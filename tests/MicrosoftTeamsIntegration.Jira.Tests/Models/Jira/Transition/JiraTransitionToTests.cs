using MicrosoftTeamsIntegration.Jira.Models.Jira.Issue;
using MicrosoftTeamsIntegration.Jira.Models.Jira.Transition;
using Xunit;

namespace MicrosoftTeamsIntegration.Jira.Tests.Models.Jira.Transition;

public class JiraTransitionToTests
{
    [Fact]
    public void JiraTransitionTo_ShouldInitializeProperties()
    {
        // Arrange
        var self = "https://jira.example.com/transition/1";
        var description = "Test Description";
        var iconUrl = "https://jira.example.com/icon.png";
        var name = "Test Name";
        var id = "1";
        var statusCategory = new JiraIssueStatusCategory();

        // Act
        var transitionTo = new JiraTransitionTo
        {
            Self = self,
            Description = description,
            IconUrl = iconUrl,
            Name = name,
            Id = id,
            StatusCategory = statusCategory
        };

        // Assert
        Assert.Equal(self, transitionTo.Self);
        Assert.Equal(description, transitionTo.Description);
        Assert.Equal(iconUrl, transitionTo.IconUrl);
        Assert.Equal(name, transitionTo.Name);
        Assert.Equal(id, transitionTo.Id);
        Assert.Equal(statusCategory, transitionTo.StatusCategory);
    }
}
