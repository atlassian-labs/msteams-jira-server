using MongoDB.Driver;

namespace MicrosoftTeamsIntegration.Jira.Services.Interfaces
{
    public interface IMongoDBContext
    {
        public int MaxConnectionPoolSize { get; }
        IMongoCollection<T> GetCollection<T>(string name);
    }
}
