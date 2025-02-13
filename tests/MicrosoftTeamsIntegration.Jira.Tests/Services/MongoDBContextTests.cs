using System.Collections.Generic;
using FakeItEasy;
using Microsoft.Extensions.Options;
using MicrosoftTeamsIntegration.Jira.Models;
using MicrosoftTeamsIntegration.Jira.Services;
using MicrosoftTeamsIntegration.Jira.Settings;
using MongoDB.Driver;
using Xunit;

namespace MicrosoftTeamsIntegration.Jira.Tests.Services
{
    public class MongoDBContextTests
    {
        private readonly IOptions<AppSettings> _appSettings = new OptionsManager<AppSettings>(
            new OptionsFactory<AppSettings>(
                new List<IConfigureOptions<AppSettings>>(),
                new List<IPostConfigureOptions<AppSettings>>()));
        private IMongoDatabase _fakeDatabase;
        private IMongoClient _fakeMongoClient;

        public MongoDBContextTests()
        {
            _appSettings.Value.DatabaseUrl = "mongodb://tes123/test";
            _fakeDatabase = A.Fake<IMongoDatabase>();
            _fakeMongoClient = A.Fake<IMongoClient>();
        }

        [Fact]
        public void MongoDBContext_Constructor_Success()
        {
            A.CallTo(() => _fakeMongoClient.GetDatabase(A<string>._, A<MongoDatabaseSettings>._)).Returns(_fakeDatabase);

            var context = new MongoDBContext(_appSettings);

            Assert.NotNull(context);
        }

        [Fact]
        public void GetCollection_ReturnsNull_WhenNameEmpty()
        {
            A.CallTo(() => _fakeMongoClient.GetDatabase(A<string>._, A<MongoDatabaseSettings>._)).Returns(_fakeDatabase);

            var context = new MongoDBContext(_appSettings);
            var collection = context.GetCollection<IntegratedUser>(string.Empty);

            Assert.Null(collection);
        }

        [Fact]
        public void GetCollection_ReturnsCollection_WhenNameCorrect()
        {
            A.CallTo(() => _fakeMongoClient.GetDatabase(A<string>._, A<MongoDatabaseSettings>._)).Returns(_fakeDatabase);

            var context = new MongoDBContext(_appSettings);
            var collection = context.GetCollection<IntegratedUser>("Users");

            Assert.NotNull(collection);
        }

        [Fact]
        public void Dispose_ShouldDisposeMongoClient()
        {
            var context = new MongoDBContext(_fakeMongoClient);

            context.Dispose();

            A.CallTo(() => _fakeMongoClient.Dispose()).MustHaveHappened();
        }
    }
}
