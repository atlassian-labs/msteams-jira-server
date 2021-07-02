using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoMapper;
using FakeItEasy;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MicrosoftTeamsIntegration.Artifacts.Services.Interfaces;
using MicrosoftTeamsIntegration.Jira.Controllers;
using MicrosoftTeamsIntegration.Jira.Models;
using MicrosoftTeamsIntegration.Jira.Models.Jira;
using MicrosoftTeamsIntegration.Jira.Models.Jira.Issue;
using MicrosoftTeamsIntegration.Jira.Models.Jira.Meta;
using MicrosoftTeamsIntegration.Jira.Services.Interfaces;
using MicrosoftTeamsIntegration.Jira.Settings;
using Xunit;

namespace MicrosoftTeamsIntegration.Jira.Tests
{
    public class JiraApiControllerTest
    {
        private readonly IDatabaseService _fakeDatabaseService = A.Fake<IDatabaseService>();
        private readonly IOptions<AppSettings> _appSettings = new OptionsManager<AppSettings>(
            new OptionsFactory<AppSettings>(
                new List<IConfigureOptions<AppSettings>>(),
                new List<IPostConfigureOptions<AppSettings>>()));
        private readonly IJiraService _fakeJiraService = A.Fake<IJiraService>();
        private readonly IMapper _fakeMapper = A.Fake<IMapper>();
        private readonly IJiraAuthService _fakeJiraAuthService = A.Fake<IJiraAuthService>();
        private readonly IDistributedCacheService _fakeDistributedCacheService = A.Fake<IDistributedCacheService>();
        private readonly ILogger<JiraApiController> _logger = A.Fake<ILogger<JiraApiController>>();
        private readonly IOptions<ClientAppOptions> _clientAppOptions = new OptionsManager<ClientAppOptions>(
            new OptionsFactory<ClientAppOptions>(
                new List<IConfigureOptions<ClientAppOptions>>(),
                new List<IPostConfigureOptions<ClientAppOptions>>()));

        [Fact]
        public async Task GetJiraUrlForPersonalScope_ShouldStatusCodeBe200()
        {
            // arrange
            var user = GetFakeOfVerifiedUser(out JiraApiController jiraApiController);

            A.CallTo(() =>
                _fakeDatabaseService.GetJiraServerUserWithConfiguredPersonalScope(A<string>._)).Returns(user);

            // act
            var result = await jiraApiController.GetJiraUrlForPersonalScope();
            var okResult = result as ObjectResult;

            // assert
            Assert.NotNull(okResult);
            Assert.Equal(200, okResult.StatusCode);
        }

        [Fact]
        public async Task GetAuthUrl_BothDbCalls_ShouldHappen()
        {
            // arrange
            var user = GetFakeOfVerifiedUser(out JiraApiController jiraApiController);

            A.CallTo(() =>
                _fakeDatabaseService.GetOrCreateUser(A<string>._, A<string>._, user.JiraInstanceUrl)).Returns(user);

            A.CallTo(() =>
                _fakeDatabaseService.UpdateUserActiveJiraInstanceForPersonalScope(A<string>._, user.JiraInstanceUrl)).Returns(Task.Delay(1));

            // act
            await jiraApiController.GetAuthUrl(user.JiraInstanceUrl, "test", true);

            A.CallTo(() =>
                _fakeDatabaseService.UpdateUserActiveJiraInstanceForPersonalScope(A<string>._, user.JiraInstanceUrl)).MustHaveHappened();

            A.CallTo(() =>
                _fakeDatabaseService.UpdateUserActiveJiraInstanceForPersonalScope(A<string>._, user.JiraInstanceUrl)).MustHaveHappened();
        }

