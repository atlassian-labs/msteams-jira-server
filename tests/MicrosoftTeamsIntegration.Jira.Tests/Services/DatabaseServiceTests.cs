using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FakeItEasy;
using Microsoft.Extensions.Options;
using MicrosoftTeamsIntegration.Jira.Models;
using MicrosoftTeamsIntegration.Jira.Models.Jira;
using MicrosoftTeamsIntegration.Jira.Services;
using MicrosoftTeamsIntegration.Jira.Services.Interfaces;
using MicrosoftTeamsIntegration.Jira.Settings;
using MongoDB.Driver;
using Xunit;

namespace MicrosoftTeamsIntegration.Jira.Tests.Services
{
    public class DatabaseServiceTests
    {
        private readonly IMongoDBContext _fakeContext;
        private readonly IOptions<AppSettings> _appSettings = new OptionsManager<AppSettings>(
            new OptionsFactory<AppSettings>(
                new List<IConfigureOptions<AppSettings>>(),
                new List<IPostConfigureOptions<AppSettings>>()));
        private IMongoCollection<IntegratedUser> _userCollection;
        private IMongoCollection<JiraAddonSettings> _addonCollection;
        private List<IntegratedUser> _userList;
        private List<JiraAddonSettings> _addonSettingsList;

        public DatabaseServiceTests()
        {
            var user = JiraDataGenerator.GenerateUser();
            user.Id = "id";
            user.MsTeamsTenantId = "jiraUserAccount";
            user.JiraInstanceUrl = string.Empty;
            user.JiraUserAccountId = "jiraid";
            user.IsUsedForPersonalScope = true;

            var addonSetting = new JiraAddonSettings() { JiraId = "id" };

            _addonSettingsList = new List<JiraAddonSettings>();
            _addonSettingsList.Add(addonSetting);

            _addonCollection = A.Fake<IMongoCollection<JiraAddonSettings>>();
            _addonCollection.InsertOne(addonSetting);

            _userCollection = A.Fake<IMongoCollection<IntegratedUser>>();
            _userCollection.InsertOne(user);

            _fakeContext = A.Fake<IMongoDBContext>();

            _userList = new List<IntegratedUser>();
            _userList.Add(user);
        }

        [Fact]
        public async Task GetJiraServerAddonSettingsByJiraId()
        {
            var service = CreateDatabaseService();

            var result = await service.GetJiraServerAddonSettingsByJiraId(string.Empty);

            Assert.IsType<JiraAddonSettings>(result);
            A.CallTo(() => _addonCollection.FindAsync(
                A<FilterDefinition<JiraAddonSettings>>._,
                A<FindOptions<JiraAddonSettings, JiraAddonSettings>>._,
                CancellationToken.None)).MustHaveHappened();
        }

        [Fact]
        public async Task GetUserByTeamsUserIdAndJiraUrl()
        {
            var service = CreateDatabaseService();

            var result = await service.GetUserByTeamsUserIdAndJiraUrl(string.Empty, string.Empty);

            Assert.IsType<IntegratedUser>(result);
            A.CallTo(() => _userCollection.FindAsync(
                A<FilterDefinition<IntegratedUser>>._,
                A<FindOptions<IntegratedUser, IntegratedUser>>._,
                CancellationToken.None)).MustHaveHappened();
        }

        [Fact]
        public async Task GetOrCreateUser_WhenUserIsFound()
        {
            var service = CreateDatabaseService();

            var result = await service.GetOrCreateUser(string.Empty, string.Empty, string.Empty);

            Assert.IsType<IntegratedUser>(result);
            Assert.Equal("id", result.Id);
            A.CallTo(() => _userCollection.FindAsync(
                A<FilterDefinition<IntegratedUser>>._,
                A<FindOptions<IntegratedUser, IntegratedUser>>._,
                CancellationToken.None)).MustHaveHappened();
        }

        [Fact]
        public async Task GetOrCreateUser_WhenUserIsFoundAndHasJiraUrl()
        {
            _userList[0].JiraInstanceUrl = "test";

            var service = CreateDatabaseService();

            var result = await service.GetOrCreateUser(string.Empty, string.Empty, string.Empty);

            Assert.IsType<IntegratedUser>(result);
            Assert.Equal(string.Empty, result.JiraInstanceUrl);
            A.CallTo(() => _userCollection.FindAsync(
                A<FilterDefinition<IntegratedUser>>._,
                A<FindOptions<IntegratedUser, IntegratedUser>>._,
                CancellationToken.None)).MustHaveHappened();
            A.CallTo(() => _userCollection.InsertOne(A<IntegratedUser>._, null, CancellationToken.None))
                .MustHaveHappened();
        }

