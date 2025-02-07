using Microsoft.Graph;
using MicrosoftTeamsIntegration.Artifacts.Services.GraphApi;

namespace MicrosoftTeamsIntegration.Artifacts.Tests.Services.GraphApi;

public class GraphSdkHelperTests
{
    private readonly GraphSdkHelper _graphSdkHelper;

    public GraphSdkHelperTests()
    {
        _graphSdkHelper = new GraphSdkHelper();
    }

    [Fact]
    public void GetAuthenticatedClient_ShouldReturnGraphServiceClient()
    {
        // Arrange
        var accessToken = "testAccessToken";

        // Act
        var client = _graphSdkHelper.GetAuthenticatedClient(accessToken);

        // Assert
        Assert.NotNull(client);
        Assert.IsType<GraphServiceClient>(client);
    }

    [Fact]
    public void Dispose_ShouldDisposeGraphClient()
    {
        // Act
        _graphSdkHelper.Dispose();

        // Assert
        Assert.True(_graphSdkHelper.GetType()
            .GetField("_disposed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.GetValue(_graphSdkHelper) as bool?);
    }
}