        [Fact]
        public async Task GetAuthUrl_BothDbCalls_ShouldNotHappen()
        {
            // arrange
            var user = GetFakeOfVerifiedUser(out JiraApiController jiraApiController);

            A.CallTo(() =>
                _fakeDatabaseService.GetOrCreateUser(A<string>._, A<string>._, user.JiraInstanceUrl)).Returns(user);

            A.CallTo(() =>
                _fakeDatabaseService.UpdateUserActiveJiraInstanceForPersonalScope(A<string>._, user.JiraInstanceUrl)).Returns(Task.Delay(1));

            // act
            await jiraApiController.GetAuthUrl(user.JiraInstanceUrl, null, null);

            // assert
            A.CallTo(() =>
                _fakeDatabaseService.UpdateUserActiveJiraInstanceForPersonalScope(A<string>._, user.JiraInstanceUrl)).MustNotHaveHappened();

            A.CallTo(() =>
                _fakeDatabaseService.UpdateUserActiveJiraInstanceForPersonalScope(A<string>._, user.JiraInstanceUrl)).MustNotHaveHappened();
        }

        [Fact]
        public async Task GetJiraAddonStatus_ShouldStatusCodeBe200()
        {
            // arrange
            var user = GetFakeOfVerifiedUser(out JiraApiController jiraApiController);

            A.CallTo(() =>
                _fakeDatabaseService.GetJiraServerAddonStatus(A<string>._, A<string>._)).Returns(new AtlassianAddonStatus { AddonIsInstalled = true });

            // act
            var result = await jiraApiController.GetJiraAddonStatus(user.JiraInstanceUrl);
            var okResult = result as ObjectResult;

            // assert
            Assert.NotNull(okResult);
            Assert.Equal(200, okResult.StatusCode);
        }

        [Fact]
        public async Task CreateIssue_ShouldServiceMethodHappen()
        {
            // arrange
            var user = GetFakeOfVerifiedUser(out JiraApiController jiraApiController);
            dynamic sampleObject = new ExpandoObject();
            sampleObject.summary = string.Empty;
            var model = new CreateJiraIssueRequest()
            {
                Fields = sampleObject,
                MetadataRef = "MetadataRef"
            };

            // act
            A.CallTo(() => _fakeJiraService.CreateIssue(user, A<CreateJiraIssueRequest>._)).Returns(new JiraIssue());

            await jiraApiController.CreateIssue(user.JiraInstanceUrl, model);

            // assert
            A.CallTo(() => _fakeJiraService.CreateIssue(user, A<CreateJiraIssueRequest>._)).MustHaveHappened();
        }

        [Fact]
        public async Task UpdateIssue_ShouldServiceMethodHappen()
        {
            // arrange
            var user = GetFakeOfVerifiedUser(out JiraApiController jiraApiController);

            // act
            A.CallTo(() => _fakeJiraService.UpdateIssue(user, A<string>._, A<JiraIssueFields>._)).Returns(new JiraApiActionCallResponse());

            await jiraApiController.UpdateIssue(user.JiraInstanceUrl, null, new JiraIssueFields());

            // assert
            A.CallTo(() => _fakeJiraService.UpdateIssue(user, A<string>._, A<JiraIssueFields>._)).MustHaveHappened();
        }

        [Fact]
        public async Task UpdateComment_ShouldServiceMethodHappen()
        {
            // arrange
            var user = GetFakeOfVerifiedUser(out JiraApiController jiraApiController);

            // act
            A.CallTo(() => _fakeJiraService.UpdateComment(user, A<string>._, A<string>._, A<string>._))
                .Returns(new JiraComment());

            await jiraApiController.UpdateComment(new UpdateIssueCommentModel() { JiraUrl = user.JiraInstanceUrl });

            // assert
            A.CallTo(() => _fakeJiraService.UpdateComment(user, A<string>._, A<string>._, A<string>._))
                .MustHaveHappened();
        }

        [Fact]
        public async Task AddCommentAndGetItBack_ShouldServiceMethodHappen()
        {
            // arrange
            var user = GetFakeOfVerifiedUser(out JiraApiController jiraApiController);

            // act
            A.CallTo(() => _fakeJiraService.AddCommentAndGetItBack(user, A<string>._, A<string>._))
                .Returns(new JiraComment());

            await jiraApiController.AddCommentAndGetItBack(new AddIssueCommentModel() { JiraUrl = user.JiraInstanceUrl, MetadataRef = "metadata" });

            // assert
            A.CallTo(() => _fakeJiraService.AddCommentAndGetItBack(user, A<string>._, A<string>._))
                .MustHaveHappened();
        }

