using MicrosoftTeamsIntegration.Jira.Models.Jira;
using Xunit;

namespace MicrosoftTeamsIntegration.Jira.Tests.Models.Jira;

public class JiraUserApplicationRolesTests
{
    [Fact]
    public void JiraUserApplicationRoles_ShouldInitializeProperties()
    {
        // Arrange
        var size = 10L;
        var items = new object[] { "item1", "item2" };

        // Act
        var userApplicationRoles = new JiraUserApplicationRoles
        {
            Size = size,
            Items = items
        };

        // Assert
        Assert.Equal(size, userApplicationRoles.Size);
        Assert.Equal(items, userApplicationRoles.Items);
    }
}
