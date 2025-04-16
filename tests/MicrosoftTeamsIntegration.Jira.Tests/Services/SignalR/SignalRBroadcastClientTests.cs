using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MicrosoftTeamsIntegration.Jira.Services.Interfaces;
using MicrosoftTeamsIntegration.Jira.Services.SignalR;
using Moq;
using Xunit;

namespace MicrosoftTeamsIntegration.Jira.Tests.Services.SignalR;

public class SignalRBroadcastClientTests
{
    private readonly Mock<ILogger<SignalRBroadcastClient>> _loggerMock;
    private readonly Mock<IHubConnectionWrapper> _hubConnectionMock;
    private readonly SignalRBroadcastClient _signalRBroadcastClient;

    public SignalRBroadcastClientTests()
    {
        _loggerMock = new Mock<ILogger<SignalRBroadcastClient>>();
        var configurationMock = new Mock<IConfiguration>();
        _hubConnectionMock = new Mock<IHubConnectionWrapper>();

        var appBaseUrlSection = new Mock<IConfigurationSection>();
        appBaseUrlSection.Setup(s => s.Value).Returns("http://localhost:3000");
        configurationMock.Setup(c => c.GetSection("AppBaseUrl")).Returns(appBaseUrlSection.Object);
        var baseUrlSection = new Mock<IConfigurationSection>();
        baseUrlSection.Setup(s => s.Value).Returns("https://example.com");
        configurationMock.Setup(c => c.GetSection("BaseUrl")).Returns(baseUrlSection.Object);

        _signalRBroadcastClient = new SignalRBroadcastClient(_loggerMock.Object, configurationMock.Object, _hubConnectionMock.Object);
    }

    [Fact]
    public async Task StartAsync_ShouldStartHubConnection()
    {
        // Arrange
        _hubConnectionMock.Setup(h => h.StartAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        await _signalRBroadcastClient.StartAsync(CancellationToken.None);

        // Assert
        _hubConnectionMock.Verify(h => h.StartAsync(It.IsAny<CancellationToken>()), Times.Once);
        _loggerMock.Verify(
            logger => logger.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
            Times.Once);
    }

    [Fact]
    public async Task StartAsync_ShouldRetryOnFailure()
    {
        // Arrange
        int retryCount = 0;
        _hubConnectionMock.Setup(h => h.StartAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Connection failed"))
            .Callback(() => retryCount++);

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _signalRBroadcastClient.StartAsync(CancellationToken.None));
        Assert.Equal(4, retryCount);
        _loggerMock.Verify(
            logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
            Times.Once);
    }

    [Fact]
    public async Task StopAsync_ShouldStopHubConnection()
    {
        // Arrange
        _hubConnectionMock.Setup(h => h.StopAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        await _signalRBroadcastClient.StopAsync(CancellationToken.None);

        // Assert
        _hubConnectionMock.Verify(h => h.StopAsync(It.IsAny<CancellationToken>()), Times.Once);
        _loggerMock.Verify(
            logger => logger.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
            Times.Once);
    }
}