        [Fact]
        public async Task GetDataAboutMyself_ShouldServiceMethodHappen()
        {
            // arrange
            var user = GetFakeOfVerifiedUser(out JiraApiController jiraApiController);

            // act
            A.CallTo(() => _fakeJiraService.GetDataAboutMyself(user)).Returns(new MyselfInfo());
            await jiraApiController.GetDataAboutMyself(user.JiraInstanceUrl);

            // assert
            A.CallTo(() => _fakeJiraService.GetDataAboutMyself(user)).MustHaveHappened();
        }

        [Fact]
        public async Task UpdateIssueDescription_ShouldServiceMethodHappen()
        {
            // arrange
            var user = GetFakeOfVerifiedUser(out JiraApiController jiraApiController);

            // act
            A.CallTo(() => _fakeJiraService.UpdateDescription(user, A<string>._, A<string>._))
                .Returns(new JiraApiActionCallResponse());

            await jiraApiController.UpdateIssueDescription(user.JiraInstanceUrl, null, new UpdateIssueDescriptionRequestModel());

            // assert
            A.CallTo(() => _fakeJiraService.UpdateDescription(user, A<string>._, A<string>._))
                .MustHaveHappened();
        }

        [Fact]
        public async Task AddComment_ShouldServiceMethodHappen()
        {
            // arrange
            var user = GetFakeOfVerifiedUser(out JiraApiController jiraApiController);

            // act
            A.CallTo(() => _fakeJiraService.AddComment(user, A<string>._, A<string>._))
                .Returns(new JiraApiActionCallResponse());

            await jiraApiController.AddComment(user.JiraInstanceUrl, null, null);

            // assert
            A.CallTo(() => _fakeJiraService.AddComment(user, A<string>._, A<string>._))
                .MustHaveHappened();
        }

        [Fact]
        public async Task UpdateSummary_ShouldServiceMethodHappen()
        {
            // arrange
            var user = GetFakeOfVerifiedUser(out JiraApiController jiraApiController);

            // act
            A.CallTo(() => _fakeJiraService.UpdateSummary(user, A<string>._, A<string>._))
                .Returns(new JiraApiActionCallResponse());

            await jiraApiController.UpdateSummary(user.JiraInstanceUrl, null, null);

            // assert
            A.CallTo(() => _fakeJiraService.UpdateSummary(user, A<string>._, A<string>._))
                .MustHaveHappened();
        }

        [Fact]
        public async Task UpdatePriority_ShouldServiceMethodHappen()
        {
            // arrange
            var user = GetFakeOfVerifiedUser(out JiraApiController jiraApiController);

            // act
            A.CallTo(() => _fakeJiraService.UpdatePriority(user, A<string>._, A<string>._))
                .Returns(new JiraApiActionCallResponse());

            await jiraApiController.UpdatePriority(user.JiraInstanceUrl, null, new JiraIssuePriority());

            // assert
            A.CallTo(() => _fakeJiraService.UpdatePriority(user, A<string>._, A<string>._))
                .MustHaveHappened();
        }

        [Fact]
        public async Task Assignee_ShouldServiceMethodHappen()
        {
            // arrange
            var user = GetFakeOfVerifiedUser(out JiraApiController jiraApiController);

            // act
            A.CallTo(() => _fakeJiraService.Assign(user, A<string>._, A<string>._))
                .Returns(new JiraApiActionCallResponseWithContent<string>());

            await jiraApiController.Assignee(user.JiraInstanceUrl, null, "testUser");

            // assert
            A.CallTo(() => _fakeJiraService.Assign(user, A<string>._, A<string>._))
                .MustHaveHappened();
        }

        [Fact]
        public async Task AssigneeToMe_ShouldServiceMethodHappen()
        {
            // arrange
            var user = GetFakeOfVerifiedUser(out JiraApiController jiraApiController);

            // act
            A.CallTo(() => _fakeJiraService.Assign(user, A<string>._, A<string>._))
                .Returns(new JiraApiActionCallResponseWithContent<string>());

            await jiraApiController.AssigneToMe(user.JiraInstanceUrl, null);

            // assert
            A.CallTo(() => _fakeJiraService.Assign(user, A<string>._, A<string>._))
                .MustHaveHappened();
        }