        [Fact]
        public async Task GetOrCreateUser_WhenUserNotFound()
        {
            _userList[0].JiraInstanceUrl = null;

            A.CallTo(() => _userCollection.FindOneAndUpdateAsync(
                    A<FilterDefinition<IntegratedUser>>._,
                    A<UpdateDefinition<IntegratedUser>>._,
                    A<FindOneAndUpdateOptions<IntegratedUser, IntegratedUser>>._,
                    CancellationToken.None))
                .Returns(new IntegratedUser() { JiraInstanceUrl = "url" });

            var service = CreateDatabaseService();

            var result = await service.GetOrCreateUser(string.Empty, string.Empty, "url");

            Assert.IsType<IntegratedUser>(result);
            Assert.Equal("url", result.JiraInstanceUrl);

            A.CallTo(() => _userCollection.FindAsync(
                A<FilterDefinition<IntegratedUser>>._,
                A<FindOptions<IntegratedUser, IntegratedUser>>._,
                CancellationToken.None)).MustHaveHappened();
            A.CallTo(() => _userCollection.FindOneAndUpdateAsync(
                    A<FilterDefinition<IntegratedUser>>._,
                    A<UpdateDefinition<IntegratedUser>>._,
                    A<FindOneAndUpdateOptions<IntegratedUser, IntegratedUser>>._,
                    CancellationToken.None))
                .MustHaveHappened();
        }

        [Fact]
        public async Task GetOrCreateJiraServerUser()
        {
            var service = CreateDatabaseService();

            var result = await service.GetOrCreateJiraServerUser(string.Empty, string.Empty, string.Empty);

            Assert.IsType<IntegratedUser>(result);
            A.CallTo(() => _addonCollection.FindAsync(
                A<FilterDefinition<JiraAddonSettings>>._,
                A<FindOptions<JiraAddonSettings, JiraAddonSettings>>._,
                CancellationToken.None)).MustHaveHappened();
            A.CallTo(() => _userCollection.FindAsync(
                A<FilterDefinition<IntegratedUser>>._,
                A<FindOptions<IntegratedUser, IntegratedUser>>._,
                CancellationToken.None)).MustHaveHappened();
            A.CallTo(() => _userCollection.InsertOne(A<IntegratedUser>._, null, CancellationToken.None))
                .MustHaveHappened();
        }

        [Fact]
        public async Task UpdateUserActiveJiraInstanceForPersonalScope()
        {
            var service = CreateDatabaseService();

            var fakeUpdateResult = A.Fake<UpdateResult>();
            A.CallTo(() => fakeUpdateResult.IsAcknowledged).Returns(true);
            A.CallTo(() => fakeUpdateResult.ModifiedCount).Returns(1);

            A.CallTo(() => _userCollection.UpdateOneAsync(
                A<FilterDefinition<IntegratedUser>>._,
                A<UpdateDefinition<IntegratedUser>>._,
                null,
                CancellationToken.None)).Returns(fakeUpdateResult);

            await service.UpdateUserActiveJiraInstanceForPersonalScope(string.Empty, string.Empty);

            A.CallTo(() => _userCollection.UpdateOneAsync(
                A<FilterDefinition<IntegratedUser>>._,
                A<UpdateDefinition<IntegratedUser>>._,
                null,
                CancellationToken.None)).MustHaveHappened(2, Times.Exactly);
        }

        [Fact]
        public async Task DeleteJiraServerUser()
        {
            var service = CreateDatabaseService();

            var fakeDeleteResult = A.Fake<DeleteResult>();
            A.CallTo(() => fakeDeleteResult.IsAcknowledged).Returns(true);
            A.CallTo(() => fakeDeleteResult.DeletedCount).Returns(1);

            A.CallTo(() => _userCollection.DeleteOneAsync(A<FilterDefinition<IntegratedUser>>._, CancellationToken.None)).Returns(fakeDeleteResult);

            await service.DeleteJiraServerUser(string.Empty, string.Empty);

            A.CallTo(() => _userCollection.DeleteOneAsync(A<FilterDefinition<IntegratedUser>>._, CancellationToken.None)).MustHaveHappened();
        }

