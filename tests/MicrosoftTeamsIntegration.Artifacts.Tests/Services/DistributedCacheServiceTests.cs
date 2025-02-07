using FakeItEasy;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using MicrosoftTeamsIntegration.Artifacts.Services;

namespace MicrosoftTeamsIntegration.Artifacts.Tests.Services;

public class DistributedCacheServiceTests
{
    private readonly IDistributedCache _fakeDistributedCache;
    private readonly ILogger<DistributedCacheService> _fakeLogger;
    private readonly DistributedCacheService _distributedCacheService;

    public DistributedCacheServiceTests()
    {
        _fakeDistributedCache = A.Fake<IDistributedCache>();
        _fakeLogger = A.Fake<ILogger<DistributedCacheService>>();
        _distributedCacheService = new DistributedCacheService(_fakeDistributedCache, _fakeLogger);
    }

    [Fact]
    public async Task Set_ShouldStoreValueInCache()
    {
        // Arrange
        var key = "testKey";
        var value = "testValue";
        var expiry = TimeSpan.FromMinutes(5);
        var cancellationToken = CancellationToken.None;

        // Act
        await _distributedCacheService.Set(key, value, expiry, cancellationToken);

        // Assert
        A.CallTo(() => _fakeDistributedCache.SetAsync(
            key,
            A<byte[]>._,
            A<DistributedCacheEntryOptions>.That.Matches(options => options.SlidingExpiration == expiry),
            cancellationToken)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task Get_ShouldReturnStoredValue()
    {
        // Arrange
        var key = "testKey";
        var expectedValue = "testValue";
        var serializedValue = new byte[] { 7, 10, 116, 101, 115, 116, 86, 97, 108, 117, 101 };
        var cancellationToken = CancellationToken.None;

        A.CallTo(() => _fakeDistributedCache.GetAsync(key, cancellationToken)).Returns(serializedValue);

        // Act
        var result = await _distributedCacheService.Get<string>(key, cancellationToken);

        // Assert
        Assert.Equal(expectedValue, result);
    }

    [Fact]
    public async Task Refresh_ShouldRefreshCacheEntry()
    {
        // Arrange
        var key = "testKey";
        var cancellationToken = CancellationToken.None;

        // Act
        await _distributedCacheService.Refresh(key, cancellationToken);

        // Assert
        A.CallTo(() => _fakeDistributedCache.RefreshAsync(key, cancellationToken)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task Remove_ShouldRemoveCacheEntry()
    {
        // Arrange
        var key = "testKey";
        var cancellationToken = CancellationToken.None;

        // Act
        await _distributedCacheService.Remove(key, cancellationToken);

        // Assert
        A.CallTo(() => _fakeDistributedCache.RemoveAsync(key, cancellationToken)).MustHaveHappenedOnceExactly();
    }
}