        [Fact]
        public async Task GetJiraTenantInfo_ShouldServiceMethodHappen()
        {
            // arrange
            var user = GetFakeOfVerifiedUser(out JiraApiController jiraApiController);

            A.CallTo(() => _fakeJiraService.GetJiraTenantInfo(user)).Returns(new JiraTenantInfo());

            // act
            await jiraApiController.GetJiraTenantInfo(user.JiraInstanceUrl);

            // assert
            A.CallTo(() => _fakeJiraService.GetJiraTenantInfo(user)).MustHaveHappened();
        }

        [Theory]
        [InlineData("jql", 0)]
        public async Task Search_ShouldServiceMethodHappen(string jql, int startAt)
        {
            // arrange
            var user = GetFakeOfVerifiedUser(out JiraApiController jiraApiController);

            A.CallTo(() => _fakeJiraService.Search(user, A<SearchForIssuesRequest>._)).Returns(new JiraIssueSearch());

            // act
            await jiraApiController.Search(user.JiraInstanceUrl, jql, startAt);

            // assert
            A.CallTo(() => _fakeJiraService.Search(user, A<SearchForIssuesRequest>._)).MustHaveHappened();
        }

        [Fact]
        public async Task Projects_ShouldServiceMethodHappen()
        {
            // arrange
            var user = GetFakeOfVerifiedUser(out JiraApiController jiraApiController);

            A.CallTo(() => _fakeJiraService.GetProjects(user, A<bool>._)).Returns(new List<JiraProject>());

            // act
            await jiraApiController.Projects(user.JiraInstanceUrl, false);

            // assert
            A.CallTo(() => _fakeJiraService.GetProjects(user, A<bool>._)).MustHaveHappened();
        }

        [Fact]
        public async Task Project_ShouldServiceMethodHappen()
        {
            // arrange
            var user = GetFakeOfVerifiedUser(out JiraApiController jiraApiController);

            A.CallTo(() => _fakeJiraService.GetProject(user, A<string>._)).Returns(new JiraProject());

            // act
            await jiraApiController.Project(user.JiraInstanceUrl, string.Empty);

            // assert
            A.CallTo(() => _fakeJiraService.GetProject(user, A<string>._)).MustHaveHappened();
        }

        [Fact]
        public async Task Statuses_ShouldServiceMethodHappen()
        {
            // arrange
            var user = GetFakeOfVerifiedUser(out JiraApiController jiraApiController);

            A.CallTo(() => _fakeJiraService.GetStatuses(user)).Returns(new List<JiraIssueStatus>());

            // act
            await jiraApiController.Statuses(user.JiraInstanceUrl);

            // assert
            A.CallTo(() => _fakeJiraService.GetStatuses(user)).MustHaveHappened();
        }

        [Fact]
        public async Task GetStatusesByProject_ShouldServiceMethodHappen()
        {
            // arrange
            var user = GetFakeOfVerifiedUser(out JiraApiController jiraApiController);

            A.CallTo(() => _fakeJiraService.GetStatusesByProject(user, A<string>._)).Returns(new List<JiraIssueStatus>());

            // act
            await jiraApiController.GetStatusesByProject(user.JiraInstanceUrl, string.Empty);

            // assert
            A.CallTo(() => _fakeJiraService.GetStatusesByProject(user, A<string>._)).MustHaveHappened();
        }

        [Fact]
        public async Task Priorities_ShouldServiceMethodHappen()
        {
            // arrange
            var user = GetFakeOfVerifiedUser(out JiraApiController jiraApiController);

            A.CallTo(() => _fakeJiraService.GetPriorities(user)).Returns(new List<JiraIssuePriority>());

            // act
            await jiraApiController.Priorities(user.JiraInstanceUrl);

            // assert
            A.CallTo(() => _fakeJiraService.GetPriorities(user)).MustHaveHappened();
        }

