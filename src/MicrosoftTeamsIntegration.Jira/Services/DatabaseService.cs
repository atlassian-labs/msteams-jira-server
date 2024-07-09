using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Extensions.Options;
using MicrosoftTeamsIntegration.Jira.Models;
using MicrosoftTeamsIntegration.Jira.Models.Jira;
using MicrosoftTeamsIntegration.Jira.Services.Interfaces;
using MicrosoftTeamsIntegration.Jira.Settings;
using MongoDB.Driver;

namespace MicrosoftTeamsIntegration.Jira.Services
{
    [UsedImplicitly]
    public sealed class DatabaseService : IDatabaseService
    {
        private readonly IMongoCollection<IntegratedUser> _usersCollection;

        private readonly IMongoCollection<JiraAddonSettings> _jiraAddonSettingsCollection;
        private static SemaphoreSlim _openConnectionSemaphore;
        private readonly IOptions<AppSettings> _appSettings;
        private readonly IMongoDBContext _context;

        public DatabaseService(IOptions<AppSettings> appSettings, IMongoDBContext context)
        {
            _appSettings = appSettings;
            _context = context;

            _openConnectionSemaphore = new SemaphoreSlim(
                context.MaxConnectionPoolSize / 2,
                context.MaxConnectionPoolSize / 2);

            _usersCollection = context.GetCollection<IntegratedUser>("Users");

            _jiraAddonSettingsCollection = context.GetCollection<JiraAddonSettings>("AddonSettings");
        }

        public async Task<IntegratedUser> GetOrCreateUser(string msTeamsUserId, string msTeamsTenantId, string jiraUrl)
        {
            var users = await ProcessThrottlingRequest(() => _usersCollection
                .Find(x => x.MsTeamsUserId == msTeamsUserId)
                .ToListAsync());

            var user = users.FirstOrDefault(x => x.JiraInstanceUrl == jiraUrl);
            if (user == null)
            {
                user = users.FirstOrDefault(x => x.JiraInstanceUrl == null);
                if (user != null)
                {
                    var userId = user.Id;
                    Expression<Func<IntegratedUser, bool>> filter = x => x.Id == userId;
                    var update = Builders<IntegratedUser>.Update.Set(x => x.JiraInstanceUrl, jiraUrl);
                    var options = new FindOneAndUpdateOptions<IntegratedUser, IntegratedUser>
                    {
                        ReturnDocument = ReturnDocument.After
                    };
                    user = await ProcessThrottlingRequest(() => _usersCollection.FindOneAndUpdateAsync(filter, update, options));
                }
                else
                {
                    user = new IntegratedUser
                    {
                        MsTeamsUserId = msTeamsUserId,
                        MsTeamsTenantId = msTeamsTenantId,
                        JiraInstanceUrl = jiraUrl
                    };
                    await ProcessThrottlingRequest(() => _usersCollection.InsertOneAsync(user));
                }
            }

            return user;
        }

        public Task<IntegratedUser> GetUserByTeamsUserIdAndJiraUrl(string msTeamsUserId, string jiraUrl)
        {
            Expression<Func<IntegratedUser, bool>> jiraServerUser = x => x.JiraServerId == jiraUrl && x.MsTeamsUserId == msTeamsUserId;

            var user = ProcessThrottlingRequest(() => _usersCollection
                .Find(jiraServerUser)
                .FirstOrDefaultAsync());

            return user;
        }

        public async Task UpdateUserActiveJiraInstanceForPersonalScope(string msTeamsUserId, string jiraUrl)
        {
            await ResetUserActiveJiraInstanceForPersonalScope(msTeamsUserId);

            var updateBuilder = new UpdateDefinitionBuilder<IntegratedUser>();
            var updateDefinition = updateBuilder
                .Set(x => x.IsUsedForPersonalScope, true)
                .Set(x => x.IsUsedForPersonalScopeBefore, false);

            await ProcessThrottlingRequest(() => _usersCollection.UpdateOneAsync(x => x.MsTeamsUserId == msTeamsUserId && x.JiraInstanceUrl == jiraUrl, updateDefinition));
        }

        public async Task<IntegratedUser> GetOrCreateJiraServerUser(string msTeamsUserId, string msTeamsTenantId, string jiraServerId)
        {
            var users = await ProcessThrottlingRequest(() => _usersCollection
                .Find(x => x.MsTeamsUserId == msTeamsUserId)
                .ToListAsync());

            var addonSettings = await GetJiraServerAddonSettingsByJiraId(jiraServerId);

            var user = users.FirstOrDefault(x => x.JiraServerId == jiraServerId);
            if (addonSettings != null && user == null)
            {
                user = new IntegratedUser
                {
                    MsTeamsUserId = msTeamsUserId,
                    MsTeamsTenantId = msTeamsTenantId,
                    JiraServerId = jiraServerId,
                    JiraInstanceUrl = addonSettings.JiraInstanceUrl,
                    IsUsedForPersonalScope = true
                };
                await ProcessThrottlingRequest(() => _usersCollection.InsertOneAsync(user));
            }

            return user;
        }

        public async Task<JiraAddonSettings> GetJiraServerAddonSettingsByJiraId(string jiraId)
        {
            var addonSettings = await ProcessThrottlingRequest(() => _jiraAddonSettingsCollection
                .Find(x => x.JiraId == jiraId)
                .FirstOrDefaultAsync());

            return addonSettings;
        }

