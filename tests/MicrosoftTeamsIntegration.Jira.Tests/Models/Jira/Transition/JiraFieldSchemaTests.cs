using MicrosoftTeamsIntegration.Jira.Models.Jira.Transition;
using Xunit;

namespace MicrosoftTeamsIntegration.Jira.Tests.Models.Jira.Transition;

public class JiraFieldSchemaTests
{
    [Fact]
    public void JiraFieldSchema_ShouldInitializeProperties()
    {
        // Arrange
        var type = "string";
        var items = "item1";
        var custom = "customField";
        var customId = 12345L;

        // Act
        var schema = new JiraFieldSchema
        {
            Type = type,
            Items = items,
            Custom = custom,
            CustomId = customId
        };

        // Assert
        Assert.Equal(type, schema.Type);
        Assert.Equal(items, schema.Items);
        Assert.Equal(custom, schema.Custom);
        Assert.Equal(customId, schema.CustomId);
    }
}