        [Fact]
        public async Task Types_ShouldServiceMethodHappen()
        {
            // arrange
            var user = GetFakeOfVerifiedUser(out JiraApiController jiraApiController);

            A.CallTo(() => _fakeJiraService.GetTypes(user)).Returns(new List<JiraIssueType>());

            // act
            await jiraApiController.Types(user.JiraInstanceUrl);

            // assert
            A.CallTo(() => _fakeJiraService.GetTypes(user)).MustHaveHappened();
        }

        [Fact]
        public async Task Filters_ShouldServiceMethodHappen()
        {
            // arrange
            var user = GetFakeOfVerifiedUser(out JiraApiController jiraApiController);

            A.CallTo(() => _fakeJiraService.GetFilters(user)).Returns(new List<JiraIssueFilter>());

            // act
            await jiraApiController.Filters(user.JiraInstanceUrl);

            // assert
            A.CallTo(() => _fakeJiraService.GetFilters(user)).MustHaveHappened();
        }

        [Fact]
        public async Task SearchProjects_ShouldServiceMethodHappen()
        {
            // arrange
            var user = GetFakeOfVerifiedUser(out JiraApiController jiraApiController);

            A.CallTo(() => _fakeJiraService.GetProjectsByName(user, A<bool>._, A<string>._)).Returns(new List<JiraProject>());

            // act
            await jiraApiController.SearchProjects(user.JiraInstanceUrl, false);

            // assert
            A.CallTo(() => _fakeJiraService.GetProjectsByName(user, A<bool>._, A<string>._)).MustHaveHappened();
        }

        [Fact]
        public async Task SearchFilters_ShouldServiceMethodHappen()
        {
            // arrange
            var user = GetFakeOfVerifiedUser(out JiraApiController jiraApiController);

            A.CallTo(() => _fakeJiraService.SearchFilters(user, A<string>._)).Returns(new List<JiraIssueFilter>());

            // act
            await jiraApiController.SearchFilters(user.JiraInstanceUrl);

            // assert
            A.CallTo(() => _fakeJiraService.SearchFilters(user, A<string>._)).MustHaveHappened();
        }

        [Fact]
        public async Task GetFilter_ShouldServiceMethodHappen()
        {
            // arrange
            var user = GetFakeOfVerifiedUser(out JiraApiController jiraApiController);

            A.CallTo(() => _fakeJiraService.GetFilter(user, A<string>._)).Returns(new JiraIssueFilter());

            // act
            await jiraApiController.GetFilter(user.JiraInstanceUrl, null);

            // assert
            A.CallTo(() => _fakeJiraService.GetFilter(user, A<string>._)).MustHaveHappened();
        }

        [Fact]
        public async Task GetIssueByIdOrKey_ShouldServiceMethodHappen()
        {
            // arrange
            var user = GetFakeOfVerifiedUser(out JiraApiController jiraApiController);

            A.CallTo(() => _fakeJiraService.GetIssueByIdOrKey(user, A<string>._)).Returns(new JiraIssue());

            // act
            await jiraApiController.GetIssueByIdOrKey(user.JiraInstanceUrl, null);

            // assert
            A.CallTo(() => _fakeJiraService.GetIssueByIdOrKey(user, A<string>._)).MustHaveHappened();
        }

        [Fact]
        public async Task FavouriteFilters_ShouldServiceMethodHappen()
        {
            // arrange
            var user = GetFakeOfVerifiedUser(out JiraApiController jiraApiController);

            A.CallTo(() => _fakeJiraService.GetFavouriteFilters(user)).Returns(new List<JiraIssueFilter>());

            // act
            await jiraApiController.FavouriteFilters(user.JiraInstanceUrl);

            // assert
            A.CallTo(() => _fakeJiraService.GetFavouriteFilters(user)).MustHaveHappened();
        }