        public async Task CreateOrUpdateJiraServerAddonSettings(string jiraId, string jiraInstanceUrl, string connectionId, string version)
        {
            var addonSettings = await GetJiraServerAddonSettingsByJiraId(jiraId);

            var updateBuilder = new UpdateDefinitionBuilder<JiraAddonSettings>()
                .Set(x => x.JiraId, jiraId)
                .Set(x => x.JiraInstanceUrl, jiraInstanceUrl)
                .Set(x => x.ConnectionId, connectionId)
                .Set(x => x.Version, version);

            var updateDefinition = addonSettings == null
                ? updateBuilder.Set(x => x.CreatedDate, DateTime.Now)
                : updateBuilder.Set(x => x.UpdatedDate, DateTime.Now);

            var options = new UpdateOptions { IsUpsert = true };

            await ProcessThrottlingRequest(() => _jiraAddonSettingsCollection.UpdateOneAsync(x => x.JiraId == jiraId, updateDefinition, options));
        }

        public async Task DeleteJiraServerAddonSettingsByConnectionId(string connectionId)
        {
            var deleteFilter = Builders<JiraAddonSettings>.Filter.Eq(x => x.ConnectionId, connectionId);

            await ProcessThrottlingRequest(() => _jiraAddonSettingsCollection.DeleteOneAsync(deleteFilter));
        }

        public async Task<AtlassianAddonStatus> GetJiraServerAddonStatus(string jiraServerId, string msTeamsUserId)
        {
            var addonSettings = await ProcessThrottlingRequest(() => _jiraAddonSettingsCollection.Find(x => x.JiraId == jiraServerId).FirstOrDefaultAsync());

            var user = await ProcessThrottlingRequest(() => _usersCollection
                .Find(x => x.JiraServerId == jiraServerId && x.MsTeamsUserId == msTeamsUserId).FirstOrDefaultAsync());

            return new AtlassianAddonStatus
            {
                AddonIsInstalled = addonSettings != null,
                AddonIsConnected = user != null,
                Version = addonSettings?.Version
            };
        }

        public async Task<IntegratedUser> GetJiraServerUserWithConfiguredPersonalScope(string msTeamsUserId)
        {
            if (string.IsNullOrEmpty(msTeamsUserId))
            {
                return null;
            }

            // Get Jira Data Center users only
            var users = await ProcessThrottlingRequest(() => _usersCollection.Find(x => x.MsTeamsUserId == msTeamsUserId)
                .SortByDescending(x => x.Id)
                .ToListAsync());

            var user = users.FirstOrDefault(x => x.IsUsedForPersonalScope);

            // update user object only if IsUsedForPersonalScope is false. In another case it's already up-to-date
            if (user != null && !user.IsUsedForPersonalScope)
            {
                var userId = user.Id;
                var updateBuilder = new UpdateDefinitionBuilder<IntegratedUser>();
                var updateDefinition = updateBuilder
                    .Set(x => x.IsUsedForPersonalScope, true)
                    .Set(x => x.IsUsedForPersonalScopeBefore, false);

                await ProcessThrottlingRequest(() => _usersCollection.UpdateOneAsync(x => x.Id == userId, updateDefinition));
            }

            return user;
        }

        public async Task<IntegratedUser> UpdateJiraServerUserActiveJiraInstanceForPersonalScope(string msTeamsUserId, string jiraServerId)
        {
            try
            {
                await ResetUserActiveJiraInstanceForPersonalScope(msTeamsUserId);

                var updateBuilder = new UpdateDefinitionBuilder<IntegratedUser>();
                var updateDefinition = updateBuilder
                    .Set(x => x.IsUsedForPersonalScope, true)
                    .Set(x => x.IsUsedForPersonalScopeBefore, false);

                await ProcessThrottlingRequest(() => _usersCollection.UpdateOneAsync(
                    x => x.MsTeamsUserId == msTeamsUserId && x.JiraServerId == jiraServerId,
                    updateDefinition));
            }
            catch
            {
                return null;
            }

            var user = await ProcessThrottlingRequest(() => _usersCollection.Find(x =>
                x.MsTeamsUserId == msTeamsUserId && x.JiraServerId == jiraServerId && x.IsUsedForPersonalScope).FirstOrDefaultAsync());

            return user;
        }

        public async Task DeleteJiraServerUser(string msTeamsUserId, string jiraId)
        {
            var deleteFilter = Builders<IntegratedUser>.Filter.And(
                Builders<IntegratedUser>.Filter.Eq(x => x.MsTeamsUserId, msTeamsUserId),
                Builders<IntegratedUser>.Filter.Eq(x => x.JiraServerId, jiraId));

            await ProcessThrottlingRequest(() => _usersCollection.DeleteOneAsync(deleteFilter));
        }

        private async Task ResetUserActiveJiraInstanceForPersonalScope(string msTeamsUserId)
        {
            var updateBuilder = new UpdateDefinitionBuilder<IntegratedUser>();
            var updateDefinition = updateBuilder
                .Set(x => x.IsUsedForPersonalScope, false)
                .Set(x => x.IsUsedForPersonalScopeBefore, true);

            var filter = Builders<IntegratedUser>.Filter.Where(x =>
                x.MsTeamsUserId == msTeamsUserId && x.IsUsedForPersonalScope);

            await ProcessThrottlingRequest(() => _usersCollection.UpdateOneAsync(filter, updateDefinition));
        }

        private static async Task<T> ProcessThrottlingRequest<T>(Func<Task<T>> func)
        {
            T result;

            await _openConnectionSemaphore.WaitAsync();
            try
            {
                result = await func();
            }
            finally
            {
                _openConnectionSemaphore.Release();
            }

            return result;
        }

        private static async Task ProcessThrottlingRequest(Func<Task> func)
        {
            await _openConnectionSemaphore.WaitAsync();
            try
            {
                await func();
            }
            finally
            {
                _openConnectionSemaphore.Release();
            }
        }
    }
}
