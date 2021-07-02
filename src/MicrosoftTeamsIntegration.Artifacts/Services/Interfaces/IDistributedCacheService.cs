using System;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace MicrosoftTeamsIntegration.Artifacts.Services.Interfaces
{
    [PublicAPI]
    public interface IDistributedCacheService
    {
        Task Set<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default);
        Task<T> Get<T>(string key, CancellationToken cancellationToken = default);
        Task Refresh(string key, CancellationToken cancellationToken = default);
        Task Remove(string key, CancellationToken cancellationToken = default);
    }
}