        [Fact]
        public async Task GetCreateMetaIssueTypes_ShouldServiceMethodHappen()
        {
            // arrange
            var user = GetFakeOfVerifiedUser(out JiraApiController jiraApiController);

            A.CallTo(() => _fakeJiraService.GetCreateMetaIssueTypes(user, A<string>._)).Returns(new List<JiraIssueTypeMeta>());

            // act
            await jiraApiController.GetCreateMetaIssueTypes(user.JiraInstanceUrl, null);

            // assert
            A.CallTo(() => _fakeJiraService.GetCreateMetaIssueTypes(user, A<string>._)).MustHaveHappened();
        }

        [Fact]
        public async Task GetCreateMetaIssueTypeFields_ShouldServiceMethodHappen()
        {
            // arrange
            var user = GetFakeOfVerifiedUser(out JiraApiController jiraApiController);

            A.CallTo(() => _fakeJiraService.GetCreateMetaIssueTypeFields(user, A<string>._, A<string>._, A<string>._))
                .Returns(new ExpandoObject());

            // act
            await jiraApiController.GetCreateMetaIssueTypeFields(user.JiraInstanceUrl, null, null, null);

            // assert
            A.CallTo(() => _fakeJiraService.GetCreateMetaIssueTypeFields(user, A<string>._, A<string>._, A<string>._))
                .MustHaveHappened();
        }

        [Fact]
        public async Task GetIssueEditMeta_ShouldServiceMethodHappen()
        {
            // arrange
            var user = GetFakeOfVerifiedUser(out JiraApiController jiraApiController);

            A.CallTo(() => _fakeJiraService.GetIssueEditMeta(user, A<string>._)).Returns(new JiraIssueEditMeta());

            // act
            await jiraApiController.GetIssueEditMeta(user.JiraInstanceUrl, null);

            // assert
            A.CallTo(() => _fakeJiraService.GetIssueEditMeta(user, A<string>._)).MustHaveHappened();
        }

        [Fact]
        public async Task SearchAssignable_ShouldServiceMethodHappen()
        {
            // arrange
            var user = GetFakeOfVerifiedUser(out JiraApiController jiraApiController);

            A.CallTo(() => _fakeJiraService.SearchAssignable(user, A<string>._, A<string>._, A<string>._))
                .Returns(new JiraUser[2]);

            // act
            await jiraApiController.SearchAssignable(user.JiraInstanceUrl, null, null, null);

            // assert
            A.CallTo(() => _fakeJiraService.SearchAssignable(user, A<string>._, A<string>._, A<string>._))
                .MustHaveHappened();
        }

        [Fact]
        public async Task SearchAssignableMultiProject_ShouldServiceMethodHappen()
        {
            // arrange
            var user = GetFakeOfVerifiedUser(out JiraApiController jiraApiController);

            A.CallTo(() => _fakeJiraService.SearchAssignableMultiProject(user, A<string>._, A<string>._))
                .Returns(new JiraUser[2]);

            // act
            await jiraApiController.SearchAssignableMultiProject(user.JiraInstanceUrl, null);

            // assert
            A.CallTo(() => _fakeJiraService.SearchAssignableMultiProject(user, A<string>._, A<string>._))
                .MustHaveHappened();
        }

        [Fact]
        public async Task SearchUsers_ShouldServiceMethodHappen()
        {
            // arrange
            var user = GetFakeOfVerifiedUser(out JiraApiController jiraApiController);

            A.CallTo(() => _fakeJiraService.SearchUsers(user, A<string>._))
                .Returns(new JiraUser[2]);

            // act
            await jiraApiController.SearchUsers(user.JiraInstanceUrl, null);

            // assert
            A.CallTo(() => _fakeJiraService.SearchUsers(user, A<string>._))
                .MustHaveHappened();
        }

        [Fact]
        public async Task GetTransitions_ShouldServiceMethodHappen()
        {
            // arrange
            var user = GetFakeOfVerifiedUser(out JiraApiController jiraApiController);

            A.CallTo(() => _fakeJiraService.GetTransitions(user, A<string>._))
                .Returns(new JiraTransitionsResponse());

            // act
            await jiraApiController.GetTransitions(user.JiraInstanceUrl, null);

            // assert
            A.CallTo(() => _fakeJiraService.GetTransitions(user, A<string>._))
                .MustHaveHappened();
        }

