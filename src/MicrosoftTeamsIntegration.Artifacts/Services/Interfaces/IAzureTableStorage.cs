using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Azure.Cosmos.Table;

namespace MicrosoftTeamsIntegration.Artifacts.Services.Interfaces
{
    [PublicAPI]
    public interface IAzureTableStorage
    {
        Task<T?> Retrieve<T>(string partitionKey, string rowKey, CancellationToken cancellationToken = default)
            where T : class, ITableEntity, new();

        Task<T[]> Retrieve<T>(string partitionKey, CancellationToken cancellationToken = default)
            where T : class, ITableEntity, new();

        Task<T[]> Retrieve<T>(string propertyName, string operation, string propertyValue, CancellationToken cancellationToken = default)
            where T : class, ITableEntity, new();

        Task<T[]> Retrieve<T>(TableQuery<T> query, CancellationToken cancellationToken = default)
            where T : class, ITableEntity, new();

        Task Delete<T>(T entity, CancellationToken cancellationToken = default)
            where T : class, ITableEntity, new();

        Task Insert<T>(T entity, CancellationToken cancellationToken = default)
            where T : class, ITableEntity, new();

        Task InsertOrMerge<T>(T entity, CancellationToken cancellationToken = default)
            where T : class, ITableEntity, new();

        Task InsertOrReplace<T>(T entity, CancellationToken cancellationToken = default)
            where T : class, ITableEntity, new();

        Task Replace<T>(T entity, CancellationToken cancellationToken = default)
            where T : class, ITableEntity, new();
    }
}
