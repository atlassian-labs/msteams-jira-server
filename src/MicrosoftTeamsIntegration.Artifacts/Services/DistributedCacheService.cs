using System;
using System.Threading;
using System.Threading.Tasks;
using Hyperion;
using JetBrains.Annotations;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.IO;
using MicrosoftTeamsIntegration.Artifacts.Services.Interfaces;

namespace MicrosoftTeamsIntegration.Artifacts.Services
{
    [PublicAPI]
    public class DistributedCacheService : IDistributedCacheService
    {
        private readonly IDistributedCache _distributedCache;
        private readonly ILogger<DistributedCacheService> _logger;
        private readonly Serializer _wireSerializer;
        private readonly RecyclableMemoryStreamManager _memoryStreamManager;

        public DistributedCacheService(IDistributedCache distributedCache, ILogger<DistributedCacheService> logger)
        {
            _distributedCache = distributedCache;
            _logger = logger;
            _wireSerializer = new Serializer(new SerializerOptions());
            _memoryStreamManager = new RecyclableMemoryStreamManager();
        }

        public Task Set<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default)
        {
            var serializedValue = Serialize(value);
            var distributedCacheEntryOptions = new DistributedCacheEntryOptions();
            if (expiry.HasValue)
            {
                distributedCacheEntryOptions = distributedCacheEntryOptions.SetSlidingExpiration(expiry.Value);
            }

            return _distributedCache.SetAsync(key, serializedValue, distributedCacheEntryOptions, cancellationToken);
        }

        public async Task<T> Get<T>(string key, CancellationToken cancellationToken = default)
        {
            var serializedValue = await _distributedCache.GetAsync(key, cancellationToken);
            return await Deserialize<T>(serializedValue, cancellationToken);
        }

        public Task Refresh(string key, CancellationToken cancellationToken = default)
        {
            return _distributedCache.RefreshAsync(key, cancellationToken);
        }

        public Task Remove(string key, CancellationToken cancellationToken = default)
        {
            return _distributedCache.RemoveAsync(key, cancellationToken);
        }

        private byte[] Serialize<T>(T value)
        {
            var objectDataAsStream = Array.Empty<byte>();
            using (var memoryStream = _memoryStreamManager.GetStream())
            {
                try
                {
                    _wireSerializer.Serialize(value, memoryStream);
                    objectDataAsStream = memoryStream.ToArray();
                }
                catch (Exception e)
                {
                   _logger.LogError(e, e.Message);
                }
            }

            return objectDataAsStream;
        }

        #nullable disable
        private async Task<T> Deserialize<T>(byte[] sourceBytes, CancellationToken cancellationToken)
        {
            T result = default;
            if (sourceBytes == null)
            {
                return result;
            }

            using (var memoryStream = _memoryStreamManager.GetStream())
            {
                await memoryStream.WriteAsync(sourceBytes, 0, sourceBytes.Length, cancellationToken);
                memoryStream.Position = 0;
                try
                {
                    result = _wireSerializer.Deserialize<T>(memoryStream);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, e.Message);
                }
            }

            return result;
        }
        #nullable enable
    }
}
