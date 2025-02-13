using System;
using System.Threading.Tasks;
using MicrosoftTeamsIntegration.Jira.Services.SignalR;
using NonBlocking;
using Xunit;

namespace MicrosoftTeamsIntegration.Jira.Tests.Services.SignalR;

public class DictionaryExtensionsTests
{
    [Fact]
    public void GetLog_ShouldReturnNoClientResponsesAvailable_WhenDictionaryIsNull()
    {
        // Arrange
        ConcurrentDictionary<Guid, TaskCompletionSource<string>> dic = null;

        // Act
        var result = dic.GetLog();

        // Assert
        Assert.Equal("No client responses available.", result);
    }

    [Fact]
    public void GetLog_ShouldReturnNoClientResponsesAvailable_WhenDictionaryIsEmpty()
    {
        // Arrange
        var dic = new ConcurrentDictionary<Guid, TaskCompletionSource<string>>();

        // Act
        var result = dic.GetLog();

        // Assert
        Assert.Equal("No client responses available.", result);
    }

    [Fact]
    public void GetLog_ShouldReturnKeys_WhenDictionaryIsNotEmpty()
    {
        // Arrange
        var dic = new ConcurrentDictionary<Guid, TaskCompletionSource<string>>();
        var key1 = Guid.NewGuid();
        var key2 = Guid.NewGuid();
        dic.TryAdd(key1, new TaskCompletionSource<string>());
        dic.TryAdd(key2, new TaskCompletionSource<string>());

        // Act
        var result = dic.GetLog();

        // Assert
        Assert.Contains(key1.ToString(), result);
        Assert.Contains(key2.ToString(), result);
    }
}
