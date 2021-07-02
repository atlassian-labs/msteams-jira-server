using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Azure.Cosmos.Table;
using MicrosoftTeamsIntegration.Artifacts.Extensions;
using MicrosoftTeamsIntegration.Artifacts.Services.Interfaces;
using NonBlocking;

namespace MicrosoftTeamsIntegration.Artifacts.Services.TableStorage
{
    [PublicAPI]
    public sealed class AzureTableStorage : IAzureTableStorage
    {
        private readonly string _appId;
        private readonly CloudTableClient _tableClient;
        private readonly ConcurrentDictionary<string, CloudTable> _tables;

        public AzureTableStorage(string appId, string connectionString)
        {
            _appId = appId;

            var account = CloudStorageAccount.Parse(connectionString);
            _tableClient = account.CreateCloudTableClient();

            _tables = new ConcurrentDictionary<string, CloudTable>();
        }

        public async Task<T?> Retrieve<T>(string partitionKey, string rowKey, CancellationToken cancellationToken = default)
            where T : class, ITableEntity, new()
        {
            var table = await GetTable<T>(cancellationToken);
            var retrieveOperation = TableOperation.Retrieve<T>(partitionKey.SanitizeForAzureKeys(), rowKey.SanitizeForAzureKeys());
            var operationResult = await table.ExecuteAsync(retrieveOperation, cancellationToken);
            return operationResult?.Result as T;
        }

        public Task<T[]> Retrieve<T>(string partitionKey, CancellationToken cancellationToken = default)
            where T : class, ITableEntity, new()
        {
            var query = new TableQuery<T>()
                .Where(TableQuery.GenerateFilterCondition(
                    nameof(TableEntity.PartitionKey),
                    QueryComparisons.Equal,
                    partitionKey.SanitizeForAzureKeys()));

            return Retrieve(query, cancellationToken);
        }

        public Task<T[]> Retrieve<T>(
            string propertyName,
            string operation,
            string propertyValue,
            CancellationToken cancellationToken = default)
            where T : class, ITableEntity, new()
        {
            if (string.Equals(nameof(TableEntity.RowKey), propertyName, StringComparison.Ordinal) ||
                string.Equals(nameof(TableEntity.PartitionKey), propertyName, StringComparison.Ordinal))
            {
                propertyValue = propertyValue.SanitizeForAzureKeys();
            }

            var query = new TableQuery<T>()
                .Where(TableQuery.GenerateFilterCondition(propertyName, operation, propertyValue));

            return Retrieve(query, cancellationToken);
        }

        public async Task<T[]> Retrieve<T>(
            TableQuery<T> query,
            CancellationToken cancellationToken = default)
            where T : class, ITableEntity, new()
        {
            var table = await GetTable<T>(cancellationToken);

            var results = new List<T>();
            TableContinuationToken? continuationToken = null;

            do
            {
                var queryResults = await table.ExecuteQuerySegmentedAsync(query, continuationToken, cancellationToken);
                continuationToken = queryResults.ContinuationToken;

                results.AddRange(queryResults.Results);
            }
            while (continuationToken != null);

            return results.ToArray();
        }

        public async Task Delete<T>(
            T entity,
            CancellationToken cancellationToken = default)
            where T : class, ITableEntity, new()
        {
            var table = await GetTable<T>(cancellationToken);
            entity.ETag = "*";
            var deleteOperation = TableOperation.Delete(entity);
            await table.ExecuteAsync(deleteOperation, cancellationToken);
        }

        public async Task Insert<T>(
            T entity,
            CancellationToken cancellationToken = default)
            where T : class, ITableEntity, new()
        {
            var table = await GetTable<T>(cancellationToken);
            var insertOperation = TableOperation.Insert(entity);
            await table.ExecuteAsync(insertOperation, cancellationToken);
        }

        public async Task InsertOrMerge<T>(
            T entity,
            CancellationToken cancellationToken = default)
            where T : class, ITableEntity, new()
        {
            var table = await GetTable<T>(cancellationToken);
            entity.ETag = "*";
            var insertOrMergeOperation = TableOperation.InsertOrMerge(entity);
            await table.ExecuteAsync(insertOrMergeOperation, cancellationToken);
        }

        public async Task InsertOrReplace<T>(
            T entity,
            CancellationToken cancellationToken = default)
            where T : class, ITableEntity, new()
        {
            var table = await GetTable<T>(cancellationToken);
            entity.ETag = "*";
            var insertOrReplaceOperation = TableOperation.InsertOrReplace(entity);
            await table.ExecuteAsync(insertOrReplaceOperation, cancellationToken);
        }

        public async Task Replace<T>(
            T entity,
            CancellationToken cancellationToken = default)
            where T : class, ITableEntity, new()
        {
            var table = await GetTable<T>(cancellationToken);
            entity.ETag = "*";
            var replaceOperation = TableOperation.Replace(entity);
            await table.ExecuteAsync(replaceOperation, cancellationToken);
        }

        private async Task<CloudTable> GetTable<T>(CancellationToken cancellationToken = default)
        {
            var type = typeof(T);
            var typeName = type.Name;
            if (type.IsGenericType)
            {
                var arguments = type.GetGenericArguments();
                if (arguments.Any())
                {
                    typeName = arguments[0].Name;
                }
            }

            var tableName = SanitizeTableName($"db{_appId}{typeName}");

            if (_tables.TryGetValue(tableName, out var cloudTable))
            {
                return cloudTable;
            }

            cloudTable = _tableClient.GetTableReference(tableName);
            await cloudTable.CreateIfNotExistsAsync(cancellationToken);

            _tables.TryAdd(tableName, cloudTable);

            return cloudTable;
        }

        private static string SanitizeTableName(string input)
        {
            if (input.Length > 63)
            {
                input = input.Substring(0, 63);
            }

            input = Regex.Replace(input, @"[^a-zA-Z0-9]+", string.Empty);
            return input;
        }
    }
}
