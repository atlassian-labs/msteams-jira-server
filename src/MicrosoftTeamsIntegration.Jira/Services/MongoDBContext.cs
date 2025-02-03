using System;
using System.Security.Authentication;
using Microsoft.Extensions.Options;
using MicrosoftTeamsIntegration.Jira.Services.Interfaces;
using MicrosoftTeamsIntegration.Jira.Settings;
using MongoDB.Driver;

namespace MicrosoftTeamsIntegration.Jira.Services
{
    public class MongoDBContext : IMongoDBContext, IDisposable
    {
        public int MaxConnectionPoolSize { get; }
        private readonly IMongoClient _mongoClient;
        private readonly IMongoDatabase _db;
        private bool _disposed;
        public MongoDBContext(IOptions<AppSettings> appSettings)
        {
            var mongoUrl = MongoUrl.Create(appSettings.Value.DatabaseUrl);
            var databaseName = mongoUrl.DatabaseName;

            var settings = MongoClientSettings.FromUrl(mongoUrl);
            settings.SslSettings = new SslSettings { EnabledSslProtocols = SslProtocols.Tls12 };

            _mongoClient = new MongoClient(settings);
            _db = _mongoClient.GetDatabase(databaseName);

            MaxConnectionPoolSize = _mongoClient.Settings.MaxConnectionPoolSize;
        }

        public MongoDBContext(IMongoClient mongoClient)
        {
            _mongoClient = mongoClient;
        }

        public IMongoCollection<T> GetCollection<T>(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }

            return _db.GetCollection<T>(name);
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
                    _mongoClient?.Dispose();
                }

                _disposed = true;
            }
        }
    }
}
