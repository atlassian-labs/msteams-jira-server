using MicrosoftTeamsIntegration.Jira.Models.Jira.Transition;
using Xunit;

namespace MicrosoftTeamsIntegration.Jira.Tests.Models.Jira.Transition;

public class JiraTransitionFieldTests
{
    [Fact]
    public void JiraTransitionField_ShouldInitializeProperties()
    {
        // Arrange
        var colourRequired = true;
        var schema = new JiraFieldSchema();
        var name = "Test Name";
        var key = "Test Key";
        var hasDefaultValue = true;
        var operations = new[] { "create", "update" };
        var allowedValues = new[] { "Value1", "Value2" };
        var defaultValue = "Default Value";

        // Act
        var field = new JiraTransitionField
        {
            ColourRequired = colourRequired,
            Schema = schema,
            Name = name,
            Key = key,
            HasDefaultValue = hasDefaultValue,
            Operations = operations,
            AllowedValues = allowedValues,
            DefaultValue = defaultValue
        };

        // Assert
        Assert.Equal(colourRequired, field.ColourRequired);
        Assert.Equal(schema, field.Schema);
        Assert.Equal(name, field.Name);
        Assert.Equal(key, field.Key);
        Assert.Equal(hasDefaultValue, field.HasDefaultValue);
        Assert.Equal(operations, field.Operations);
        Assert.Equal(allowedValues, field.AllowedValues);
        Assert.Equal(defaultValue, field.DefaultValue);
    }

    [Fact]
    public void JiraTransitionFields_ShouldInitializeProperties()
    {
        // Arrange
        var summaryField = new JiraTransitionField
        {
            ColourRequired = true,
            Name = "Summary",
            Key = "summary",
            HasDefaultValue = true,
            Operations = new[] { "create", "update" },
            AllowedValues = new[] { "Value1", "Value2" },
            DefaultValue = "Default Summary"
        };

        var colourField = new JiraTransitionField
        {
            ColourRequired = true,
            Name = "Colour",
            Key = "colour",
            HasDefaultValue = true,
            Operations = new[] { "create", "update" },
            AllowedValues = new[] { "Red", "Blue" },
            DefaultValue = "Red"
        };

        // Act
        var fields = new JiraTransitionFields
        {
            Summary = summaryField,
            Colour = colourField
        };

        // Assert
        Assert.Equal(summaryField, fields.Summary);
        Assert.Equal(colourField, fields.Colour);
    }
}
