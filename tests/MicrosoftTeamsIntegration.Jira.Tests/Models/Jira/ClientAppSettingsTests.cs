using MicrosoftTeamsIntegration.Jira.Models;
using Xunit;

namespace MicrosoftTeamsIntegration.Jira.Tests.Models.Jira;

public class ClientAppSettingsTests
{
    [Fact]
    public void ClientAppSettings_ShouldInitializeProperties()
    {
        // Arrange
        var clientId = "test-client-id";
        var baseUrl = "https://example.com";
        var microsoftLoginBaseUrl = "https://login.microsoftonline.com";
        var instrumentationKey = "test-instrumentation-key";
        var analyticsEnvironment = "test-environment";

        // Act
        var settings = new ClientAppSettings(
            clientId,
            baseUrl,
            microsoftLoginBaseUrl,
            instrumentationKey,
            analyticsEnvironment);

        // Assert
        Assert.Equal(clientId, settings.ClientId);
        Assert.Equal(baseUrl, settings.BaseUrl);
        Assert.Equal(microsoftLoginBaseUrl, settings.MicrosoftLoginBaseUrl);
        Assert.Equal(instrumentationKey, settings.InstrumentationKey);
        Assert.Equal(analyticsEnvironment, settings.AnalyticsEnvironment);
        Assert.NotNull(settings.Version);
    }
}