        [Fact]
        public async Task DeleteJiraServerAddonSettingsByConnectionId()
        {
            var service = CreateDatabaseService();

            var fakeDeleteResult = A.Fake<DeleteResult>();
            A.CallTo(() => fakeDeleteResult.IsAcknowledged).Returns(true);
            A.CallTo(() => fakeDeleteResult.DeletedCount).Returns(1);

            A.CallTo(() => _addonCollection.DeleteOneAsync(A<FilterDefinition<JiraAddonSettings>>._, CancellationToken.None))
                .Returns(fakeDeleteResult);

            await service.DeleteJiraServerAddonSettingsByConnectionId(string.Empty);

            A.CallTo(() => _addonCollection.DeleteOneAsync(A<FilterDefinition<JiraAddonSettings>>._, CancellationToken.None))
                .MustHaveHappened();
        }

        [Fact]
        public async Task CreateOrUpdateJiraServerAddonSettings()
        {
            var service = CreateDatabaseService();

            var fakeUpdateResult = A.Fake<UpdateResult>();
            A.CallTo(() => fakeUpdateResult.IsAcknowledged).Returns(true);
            A.CallTo(() => fakeUpdateResult.ModifiedCount).Returns(1);

            A.CallTo(() => _addonCollection.UpdateOneAsync(
                A<FilterDefinition<JiraAddonSettings>>._,
                A<UpdateDefinition<JiraAddonSettings>>._,
                A<UpdateOptions>._,
                CancellationToken.None)).Returns(fakeUpdateResult);

            await service.CreateOrUpdateJiraServerAddonSettings(string.Empty, string.Empty, string.Empty, string.Empty);

            A.CallTo(() => _addonCollection.FindAsync(
                A<FilterDefinition<JiraAddonSettings>>._,
                A<FindOptions<JiraAddonSettings, JiraAddonSettings>>._,
                CancellationToken.None)).MustHaveHappened();

            A.CallTo(() => _addonCollection.UpdateOneAsync(
                A<FilterDefinition<JiraAddonSettings>>._,
                A<UpdateDefinition<JiraAddonSettings>>._,
                A<UpdateOptions>._,
                CancellationToken.None)).MustHaveHappened();
        }

        [Fact]
        public async Task GetJiraServerAddonStatus()
        {
            var service = CreateDatabaseService();

            var result = await service.GetJiraServerAddonStatus(string.Empty, string.Empty);

            Assert.IsType<AtlassianAddonStatus>(result);
            A.CallTo(() => _addonCollection.FindAsync(
                A<FilterDefinition<JiraAddonSettings>>._,
                A<FindOptions<JiraAddonSettings, JiraAddonSettings>>._,
                CancellationToken.None)).MustHaveHappened();
            A.CallTo(() => _userCollection.FindAsync(
                A<FilterDefinition<IntegratedUser>>._,
                A<FindOptions<IntegratedUser, IntegratedUser>>._,
                CancellationToken.None)).MustHaveHappened();
        }

        [Fact]
        public async Task GetJiraServerUserWithConfiguredPersonalScope()
        {
            var service = CreateDatabaseService();

            var fakeUpdateResult = A.Fake<UpdateResult>();
            A.CallTo(() => fakeUpdateResult.IsAcknowledged).Returns(true);
            A.CallTo(() => fakeUpdateResult.ModifiedCount).Returns(1);

            A.CallTo(() => _userCollection.UpdateOneAsync(
                A<FilterDefinition<IntegratedUser>>._,
                A<UpdateDefinition<IntegratedUser>>._,
                null,
                CancellationToken.None)).Returns(fakeUpdateResult);

            var result = await service.GetJiraServerUserWithConfiguredPersonalScope("test");

            Assert.IsType<IntegratedUser>(result);
            A.CallTo(() => _userCollection.FindAsync(
                A<FilterDefinition<IntegratedUser>>._,
                A<FindOptions<IntegratedUser, IntegratedUser>>._,
                CancellationToken.None)).MustHaveHappened();
            A.CallTo(() => _userCollection.UpdateOneAsync(
                A<FilterDefinition<IntegratedUser>>._,
                A<UpdateDefinition<IntegratedUser>>._,
                null,
                CancellationToken.None)).MustNotHaveHappened();
        }

