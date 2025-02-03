using MicrosoftTeamsIntegration.Jira.Models.Jira;
using Xunit;

namespace MicrosoftTeamsIntegration.Jira.Tests.Models.Jira;

public class JiraAvatarUrlsTests
{
    [Fact]
    public void JiraAvatarUrls_ShouldInitializeProperties()
    {
        // Arrange
        var the48X48 = "https://jira.example.com/avatar/48x48.png";
        var the24X24 = "https://jira.example.com/avatar/24x24.png";
        var the16X16 = "https://jira.example.com/avatar/16x16.png";
        var the32X32 = "https://jira.example.com/avatar/32x32.png";

        // Act
        var avatarUrls = new JiraAvatarUrls
        {
            The48X48 = the48X48,
            The24X24 = the24X24,
            The16X16 = the16X16,
            The32X32 = the32X32
        };

        // Assert
        Assert.Equal(the48X48, avatarUrls.The48X48);
        Assert.Equal(the24X24, avatarUrls.The24X24);
        Assert.Equal(the16X16, avatarUrls.The16X16);
        Assert.Equal(the32X32, avatarUrls.The32X32);
    }
}