        [Fact]
        public async Task GetMyPermissions_ShouldServiceMethodHappen()
        {
            // arrange
            var user = GetFakeOfVerifiedUser(out JiraApiController jiraApiController);

            A.CallTo(() => _fakeJiraService.GetMyPermissions(user, A<string>._, A<string>._, A<string>._))
                .Returns(new JiraPermissionsResponse());

            // act
            await jiraApiController.GetMyPermissions(user.JiraInstanceUrl, null, null, null);

            // assert
            A.CallTo(() => _fakeJiraService.GetMyPermissions(user, A<string>._, A<string>._, A<string>._))
                .MustHaveHappened();
        }

        [Fact]
        public async Task GetFieldAutocompleteData_ShouldServiceMethodHappen()
        {
            // arrange
            var user = GetFakeOfVerifiedUser(out JiraApiController jiraApiController);

            A.CallTo(() => _fakeJiraService.GetFieldAutocompleteData(user, A<string>._))
                .Returns(new List<JiraIssueAutocompleteData>());

            // act
            await jiraApiController.GetFieldAutocompleteData(user.JiraInstanceUrl);

            // assert
            A.CallTo(() => _fakeJiraService.GetFieldAutocompleteData(user, A<string>._))
                .MustHaveHappened();
        }

        [Fact]
        public async Task GetSprints_ShouldServiceMethodHappen()
        {
            // arrange
            var user = GetFakeOfVerifiedUser(out JiraApiController jiraApiController);

            A.CallTo(() => _fakeJiraService.GetAllSprintsForProject(user, A<string>._))
                .Returns(new List<JiraIssueSprint>());

            // act
            await jiraApiController.GetSprints(user.JiraInstanceUrl, null);

            // assert
            A.CallTo(() => _fakeJiraService.GetAllSprintsForProject(user, A<string>._))
                .MustHaveHappened();
        }

        [Fact]
        public async Task GetEpics_ShouldServiceMethodHappen()
        {
            // arrange
            var user = GetFakeOfVerifiedUser(out JiraApiController jiraApiController);

            A.CallTo(() => _fakeJiraService.GetAllEpicsForProject(user, A<string>._))
                .Returns(new List<JiraIssueEpic>());

            // act
            await jiraApiController.GetEpics(user.JiraInstanceUrl, null);

            // assert
            A.CallTo(() => _fakeJiraService.GetAllEpicsForProject(user, A<string>._))
                .MustHaveHappened();
        }

        [Fact]
        public async Task DoTransition_ShouldServiceMethodHappen()
        {
            // arrange
            var user = GetFakeOfVerifiedUser(out JiraApiController jiraApiController);

            A.CallTo(() => _fakeJiraService.DoTransition(user, A<string>._, A<DoTransitionRequest>._))
                .Returns(new JiraApiActionCallResponse());

            // act
            await jiraApiController.DoTransition(user.JiraInstanceUrl, null, new DoTransitionRequest());

            // assert
            A.CallTo(() => _fakeJiraService.DoTransition(user, A<string>._, A<DoTransitionRequest>._))
                .MustHaveHappened();
        }

        [Fact]
        public async Task Logout_ShouldServiceMethodHappen()
        {
            // arrange
            var user = GetFakeOfVerifiedUser(out JiraApiController jiraApiController);

            A.CallTo(() => _fakeJiraAuthService.Logout(user)).Returns(new JiraAuthResponse());

            // act
            await jiraApiController.Logout(user.JiraInstanceUrl);

            // assert
            A.CallTo(() => _fakeJiraAuthService.Logout(user))
                .MustHaveHappened();
        }

        [Fact]
        public async Task ValidateConnection_AddonNotInstalled()
        {
            // arrange
            var user = GetFakeOfVerifiedUser(out JiraApiController jiraApiController);

            A.CallTo(() => _fakeDatabaseService.GetJiraServerAddonSettingsByJiraId(A<string>._)).Returns(new JiraAddonSettings());

            // act
            await jiraApiController.ValidateConnection(user.JiraInstanceUrl);

            // assert
            A.CallTo(() => _fakeDatabaseService.GetJiraServerAddonSettingsByJiraId(A<string>._))
                .MustHaveHappened();
        }