        [Fact]
        public async Task GetJiraServerUserWithConfiguredPersonalScope_ReturnsNull_WhenUserIdNull()
        {
            var service = CreateDatabaseService();

            var fakeUpdateResult = A.Fake<UpdateResult>();
            A.CallTo(() => fakeUpdateResult.IsAcknowledged).Returns(true);
            A.CallTo(() => fakeUpdateResult.ModifiedCount).Returns(1);

            A.CallTo(() => _userCollection.UpdateOneAsync(
                A<FilterDefinition<IntegratedUser>>._,
                A<UpdateDefinition<IntegratedUser>>._,
                null,
                CancellationToken.None)).Returns(fakeUpdateResult);

            var result = await service.GetJiraServerUserWithConfiguredPersonalScope(null);

            Assert.Null(result);
            A.CallTo(() => _userCollection.FindAsync(
                A<FilterDefinition<IntegratedUser>>._,
                A<FindOptions<IntegratedUser, IntegratedUser>>._,
                CancellationToken.None)).MustNotHaveHappened();
            A.CallTo(() => _userCollection.UpdateOneAsync(
                A<FilterDefinition<IntegratedUser>>._,
                A<UpdateDefinition<IntegratedUser>>._,
                null,
                CancellationToken.None)).MustNotHaveHappened();
        }

        [Fact]
        public async Task UpdateJiraServerUserActiveJiraInstanceForPersonalScope()
        {
            var service = CreateDatabaseService();

            var fakeUpdateResult = A.Fake<UpdateResult>();
            A.CallTo(() => fakeUpdateResult.IsAcknowledged).Returns(true);
            A.CallTo(() => fakeUpdateResult.ModifiedCount).Returns(1);

            A.CallTo(() => _userCollection.UpdateOneAsync(
                A<FilterDefinition<IntegratedUser>>._,
                A<UpdateDefinition<IntegratedUser>>._,
                null,
                CancellationToken.None)).Returns(fakeUpdateResult);

            var result = await service.UpdateJiraServerUserActiveJiraInstanceForPersonalScope(string.Empty, string.Empty);

            Assert.IsType<IntegratedUser>(result);
            A.CallTo(() => _userCollection.FindAsync(
                A<FilterDefinition<IntegratedUser>>._,
                A<FindOptions<IntegratedUser, IntegratedUser>>._,
                CancellationToken.None)).MustHaveHappened();
            A.CallTo(() => _userCollection.UpdateOneAsync(
                A<FilterDefinition<IntegratedUser>>._,
                A<UpdateDefinition<IntegratedUser>>._,
                null,
                CancellationToken.None)).MustHaveHappened(2, Times.Exactly);
        }

        private DatabaseService CreateDatabaseService()
        {
            var userCursor = A.Fake<IAsyncCursor<IntegratedUser>>();
            A.CallTo(() => userCursor.Current).Returns(_userList);
            A.CallTo(() => userCursor.MoveNext(CancellationToken.None)).ReturnsNextFromSequence(true, false);
            A.CallTo(() => userCursor.MoveNextAsync(CancellationToken.None)).ReturnsNextFromSequence(Task.FromResult(true), Task.FromResult(false));
            A.CallTo(() => _userCollection.FindAsync(
                A<FilterDefinition<IntegratedUser>>._,
                A<FindOptions<IntegratedUser, IntegratedUser>>._,
                CancellationToken.None)).Returns(userCursor);

            var addonCursor = A.Fake<IAsyncCursor<JiraAddonSettings>>();
            A.CallTo(() => addonCursor.Current).Returns(_addonSettingsList);
            A.CallTo(() => addonCursor.MoveNext(CancellationToken.None)).ReturnsNextFromSequence(true, false);
            A.CallTo(() => addonCursor.MoveNextAsync(CancellationToken.None)).ReturnsNextFromSequence(Task.FromResult(true), Task.FromResult(false));
            A.CallTo(() => _addonCollection.FindAsync(
                A<FilterDefinition<JiraAddonSettings>>._,
                A<FindOptions<JiraAddonSettings, JiraAddonSettings>>._,
                CancellationToken.None)).Returns(addonCursor);

            A.CallTo(() => _fakeContext.GetCollection<IntegratedUser>("Users")).Returns(_userCollection);
            A.CallTo(() => _fakeContext.GetCollection<JiraAddonSettings>("AddonSettings")).Returns(_addonCollection);
            A.CallTo(() => _fakeContext.MaxConnectionPoolSize).Returns(10);

            return new DatabaseService(_appSettings, _fakeContext);
        }
    }
}
