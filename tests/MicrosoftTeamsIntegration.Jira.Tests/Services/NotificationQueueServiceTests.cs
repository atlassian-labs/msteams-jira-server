using System;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using MicrosoftTeamsIntegration.Jira.Services;
using Xunit;

namespace MicrosoftTeamsIntegration.Jira.Tests.Services;

public class NotificationQueueServiceTests
{
    private readonly FakeLogger<NotificationQueueService> _loggerFake;
    private readonly QueueClient _queueClientFake;
    private readonly NotificationQueueService _service;

    public NotificationQueueServiceTests()
    {
        _loggerFake = new FakeLogger<NotificationQueueService>();
        _queueClientFake = A.Fake<QueueClient>();
        _service = new NotificationQueueService(_loggerFake, _queueClientFake);
    }

    [Fact]
    public async Task QueueNotificationMessage_ShouldSendMessage()
    {
        // Arrange
        var message = "Test message";
        A.CallTo(() => _queueClientFake.SendMessageAsync(message))
            .Returns(A.Fake<Response<SendReceipt>>());

        // Act
        await _service.QueueNotificationMessage(message);

        // Assert
        A.CallTo(() => _queueClientFake.SendMessageAsync(message)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task QueueNotificationMessage_ShouldLogError_OnException()
    {
        // Arrange
        var message = "Test message";
        A.CallTo(() => _queueClientFake.SendMessageAsync(message))
            .ThrowsAsync(new Exception("Test exception"));

        // Act
        await _service.QueueNotificationMessage(message);

        // Assert
        Assert.Equal(1, _loggerFake.Collector.Count);
        Assert.Equal(LogLevel.Error, _loggerFake.LatestRecord.Level);
    }

    [Fact]
    public async Task DequeueNotificationsMessages_ShouldReturnMessages()
    {
        // Arrange
        A.CallTo(() => _queueClientFake.ReceiveMessagesAsync(A<int?>._, A<TimeSpan?>._, A<CancellationToken>._))
            .Returns(A.Fake<Response<QueueMessage[]>>());

        // Act
        var result = await _service.DequeueNotificationsMessages();

        // Assert
        Assert.NotNull(result);
        A.CallTo(() => _queueClientFake.ReceiveMessagesAsync(A<int?>._, A<TimeSpan?>._, A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task DequeueNotificationsMessages_ShouldLogError_OnException()
    {
        // Arrange
        A.CallTo(() => _queueClientFake.ReceiveMessagesAsync(A<int?>._, A<TimeSpan?>._, A<CancellationToken>._))
            .ThrowsAsync(new Exception("Test exception"));

        // Act
        var result = await _service.DequeueNotificationsMessages();

        // Assert
        Assert.Empty(result);
        Assert.Equal(1, _loggerFake.Collector.Count);
        Assert.Equal(LogLevel.Error, _loggerFake.LatestRecord.Level);
    }

    [Fact]
    public async Task DeleteNotificationMessage_ShouldDeleteMessage()
    {
        // Arrange
        var messageId = "test-message-id";
        var popReceipt = "test-pop-receipt";
        A.CallTo(() => _queueClientFake.DeleteMessageAsync(messageId, popReceipt, A<CancellationToken>._))
            .Returns(A.Fake<Response>());

        // Act
        await _service.DeleteNotificationMessage(messageId, popReceipt);

        // Assert
        A.CallTo(() => _queueClientFake.DeleteMessageAsync(messageId, popReceipt, A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task DeleteNotificationMessage_ShouldLogError_OnException()
    {
        // Arrange
        var messageId = "test-message-id";
        var popReceipt = "test-pop-receipt";
        A.CallTo(() => _queueClientFake.DeleteMessageAsync(messageId, popReceipt, A<CancellationToken>._))
            .ThrowsAsync(new Exception("Test exception"));

        // Act
        await _service.DeleteNotificationMessage(messageId, popReceipt);

        // Assert
        Assert.Equal(1, _loggerFake.Collector.Count);
        Assert.Equal(LogLevel.Error, _loggerFake.LatestRecord.Level);
    }
}
