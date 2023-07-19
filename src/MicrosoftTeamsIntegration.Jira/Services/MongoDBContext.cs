using System.Security.Authentication;
using Microsoft.Extensions.Options;
using MicrosoftTeamsIntegration.Jira.Services.Interfaces;
using MicrosoftTeamsIntegration.Jira.Settings;
using MongoDB.Driver;

namespace MicrosoftTeamsIntegration.Jira.Services
{
    public class MongoDBContext : IMongoDBContext
    {
        public int MaxConnectionPoolSize { get; }
        private readonly MongoClient _mongoClient;
        private readonly IMongoDatabase _db;
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

        public IMongoCollection<T> GetCollection<T>(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }

            return _db.GetCollection<T>(name);
        }
    }
}