        [Fact]
        public async Task SubmitLoginInfoTest()
        {
            // arrange
            var jiraApiController = CreateJiraApiController();

            A.CallTo(() => _fakeDatabaseService.GetJiraServerAddonStatus(A<string>._, A<string>._)).Returns(new AtlassianAddonStatus());

            // act
            await jiraApiController.SubmitLoginInfo(new JiraAuthParamMessage() { JiraId = "jiraId" });

            // assert
            A.CallTo(() => _fakeDatabaseService.GetJiraServerAddonStatus(A<string>._, A<string>._))
                .MustHaveHappened();
        }

        [Theory]
        [InlineData("https://tyr-dev.atlassian.net")]
        public async Task GetAndVerifyUser_UsersJiraUrl_ShouldGetUserWithAuthInfo(string jiraUrl)
        {
            // arrange
            var user = JiraDataGenerator.GenerateUser();
            user.JiraInstanceUrl = jiraUrl;

            var jiraApiController = CreateJiraApiController();

            A.CallTo(() =>
                _fakeDatabaseService.GetUserByTeamsUserIdAndJiraUrl(A<string>._, user.JiraInstanceUrl)).Returns(user);

            A.CallTo(() =>
                _fakeJiraAuthService.IsJiraConnected(user)).Returns(true);

            // act
            await jiraApiController.GetDataAboutMyself(jiraUrl);

            A.CallTo(() =>
                    _fakeDatabaseService.GetUserByTeamsUserIdAndJiraUrl(A<string>._, user.JiraInstanceUrl))
                .MustHaveHappened();

            A.CallTo(() =>
                _fakeJiraAuthService.IsJiraConnected(user)).MustHaveHappened();
        }

        [Theory]
        [InlineData(false)]
        public async Task GetAndVerifyUser_AddonIsInstalled_ShouldThrowException(bool addonIsInstalled)
        {
            // arrange
            var jiraApiController = CreateJiraApiController();

            // user without JiraAuthInfo
            var user = new IntegratedUser
            {
                MsTeamsUserId = Guid.NewGuid().ToString(),
                JiraInstanceUrl = "tyr-dev.atlassian.net"
            };

            A.CallTo(() =>
                _fakeDatabaseService.GetUserByTeamsUserIdAndJiraUrl(A<string>._, user.JiraInstanceUrl)).Returns(user);

            A.CallTo(() =>
                _fakeJiraAuthService.IsJiraConnected(user)).Returns(addonIsInstalled);

            // act
            // assert
            var result = await Assert.ThrowsAnyAsync<Exception>(
                () => jiraApiController.GetDataAboutMyself(user.JiraInstanceUrl));
        }

        private IntegratedUser GetFakeOfVerifiedUser(out JiraApiController jiraApiController, string jiraUrl = "https://tyr-dev.atlassian.net")
        {
            var user = JiraDataGenerator.GenerateUser();
            user.JiraInstanceUrl = jiraUrl;

            jiraApiController = CreateJiraApiController();

            A.CallTo(() =>
                _fakeDatabaseService.GetUserByTeamsUserIdAndJiraUrl(A<string>._, user.JiraInstanceUrl)).Returns(user);

            A.CallTo(() => _fakeJiraAuthService.IsJiraConnected(user)).Returns(true);

            return user;
        }

        private JiraApiController CreateJiraApiController()
        {
            var jiraApiController = A.Fake<JiraApiController>(
                    x => x.WithArgumentsForConstructor(new object[] {
                            _fakeDatabaseService,
                            _appSettings,
                            _fakeJiraService,
                            _fakeMapper,
                            _fakeJiraAuthService,
                            _fakeDistributedCacheService,
                            _logger,
                            _clientAppOptions
                    }));

            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim("http://schemas.microsoft.com/identity/claims/objectidentifier", "example claim value")
            }));

            jiraApiController.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };

            return jiraApiController;
        }
    }
}
