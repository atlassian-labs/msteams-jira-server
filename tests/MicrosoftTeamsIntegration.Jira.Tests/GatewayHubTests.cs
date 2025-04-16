using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Connections.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using MicrosoftTeamsIntegration.Jira.Services.Interfaces;
using MicrosoftTeamsIntegration.Jira.Services.SignalR.Interfaces;
using Moq;
using Xunit;

namespace MicrosoftTeamsIntegration.Jira.Tests;

public class GatewayHubTests : IDisposable
{
    private readonly Mock<IDatabaseService> _databaseServiceMock;
    private readonly Mock<ISignalRService> _signalRServiceMock;
    private readonly Mock<HubCallerContext> _contextMock;
    private readonly Mock<IGroupManager> _groupsMock;
    private readonly GatewayHub _gatewayHub;

    private bool _disposed;

    public GatewayHubTests()
    {
        _databaseServiceMock = new Mock<IDatabaseService>();
        var loggerMock = new Mock<ILogger<GatewayHub>>();
        _signalRServiceMock = new Mock<ISignalRService>();
        _contextMock = new Mock<HubCallerContext>();
        _groupsMock = new Mock<IGroupManager>();

        _gatewayHub = new GatewayHub(
            _databaseServiceMock.Object,
            loggerMock.Object,
            _signalRServiceMock.Object)
        {
            Context = _contextMock.Object,
            Groups = _groupsMock.Object
        };
    }

    [Fact]
    public async Task Callback_ShouldCallSignalRServiceCallback()
    {
        // Arrange
        var identifier = Guid.NewGuid();
        var response = "response-message";

        // Act
        await _gatewayHub.Callback(identifier, response);

        // Assert
        _signalRServiceMock.Verify(s => s.Callback(identifier, response), Times.Once);
    }

    [Fact]
    public async Task Broadcast_ShouldCallSignalRServiceBroadcast()
    {
        // Arrange
        var identifier = Guid.NewGuid();
        var response = "response-message";

        // Act
        await _gatewayHub.Broadcast(identifier, response);

        // Assert
        _signalRServiceMock.Verify(s => s.Broadcast(identifier, response), Times.Once);
    }

    [Fact]
    public async Task OnConnectedAsync_ShouldAddToGroupAndUpdateDatabase()
    {
        // Arrange
        var connectionId = "test-connection-id";
        var jiraId = "test-jira-id";
        var jiraInstanceUrl = "http://jira-instance.com";
        var version = "1.0.0";
        var groupName = "test-group";

        var httpContextMock = new Mock<HttpContext>();
        httpContextMock.Setup(h => h.Request.QueryString)
            .Returns(new QueryString($"?atlasId={jiraId}&atlasUrl={jiraInstanceUrl}&pluginVersion={version}&groupName={groupName}"));
        var httpContextFeatureMock = new Mock<IHttpContextFeature>();
        httpContextFeatureMock.Setup(f => f.HttpContext).Returns(httpContextMock.Object);

        var featuresMock = new Mock<IFeatureCollection>();
        featuresMock.Setup(f => f.Get<IHttpContextFeature>()).Returns(httpContextFeatureMock.Object);

        _contextMock.Setup(c => c.ConnectionId).Returns(connectionId);
        _contextMock.Setup(c => c.Features).Returns(featuresMock.Object);

        // Act
        await _gatewayHub.OnConnectedAsync();

        // Assert
        _databaseServiceMock.Verify(d => d.CreateOrUpdateJiraServerAddonSettings(jiraId, jiraInstanceUrl, connectionId, version), Times.Once);
        _groupsMock.Verify(g => g.AddToGroupAsync(connectionId, groupName, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task OnDisconnectedAsync_ShouldDeleteDatabaseEntry()
    {
        // Arrange
        var connectionId = "test-connection-id";
        _contextMock.Setup(c => c.ConnectionId).Returns(connectionId);

        // Act
        await _gatewayHub.OnDisconnectedAsync(null);

        // Assert
        _databaseServiceMock.Verify(d => d.DeleteJiraServerAddonSettingsByConnectionId(connectionId), Times.Once);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _gatewayHub.Dispose();
            }

            _disposed = true;
        }
    }
}
