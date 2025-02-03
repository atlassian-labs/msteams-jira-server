using MicrosoftTeamsIntegration.Jira.Models;
using Xunit;

namespace MicrosoftTeamsIntegration.Jira.Tests.Models;

public class DialogAnalyticsEventAttributesTests
{
    [Fact]
    public void DialogAnalyticsEventAttributes_ShouldInitializeProperties()
    {
        // Arrange
        var dialogType = "test-dialog-type";
        var isGroupConversation = true;
        var errorMessage = "test-error-message";

        // Act
        var attributes = new DialogAnalyticsEventAttributes
        {
            DialogType = dialogType,
            IsGroupConversation = isGroupConversation,
            ErrorMessage = errorMessage
        };

        // Assert
        Assert.Equal(dialogType, attributes.DialogType);
        Assert.Equal(isGroupConversation, attributes.IsGroupConversation);
        Assert.Equal(errorMessage, attributes.ErrorMessage);
    }
}
