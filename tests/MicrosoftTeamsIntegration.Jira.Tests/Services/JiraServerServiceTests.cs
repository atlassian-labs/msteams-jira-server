using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Threading;
using System.Threading.Tasks;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MicrosoftTeamsIntegration.Jira.Models;
using MicrosoftTeamsIntegration.Jira.Models.Jira;
using MicrosoftTeamsIntegration.Jira.Models.Jira.Issue;
using MicrosoftTeamsIntegration.Jira.Models.Jira.Meta;
using MicrosoftTeamsIntegration.Jira.Models.Jira.Transition;
using MicrosoftTeamsIntegration.Jira.Services;
using MicrosoftTeamsIntegration.Jira.Services.Interfaces;
using MicrosoftTeamsIntegration.Jira.Services.SignalR;
using MicrosoftTeamsIntegration.Jira.Services.SignalR.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace MicrosoftTeamsIntegration.Jira.Tests.Services
{
    public class JiraServerServiceTests
    {
        private readonly ISignalRService _signalRService = A.Fake<ISignalRService>();
        private readonly IDatabaseService _databaseService = A.Fake<IDatabaseService>();
        private readonly IJiraAuthService _jiraAuthService = A.Fake<IJiraAuthService>();
        private readonly ILogger<JiraService> _logger = new NullLogger<JiraService>();

        [Fact]
        public async Task GetUserNameOrAccountId_SuccessfullyGetsUsernameFromJira_ReturnsIt()
        {
            // Arrange
            const string username = "Arthur Morgan";
            A.CallTo(() =>
                    _signalRService.SendRequestAndWaitForResponse(A<string>._, A<string>._, CancellationToken.None))
                .Returns(new SignalRResponse
                {
                    Received = true,
                    Message = JsonConvert.SerializeObject(new JiraResponse<MyselfInfo>
                        { ResponseCode = 200, Response = new MyselfInfo { Name = username } })
                });
            var jiraServerService = new JiraService(_signalRService, _databaseService, _logger);

            // Act
            var result = await jiraServerService.GetUserNameOrAccountId(new IntegratedUser()
            {
                AccessToken = "token"
            });

            // Assert
            Assert.Equal(username, result);
        }

        [Fact]
        public async Task GetUserNameOrAccountId_DoesNotGetsUsernameFromJira_ReturnsNull()
        {
            // Arrange
            A.CallTo(() =>
                    _signalRService.SendRequestAndWaitForResponse(A<string>._, A<string>._, CancellationToken.None))
                .Returns(new SignalRResponse
                {
                    Received = true,
                    Message = JsonConvert.SerializeObject(new JiraResponse<MyselfInfo>
                        { ResponseCode = 200, Response = new MyselfInfo() })
                });
            var jiraServerService = new JiraService(_signalRService, _databaseService, _logger);

            // Act
            var result = await jiraServerService.GetUserNameOrAccountId(new IntegratedUser());

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetUserNameOrAccountId_ExceptionDuringGettingUsernameFromJira_ReturnsNull()
        {
            // Arrange
            A.CallTo(() =>
                    _signalRService.SendRequestAndWaitForResponse(A<string>._, A<string>._, CancellationToken.None))
                .Throws<NullReferenceException>();
            var jiraServerService = new JiraService(_signalRService, _databaseService, _logger);

            // Act
            var result = await jiraServerService.GetUserNameOrAccountId(new IntegratedUser());

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetIssueEditMeta()
        {
            const string name = "Test Name";

            A.CallTo(() =>
                    _signalRService.SendRequestAndWaitForResponse(A<string>._, A<string>._, CancellationToken.None))
                .Returns(new SignalRResponse
                {
                    Received = true,
                    Message = JsonConvert.SerializeObject(new JiraResponse<JiraIssueEditMeta>
                    {
                        ResponseCode = 200,
                        Response = new JiraIssueEditMeta()
                        {
                            Fields = new JiraIssueEditMetaFields()
                            {
                                Comment = new JiraIssueFieldMeta<string>()
                                {
                                    Name = name
                                }
                            }
                        }
                    })
                });

            var jiraServerService = new JiraService(_signalRService, _databaseService, _logger);

            var result = await jiraServerService.GetIssueEditMeta(new IntegratedUser() { AccessToken = "token" }, string.Empty);

            Assert.NotNull(result);
            Assert.Equal(name, result.Fields.Comment.Name);
        }

        [Fact]
        public async Task GetTypes()
        {
            const string id = "Test Name";

            A.CallTo(() =>
                    _signalRService.SendRequestAndWaitForResponse(A<string>._, A<string>._, CancellationToken.None))
                .Returns(new SignalRResponse
                {
                    Received = true,
                    Message = JsonConvert.SerializeObject(new JiraResponse<List<JiraIssueType>>
                    {
                        ResponseCode = 200,
                        Response = new List<JiraIssueType>()
                        {
                            new JiraIssueType()
                            {
                                Id = id
                            }
                        }
                    })
                });

            var jiraServerService = new JiraService(_signalRService, _databaseService, _logger);

            var result = await jiraServerService.GetTypes(new IntegratedUser() { AccessToken = "token" });

            Assert.NotNull(result);
            Assert.Equal(id, result[0].Id);
        }

        [Fact]
        public async Task GetGetFieldAutocompleteDataTypes()
        {
            const string displayName = "Display Name";

            A.CallTo(() =>
                    _signalRService.SendRequestAndWaitForResponse(A<string>._, A<string>._, CancellationToken.None))
                .Returns(new SignalRResponse
                {
                    Received = true,
                    Message = JsonConvert.SerializeObject(new JiraResponse<JiraIssueAutocompleteResult>
                    {
                        ResponseCode = 200,
                        Response = new JiraIssueAutocompleteResult()
                        {
                            Results = new List<JiraIssueAutocompleteData>()
                            {
                                new JiraIssueAutocompleteData()
                                {
                                    DisplayName = displayName
                                }
                            }
                        }
                    })
                });

            var jiraServerService = new JiraService(_signalRService, _databaseService, _logger);

            var result = await jiraServerService.GetFieldAutocompleteData(new IntegratedUser() { AccessToken = "token" }, string.Empty);

            Assert.NotNull(result);
            Assert.Equal(displayName, result[0].DisplayName);
        }

        [Fact]
        public async Task CreateIssue()
        {
            const string id = "id Name";

            A.CallTo(() =>
                    _signalRService.SendRequestAndWaitForResponse(A<string>._, A<string>._, CancellationToken.None))
                .Returns(new SignalRResponse
                {
                    Received = true,
                    Message = JsonConvert.SerializeObject(new JiraResponse<JiraIssue>
                    {
                        ResponseCode = 200,
                        Response = new JiraIssue()
                        {
                            Id = id
                        }
                    })
                });

            var jiraServerService = new JiraService(_signalRService, _databaseService, _logger);

            var result = await jiraServerService.CreateIssue(new IntegratedUser() { AccessToken = "token" }, new CreateJiraIssueRequest());

            Assert.NotNull(result);
            Assert.Equal(id, result.Id);
        }

        [Fact]
        public async Task GetProjects()
        {
            const string id = "id Name";

            A.CallTo(() =>
                    _signalRService.SendRequestAndWaitForResponse(A<string>._, A<string>._, CancellationToken.None))
                .Returns(new SignalRResponse
                {
                    Received = true,
                    Message = JsonConvert.SerializeObject(new JiraResponse<List<JiraProject>>
                    {
                        ResponseCode = 200,
                        Response = new List<JiraProject>()
                        {
                            new JiraProject()
                            {
                                Id = id
                            }
                        }
                    })
                });

            var jiraServerService = new JiraService(_signalRService, _databaseService, _logger);

            var result = await jiraServerService.GetProjects(new IntegratedUser() { AccessToken = "token" }, false);

            Assert.NotNull(result);
            Assert.Equal(id, result[0].Id);
        }

        [Fact]
        public async Task GetProject()
        {
            const string id = "id Name";

            A.CallTo(() =>
                    _signalRService.SendRequestAndWaitForResponse(A<string>._, A<string>._, CancellationToken.None))
                .Returns(new SignalRResponse
                {
                    Received = true,
                    Message = JsonConvert.SerializeObject(new JiraResponse<JiraProject>
                    {
                        ResponseCode = 200,
                        Response = new JiraProject()
                        {
                            Id = id
                        }
                    })
                });

            var jiraServerService = new JiraService(_signalRService, _databaseService, _logger);

            var result = await jiraServerService.GetProject(new IntegratedUser() { AccessToken = "token" }, string.Empty);

            Assert.NotNull(result);
            Assert.Equal(id, result.Id);
        }

        [Fact]
        public async Task GetPriorities()
        {
            const string id = "id Name";

            A.CallTo(() =>
                    _signalRService.SendRequestAndWaitForResponse(A<string>._, A<string>._, CancellationToken.None))
                .Returns(new SignalRResponse
                {
                    Received = true,
                    Message = JsonConvert.SerializeObject(new JiraResponse<List<JiraIssuePriority>>
                    {
                        ResponseCode = 200,
                        Response = new List<JiraIssuePriority>()
                        {
                            new JiraIssuePriority()
                            {
                                Id = id
                            }
                        }
                    })
                });

            var jiraServerService = new JiraService(_signalRService, _databaseService, _logger);

            var result = await jiraServerService.GetPriorities(new IntegratedUser() { AccessToken = "token" });

            Assert.NotNull(result);
            Assert.Equal(id, result[0].Id);
        }

        [Fact]
        public async Task GetStatuses()
        {
            const string id = "id Name";

            A.CallTo(() =>
                    _signalRService.SendRequestAndWaitForResponse(A<string>._, A<string>._, CancellationToken.None))
                .Returns(new SignalRResponse
                {
                    Received = true,
                    Message = JsonConvert.SerializeObject(new JiraResponse<List<JiraIssueStatus>>
                    {
                        ResponseCode = 200,
                        Response = new List<JiraIssueStatus>()
                        {
                            new JiraIssueStatus()
                            {
                                Id = id
                            }
                        }
                    })
                });

            var jiraServerService = new JiraService(_signalRService, _databaseService, _logger);

            var result = await jiraServerService.GetStatuses(new IntegratedUser() { AccessToken = "token" });

            Assert.NotNull(result);
            Assert.Equal(id, result[0].Id);
        }

        [Fact]
        public async Task GetStatusesByProject()
        {
            const string id = "id Name";

            A.CallTo(() =>
                    _signalRService.SendRequestAndWaitForResponse(A<string>._, A<string>._, CancellationToken.None))
                .Returns(new SignalRResponse
                {
                    Received = true,
                    Message = JsonConvert.SerializeObject(new JiraResponse<List<JiraStatusesResponse>>
                    {
                        ResponseCode = 200,
                        Response = new List<JiraStatusesResponse>()
                        {
                            new JiraStatusesResponse()
                            {
                                Statuses = new List<JiraIssueStatus>()
                                {
                                    new JiraIssueStatus()
                                    {
                                        Id = id
                                    }
                                }
                            }
                        }
                    })
                });

            var jiraServerService = new JiraService(_signalRService, _databaseService, _logger);

            var result = await jiraServerService.GetStatusesByProject(new IntegratedUser() { AccessToken = "token" }, string.Empty);

            Assert.NotNull(result);
            Assert.Equal(id, result[0].Id);
        }

        [Fact]
        public async Task GetFilters()
        {
            const string id = "id Name";

            A.CallTo(() =>
                    _signalRService.SendRequestAndWaitForResponse(A<string>._, A<string>._, CancellationToken.None))
                .Returns(new SignalRResponse
                {
                    Received = true,
                    Message = JsonConvert.SerializeObject(new JiraResponse<List<JiraIssueFilter>>
                    {
                        ResponseCode = 200,
                        Response = new List<JiraIssueFilter>()
                        {
                            new JiraIssueFilter()
                            {
                                Id = id
                            }
                        }
                    })
                });

            var jiraServerService = new JiraService(_signalRService, _databaseService, _logger);

            var result = await jiraServerService.GetFilters(new IntegratedUser() { AccessToken = "token" });

            Assert.NotNull(result);
            Assert.Equal(id, result[0].Id);
        }

        [Fact]
        public async Task SearchFilters()
        {
            const string id = "id Name";

            A.CallTo(() =>
                    _signalRService.SendRequestAndWaitForResponse(A<string>._, A<string>._, CancellationToken.None))
                .Returns(new SignalRResponse
                {
                    Received = true,
                    Message = JsonConvert.SerializeObject(new JiraResponse<List<JiraIssueFilter>>
                    {
                        ResponseCode = 200,
                        Response = new List<JiraIssueFilter>()
                        {
                            new JiraIssueFilter()
                            {
                                Id = id
                            }
                        }
                    })
                });

            var jiraServerService = new JiraService(_signalRService, _databaseService, _logger);

            var result = await jiraServerService.SearchFilters(new IntegratedUser() { AccessToken = "token" }, string.Empty);

            Assert.NotNull(result);
            Assert.Equal(id, result[0].Id);
        }

        [Fact]
        public async Task GetFilter()
        {
            const string id = "id Name";

            A.CallTo(() =>
                    _signalRService.SendRequestAndWaitForResponse(A<string>._, A<string>._, CancellationToken.None))
                .Returns(new SignalRResponse
                {
                    Received = true,
                    Message = JsonConvert.SerializeObject(new JiraResponse<JiraIssueFilter>
                    {
                        ResponseCode = 200,
                        Response = new JiraIssueFilter()
                        {
                            Id = id
                        }
                    })
                });

            var jiraServerService = new JiraService(_signalRService, _databaseService, _logger);

            var result = await jiraServerService.GetFilter(new IntegratedUser() { AccessToken = "token" }, string.Empty);

            Assert.NotNull(result);
            Assert.Equal(id, result.Id);
        }

        [Fact]
        public async Task GetFavouriteFilters()
        {
            const string id = "id Name";

            A.CallTo(() =>
                    _signalRService.SendRequestAndWaitForResponse(A<string>._, A<string>._, CancellationToken.None))
                .Returns(new SignalRResponse
                {
                    Received = true,
                    Message = JsonConvert.SerializeObject(new JiraResponse<List<JiraIssueFilter>>
                    {
                        ResponseCode = 200,
                        Response = new List<JiraIssueFilter>()
                        {
                            new JiraIssueFilter()
                            {
                                Id = id
                            }
                        }
                    })
                });

            var jiraServerService = new JiraService(_signalRService, _databaseService, _logger);

            var result = await jiraServerService.GetFavouriteFilters(new IntegratedUser() { AccessToken = "token" });

            Assert.NotNull(result);
            Assert.Equal(id, result[0].Id);
        }

        [Fact]
        public async Task GetMyPermissions()
        {
            const string description = "Description";

            A.CallTo(() =>
                    _signalRService.SendRequestAndWaitForResponse(A<string>._, A<string>._, CancellationToken.None))
                .Returns(new SignalRResponse
                {
                    Received = true,
                    Message = JsonConvert.SerializeObject(new JiraResponse<JiraPermissionsResponse>
                    {
                        ResponseCode = 200,
                        Response = new JiraPermissionsResponse()
                        {
                            Permissions = new JiraPermissions()
                            {
                                AddComments = new JiraPermission()
                                {
                                    Description = description
                                }
                            }
                        }
                    })
                });

            var jiraServerService = new JiraService(_signalRService, _databaseService, _logger);

            var result = await jiraServerService.GetMyPermissions(new IntegratedUser() { AccessToken = "token" }, string.Empty, string.Empty, string.Empty);

            Assert.NotNull(result);
            Assert.Equal(description, result.Permissions.AddComments.Description);
        }

        [Fact]
        public async Task GetIssueByIdOrKey()
        {
            const string id = "id Name";

            A.CallTo(() =>
                    _signalRService.SendRequestAndWaitForResponse(A<string>._, A<string>._, CancellationToken.None))
                .Returns(new SignalRResponse
                {
                    Received = true,
                    Message = JsonConvert.SerializeObject(new JiraResponse<JiraIssue>
                    {
                        ResponseCode = 200,
                        Response = new JiraIssue()
                        {
                            Id = id
                        }
                    })
                });

            var jiraServerService = new JiraService(_signalRService, _databaseService, _logger);

            var result = await jiraServerService.GetIssueByIdOrKey(new IntegratedUser() { AccessToken = "token" }, string.Empty);

            Assert.NotNull(result);
            Assert.Equal(id, result.Id);
        }

        [Fact]
        public async Task GetTransitions()
        {
            const string id = "id Name";

            A.CallTo(() =>
                    _signalRService.SendRequestAndWaitForResponse(A<string>._, A<string>._, CancellationToken.None))
                .Returns(new SignalRResponse
                {
                    Received = true,
                    Message = JsonConvert.SerializeObject(new JiraResponse<JiraTransitionsResponse>
                    {
                        ResponseCode = 200,
                        Response = new JiraTransitionsResponse()
                        {
                            Transitions = new List<JiraTransition>()
                            {
                                new JiraTransition()
                                {
                                    Id = id
                                }
                            }
                        }
                    })
                });

            var jiraServerService = new JiraService(_signalRService, _databaseService, _logger);

            var result = await jiraServerService.GetTransitions(new IntegratedUser() { AccessToken = "token" }, string.Empty);

            Assert.NotNull(result);
            Assert.Equal(id, result.Transitions[0].Id);
        }

        [Fact]
        public async Task AddCommentAndGetItBack()
        {
            const string id = "id Name";

            A.CallTo(() =>
                    _signalRService.SendRequestAndWaitForResponse(A<string>._, A<string>._, CancellationToken.None))
                .Returns(new SignalRResponse
                {
                    Received = true,
                    Message = JsonConvert.SerializeObject(new JiraResponse<JiraComment>
                    {
                        ResponseCode = 200,
                        Response = new JiraComment()
                        {
                           Id = id
                        }
                    })
                });

            var jiraServerService = new JiraService(_signalRService, _databaseService, _logger);

            var result = await jiraServerService.AddCommentAndGetItBack(new IntegratedUser() { AccessToken = "token" }, string.Empty, string.Empty);

            Assert.NotNull(result);
            Assert.Equal(id, result.Id);
        }

        [Fact]
        public async Task UpdateComment()
        {
            const string id = "id Name";

            A.CallTo(() =>
                    _signalRService.SendRequestAndWaitForResponse(A<string>._, A<string>._, CancellationToken.None))
                .Returns(new SignalRResponse
                {
                    Received = true,
                    Message = JsonConvert.SerializeObject(new JiraResponse<JiraComment>
                    {
                        ResponseCode = 200,
                        Response = new JiraComment()
                        {
                            Id = id
                        }
                    })
                });

            var jiraServerService = new JiraService(_signalRService, _databaseService, _logger);

            var result = await jiraServerService.UpdateComment(new IntegratedUser() { AccessToken = "token" }, string.Empty, string.Empty, string.Empty);

            Assert.NotNull(result);
            Assert.Equal(id, result.Id);
        }

        [Fact]
        public async Task SearchUsers()
        {
            const string accountId = "Account Id";

            A.CallTo(() =>
                    _signalRService.SendRequestAndWaitForResponse(A<string>._, A<string>._, CancellationToken.None))
                .Returns(new SignalRResponse
                {
                    Received = true,
                    Message = JsonConvert.SerializeObject(new JiraResponse<JiraUser[]>
                    {
                        ResponseCode = 200,
                        Response = new JiraUser[1]
                        {
                            new JiraUser()
                            {
                                AccountId = accountId
                            }
                        }
                    })
                });

            var jiraServerService = new JiraService(_signalRService, _databaseService, _logger);

            var result = await jiraServerService.SearchUsers(new IntegratedUser() { AccessToken = "token" }, string.Empty);

            Assert.NotNull(result);
            Assert.Equal(accountId, result[0].AccountId);
        }

        [Fact]
        public async Task SearchAssignableMultiProject()
        {
            const string accountId = "Account Id";

            A.CallTo(() =>
                    _signalRService.SendRequestAndWaitForResponse(A<string>._, A<string>._, CancellationToken.None))
                .Returns(new SignalRResponse
                {
                    Received = true,
                    Message = JsonConvert.SerializeObject(new JiraResponse<JiraUser[]>
                    {
                        ResponseCode = 200,
                        Response = new JiraUser[1]
                        {
                            new JiraUser()
                            {
                                AccountId = accountId
                            }
                        }
                    })
                });

            var jiraServerService = new JiraService(_signalRService, _databaseService, _logger);

            var result = await jiraServerService.SearchAssignableMultiProject(new IntegratedUser() { AccessToken = "token" }, string.Empty, string.Empty);

            Assert.NotNull(result);
            Assert.Equal(accountId, result[0].AccountId);
        }

        [Fact]
        public async Task SearchAssignable()
        {
            const string accountId = "Account Id";

            A.CallTo(() =>
                    _signalRService.SendRequestAndWaitForResponse(A<string>._, A<string>._, CancellationToken.None))
                .Returns(new SignalRResponse
                {
                    Received = true,
                    Message = JsonConvert.SerializeObject(new JiraResponse<JiraUser[]>
                    {
                        ResponseCode = 200,
                        Response = new JiraUser[1]
                        {
                            new JiraUser()
                            {
                                AccountId = accountId
                            }
                        }
                    })
                });

            var jiraServerService = new JiraService(_signalRService, _databaseService, _logger);

            var result = await jiraServerService.SearchAssignable(new IntegratedUser() { AccessToken = "token" }, string.Empty,  string.Empty, string.Empty);

            Assert.NotNull(result);
            Assert.Equal(accountId, result[0].AccountId);
        }

        [Fact]
        public async Task Assign()
        {
            var userName = "test";

            var user = new JiraUser() { Name = userName };
            dynamic jObject = new JObject();
            jObject.Assignee = (JObject)JToken.FromObject(user);

            string message =
                "{\"requestUrl\":\"api/2/issue/\",\"requestType\":\"GET\",\"requestBody\":\"\",\"token\":\"token\",\"teamsId\":null,\"atlasId\":null}";

            A.CallTo(() =>
                    _signalRService.SendRequestAndWaitForResponse(A<string>._, A<string>._, CancellationToken.None))
                .Returns(new SignalRResponse
                {
                    Received = true,
                    Message = JsonConvert.SerializeObject(new JiraResponse<JiraApiActionCallResponseWithContent<string>>
                    {
                        ResponseCode = 200,
                        Response = new JiraApiActionCallResponseWithContent<string>()
                        {
                            Content = "content"
                        }
                    })
                });

            A.CallTo(() =>
                    _signalRService.SendRequestAndWaitForResponse(A<string>._, message, CancellationToken.None))
                .Returns(new SignalRResponse
                {
                    Received = true,
                    Message = JsonConvert.SerializeObject(new JiraResponse<JiraIssue>
                    {
                        ResponseCode = 200,
                        Response = new JiraIssue()
                        {
                            Id = "ID",
                            FieldsRaw = jObject
                        }
                    })
                });

            var jiraServerService = new JiraService(_signalRService, _databaseService, _logger);

            var result = await jiraServerService.Assign(new IntegratedUser() { AccessToken = "token" }, string.Empty, string.Empty);

            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
            Assert.Equal(userName, result.Content);
        }

        [Fact]
        public async Task Assign_WhenResponseForTheUser_IsFalse()
        {
            A.CallTo(() =>
                    _signalRService.SendRequestAndWaitForResponse(A<string>._, A<string>._, CancellationToken.None))
                .Returns(new SignalRResponse
                {
                    Received = true,
                    Message = JsonConvert.SerializeObject(new JiraResponse<JiraApiActionCallResponseWithContent<string>>
                    {
                        ResponseCode = 300,
                        Response = new JiraApiActionCallResponseWithContent<string>()
                        {
                            Content = "content"
                        }
                    })
                });

            var jiraServerService = new JiraService(_signalRService, _databaseService, _logger);

            var result = await jiraServerService.Assign(new IntegratedUser() { AccessToken = "token" }, string.Empty, string.Empty);

            Assert.NotNull(result);
            Assert.False(result.IsSuccess);
        }

        [Fact]
        public async Task GetWatchers()
        {
            const string accountId = "Account Id";

            A.CallTo(() =>
                    _signalRService.SendRequestAndWaitForResponse(A<string>._, A<string>._, CancellationToken.None))
                .Returns(new SignalRResponse
                {
                    Received = true,
                    Message = JsonConvert.SerializeObject(new JiraResponse<JiraWatcher>
                    {
                        ResponseCode = 200,
                        Response = new JiraWatcher()
                        {
                            Watchers = new List<JiraUser>()
                            {
                                new JiraUser()
                                {
                                    AccountId = accountId
                                }
                            }
                        }
                    })
                });

            var jiraServerService = new JiraService(_signalRService, _databaseService, _logger);

            var result = await jiraServerService.GetWatchers(new IntegratedUser() { AccessToken = "token" }, string.Empty);

            Assert.NotNull(result);
            Assert.Equal(accountId, result.Watchers[0].AccountId);
        }

        [Fact]
        public async Task GetAllBoards()
        {
            const string id = "Board Id";

            A.CallTo(() =>
                    _signalRService.SendRequestAndWaitForResponse(A<string>._, A<string>._, CancellationToken.None))
                .Returns(new SignalRResponse
                {
                    Received = true,
                    Message = JsonConvert.SerializeObject(new JiraResponse<JiraAgileResultFor<JiraIssueBoard>>
                    {
                        ResponseCode = 200,
                        Response = new JiraAgileResultFor<JiraIssueBoard>()
                        {
                            Values = new List<JiraIssueBoard>()
                            {
                                new JiraIssueBoard()
                                {
                                    Id = id
                                }
                            }
                        }
                    })
                });

            var jiraServerService = new JiraService(_signalRService, _databaseService, _logger);

            var result = await jiraServerService.GetAllBoards(new IntegratedUser() { AccessToken = "token" }, string.Empty, string.Empty, string.Empty);

            Assert.NotNull(result);
            Assert.Equal(id, result[0].Id);
        }

        [Fact]
        public async Task GetAllSprints()
        {
            const long id = 1234;

            A.CallTo(() =>
                    _signalRService.SendRequestAndWaitForResponse(A<string>._, A<string>._, CancellationToken.None))
                .Returns(new SignalRResponse
                {
                    Received = true,
                    Message = JsonConvert.SerializeObject(new JiraResponse<JiraPaginatedResponse<JiraIssueSprint>>
                    {
                        ResponseCode = 200,
                        Response = new JiraPaginatedResponse<JiraIssueSprint>()
                        {
                            IsLast = true,
                            Values = new List<JiraIssueSprint>()
                            {
                                new JiraIssueSprint()
                                {
                                    Id = id
                                }
                            }
                        }
                    })
                });

            var jiraServerService = new JiraService(_signalRService, _databaseService, _logger);

            var result = await jiraServerService.GetAllSprints(new IntegratedUser() { AccessToken = "token" }, string.Empty);

            Assert.NotNull(result);
            Assert.Equal(id, result[0].Id);
        }

        [Fact]
        public async Task GetAllEpics()
        {
            const long id = 1234;

            A.CallTo(() =>
                    _signalRService.SendRequestAndWaitForResponse(A<string>._, A<string>._, CancellationToken.None))
                .Returns(new SignalRResponse
                {
                    Received = true,
                    Message = JsonConvert.SerializeObject(new JiraResponse<JiraPaginatedResponse<JiraIssueEpic>>
                    {
                        ResponseCode = 200,
                        Response = new JiraPaginatedResponse<JiraIssueEpic>()
                        {
                            IsLast = true,
                            Values = new List<JiraIssueEpic>()
                            {
                                new JiraIssueEpic()
                                {
                                    Id = id
                                }
                            }
                        }
                    })
                });

            var jiraServerService = new JiraService(_signalRService, _databaseService, _logger);

            var result = await jiraServerService.GetAllEpics(new IntegratedUser() { AccessToken = "token" }, string.Empty);

            Assert.NotNull(result);
            Assert.Equal(id, result[0].Id);
        }

        [Fact]
        public async Task GetAllSprintsForProject()
        {
            const long id = 1234;

            A.CallTo(() =>
                    _signalRService.SendRequestAndWaitForResponse(A<string>._, A<string>._, CancellationToken.None))
                .Returns(new SignalRResponse
                {
                    Received = true,
                    Message = JsonConvert.SerializeObject(new JiraResponse<JiraAgileResultFor<JiraIssueBoard>>
                    {
                        ResponseCode = 200,
                        Response = new JiraAgileResultFor<JiraIssueBoard>()
                        {
                            Values = new List<JiraIssueBoard>()
                            {
                                new JiraIssueBoard()
                                {
                                    Id = "Project id"
                                }
                            }
                        }
                    })
                });

            A.CallTo(() =>
                    _signalRService.SendRequestAndWaitForResponse(A<string>._, A<string>._, CancellationToken.None))
                .Returns(new SignalRResponse
                {
                    Received = true,
                    Message = JsonConvert.SerializeObject(new JiraResponse<JiraPaginatedResponse<JiraIssueSprint>>
                    {
                        ResponseCode = 200,
                        Response = new JiraPaginatedResponse<JiraIssueSprint>()
                        {
                            IsLast = true,
                            Values = new List<JiraIssueSprint>()
                            {
                                new JiraIssueSprint()
                                {
                                    Id = id
                                }
                            }
                        }
                    })
                });

            var jiraServerService = new JiraService(_signalRService, _databaseService, _logger);

            var result = await jiraServerService.GetAllSprintsForProject(new IntegratedUser() { AccessToken = "token" }, string.Empty);

            Assert.NotNull(result);
            Assert.Equal(id, result[0].Id);
        }

        [Fact]
        public async Task GetAllEpicsForProject()
        {
            const long id = 1234;

            A.CallTo(() =>
                    _signalRService.SendRequestAndWaitForResponse(A<string>._, A<string>._, CancellationToken.None))
                .Returns(new SignalRResponse
                {
                    Received = true,
                    Message = JsonConvert.SerializeObject(new JiraResponse<JiraAgileResultFor<JiraIssueBoard>>
                    {
                        ResponseCode = 200,
                        Response = new JiraAgileResultFor<JiraIssueBoard>()
                        {
                            Values = new List<JiraIssueBoard>()
                            {
                                new JiraIssueBoard()
                                {
                                    Id = "Project id"
                                }
                            }
                        }
                    })
                });

            A.CallTo(() =>
                    _signalRService.SendRequestAndWaitForResponse(A<string>._, A<string>._, CancellationToken.None))
                .Returns(new SignalRResponse
                {
                    Received = true,
                    Message = JsonConvert.SerializeObject(new JiraResponse<JiraPaginatedResponse<JiraIssueEpic>>
                    {
                        ResponseCode = 200,
                        Response = new JiraPaginatedResponse<JiraIssueEpic>()
                        {
                            IsLast = true,
                            Values = new List<JiraIssueEpic>()
                            {
                                new JiraIssueEpic()
                                {
                                    Id = id
                                }
                            }
                        }
                    })
                });

            var jiraServerService = new JiraService(_signalRService, _databaseService, _logger);

            var result = await jiraServerService.GetAllEpicsForProject(new IntegratedUser() { AccessToken = "token" }, string.Empty);

            Assert.NotNull(result);
            Assert.Equal(id, result[0].Id);
        }

        [Fact]
        public async Task GetJiraCapabilities()
        {
            const string listIssueTypeFields = "ListIssueTypeFields";

            A.CallTo(() =>
                    _signalRService.SendRequestAndWaitForResponse(A<string>._, A<string>._, CancellationToken.None))
                .Returns(new SignalRResponse
                {
                    Received = true,
                    Message = JsonConvert.SerializeObject(new JiraResponse<JiraCapabilitiesResponse>
                    {
                        ResponseCode = 200,
                        Response = new JiraCapabilitiesResponse()
                        {
                            Capabilities = new JiraCapabilities()
                            {
                                ListIssueTypeFields = listIssueTypeFields
                            }
                        }
                    })
                });

            var jiraServerService = new JiraService(_signalRService, _databaseService, _logger);

            var result = await jiraServerService.GetJiraCapabilities(new IntegratedUser() { AccessToken = "token" });

            Assert.NotNull(result);
            Assert.Equal(listIssueTypeFields, result.ListIssueTypeFields);
        }

        [Fact]
        public async Task Search_WhenJiraIssuesNull()
        {
            A.CallTo(() =>
                    _signalRService.SendRequestAndWaitForResponse(A<string>._, A<string>._, CancellationToken.None))
                .Returns(new SignalRResponse
                {
                    Received = true,
                    Message = JsonConvert.SerializeObject(new JiraResponse<JiraIssueSearch>
                    {
                        ResponseCode = 200,
                        Response = new JiraIssueSearch() { MaxResults = 50 }
                    })
                });

            var jiraServerService = new JiraService(_signalRService, _databaseService, _logger);

            var result = await jiraServerService.Search(new IntegratedUser() { AccessToken = "token" }, new SearchForIssuesRequest());

            Assert.NotNull(result);
            Assert.Equal(50, result.MaxResults);
        }

        [Fact]
        public async Task Search_WhenJiraIssuesNotNull()
        {
            const string id = "Project Id";

            A.CallTo(() =>
                    _signalRService.SendRequestAndWaitForResponse(A<string>._, A<string>._, CancellationToken.None))
                .Returns(new SignalRResponse
                {
                    Received = true,
                    Message = JsonConvert.SerializeObject(new JiraResponse<List<JiraIssuePriority>>
                    {
                        ResponseCode = 200,
                        Response = new List<JiraIssuePriority>()
                        {
                            new JiraIssuePriority()
                            {
                                Id = id
                            }
                        }
                    })
                });

            A.CallTo(() =>
                    _signalRService.SendRequestAndWaitForResponse(A<string>._, A<string>._, CancellationToken.None))
                .Returns(new SignalRResponse
                {
                    Received = true,
                    Message = JsonConvert.SerializeObject(new JiraResponse<JiraIssueVotes>
                    {
                        ResponseCode = 200,
                        Response = new JiraIssueVotes()
                    })
                });

            A.CallTo(() =>
                    _signalRService.SendRequestAndWaitForResponse(A<string>._, A<string>._, CancellationToken.None))
                .Returns(new SignalRResponse
                {
                    Received = true,
                    Message = JsonConvert.SerializeObject(new JiraResponse<JiraIssueSearch>
                    {
                        ResponseCode = 200,
                        Response = new JiraIssueSearch()
                        {
                            MaxResults = 50,
                            JiraIssues = new JiraIssue[]
                            {
                                new JiraIssue()
                                {
                                    Id = "Jira Id"
                                }
                            }
                        }
                    })
                });

            var jiraServerService = new JiraService(_signalRService, _databaseService, _logger);

            var result = await jiraServerService.Search(new IntegratedUser() { AccessToken = "token" }, new SearchForIssuesRequest());

            Assert.NotNull(result);
            Assert.Equal(50, result.MaxResults);
        }

        [Fact]
        public async Task Vote()
        {
            const string testMessage = "Test Message";

            A.CallTo(() =>
                    _signalRService.SendRequestAndWaitForResponse(A<string>._, A<string>._, CancellationToken.None))
                .Returns(new SignalRResponse
                {
                    Received = true,
                    Message = JsonConvert.SerializeObject(new JiraResponse<string>
                    {
                        ResponseCode = 200,
                        Message = testMessage
                    })
                });
            var jiraServerService = new JiraService(_signalRService, _databaseService, _logger);

            var result = await jiraServerService.Vote(new IntegratedUser() { AccessToken = "token" }, string.Empty);

            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
            Assert.Equal(testMessage, result.ErrorMessage);
        }

        [Fact]
        public async Task AddComment()
        {
            const string testMessage = "Test Message";

            A.CallTo(() =>
                    _signalRService.SendRequestAndWaitForResponse(A<string>._, A<string>._, CancellationToken.None))
                .Returns(new SignalRResponse
                {
                    Received = true,
                    Message = JsonConvert.SerializeObject(new JiraResponse<CommentIssueRequest>
                    {
                        ResponseCode = 200,
                        Message = testMessage
                    })
                });
            var jiraServerService = new JiraService(_signalRService, _databaseService, _logger);

            var result = await jiraServerService.AddComment(new IntegratedUser() { AccessToken = "token" }, string.Empty, string.Empty);

            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
            Assert.Equal(testMessage, result.ErrorMessage);
        }

        [Fact]
        public async Task UpdateSummary()
        {
            const string testMessage = "Test Message";

            A.CallTo(() =>
                    _signalRService.SendRequestAndWaitForResponse(A<string>._, A<string>._, CancellationToken.None))
                .Returns(new SignalRResponse
                {
                    Received = true,
                    Message = JsonConvert.SerializeObject(new JiraResponse<JiraApiActionCallResponse>
                    {
                        ResponseCode = 200,
                        Message = testMessage
                    })
                });
            var jiraServerService = new JiraService(_signalRService, _databaseService, _logger);

            var result = await jiraServerService.UpdateSummary(new IntegratedUser() { AccessToken = "token" }, string.Empty, string.Empty);

            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
            Assert.Equal(testMessage, result.ErrorMessage);
        }

        [Fact]
        public async Task UpdateDescription()
        {
            const string testMessage = "Test Message";

            A.CallTo(() =>
                    _signalRService.SendRequestAndWaitForResponse(A<string>._, A<string>._, CancellationToken.None))
                .Returns(new SignalRResponse
                {
                    Received = true,
                    Message = JsonConvert.SerializeObject(new JiraResponse<string>
                    {
                        ResponseCode = 200,
                        Message = testMessage
                    })
                });
            var jiraServerService = new JiraService(_signalRService, _databaseService, _logger);

            var result = await jiraServerService.UpdateDescription(new IntegratedUser() { AccessToken = "token" }, string.Empty, string.Empty);

            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
            Assert.Equal(testMessage, result.ErrorMessage);
        }

        [Fact]
        public async Task DoTransition()
        {
            const string testMessage = "Test Message";

            A.CallTo(() =>
                    _signalRService.SendRequestAndWaitForResponse(A<string>._, A<string>._, CancellationToken.None))
                .Returns(new SignalRResponse
                {
                    Received = true,
                    Message = JsonConvert.SerializeObject(new JiraResponse<string>
                    {
                        ResponseCode = 200,
                        Message = testMessage
                    })
                });
            var jiraServerService = new JiraService(_signalRService, _databaseService, _logger);

            var result = await jiraServerService.DoTransition(new IntegratedUser() { AccessToken = "token" }, string.Empty, new DoTransitionRequest());

            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
            Assert.Equal(testMessage, result.ErrorMessage);
        }

        [Fact]
        public async Task UpdatePriority()
        {
            const string testMessage = "Test Message";

            A.CallTo(() =>
                    _signalRService.SendRequestAndWaitForResponse(A<string>._, A<string>._, CancellationToken.None))
                .Returns(new SignalRResponse
                {
                    Received = true,
                    Message = JsonConvert.SerializeObject(new JiraResponse<string>
                    {
                        ResponseCode = 200,
                        Message = testMessage
                    })
                });
            var jiraServerService = new JiraService(_signalRService, _databaseService, _logger);

            var result = await jiraServerService.UpdatePriority(new IntegratedUser() { AccessToken = "token" }, string.Empty, string.Empty);

            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
            Assert.Equal(testMessage, result.ErrorMessage);
        }

        [Fact]
        public async Task Watch()
        {
            const string testMessage = "Test Message";

            A.CallTo(() =>
                    _signalRService.SendRequestAndWaitForResponse(A<string>._, A<string>._, CancellationToken.None))
                .Returns(new SignalRResponse
                {
                    Received = true,
                    Message = JsonConvert.SerializeObject(new JiraResponse<string>
                    {
                        ResponseCode = 200,
                        Message = testMessage
                    })
                });
            var jiraServerService = new JiraService(_signalRService, _databaseService, _logger);

            var result = await jiraServerService.Watch(new IntegratedUser() { AccessToken = "token" }, string.Empty);

            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
            Assert.Equal(testMessage, result.ErrorMessage);
        }

        [Fact]
        public async Task Unwatch()
        {
            const string testMessage = "Test Message";

            A.CallTo(() =>
                    _signalRService.SendRequestAndWaitForResponse(A<string>._, A<string>._, CancellationToken.None))
                .Returns(new SignalRResponse
                {
                    Received = true,
                    Message = JsonConvert.SerializeObject(new JiraResponse<string>
                    {
                        ResponseCode = 200,
                        Message = testMessage
                    })
                });
            var jiraServerService = new JiraService(_signalRService, _databaseService, _logger);

            var result = await jiraServerService.Unwatch(new IntegratedUser() { AccessToken = "token" }, string.Empty);

            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
            Assert.Equal(testMessage, result.ErrorMessage);
        }

        [Fact]
        public async Task Unvote()
        {
            const string testMessage = "Test Message";

            A.CallTo(() =>
                    _signalRService.SendRequestAndWaitForResponse(A<string>._, A<string>._, CancellationToken.None))
                .Returns(new SignalRResponse
                {
                    Received = true,
                    Message = JsonConvert.SerializeObject(new JiraResponse<string>
                    {
                        ResponseCode = 200,
                        Message = testMessage
                    })
                });
            var jiraServerService = new JiraService(_signalRService, _databaseService, _logger);

            var result = await jiraServerService.Unvote(new IntegratedUser() { AccessToken = "token" }, string.Empty);

            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
            Assert.Equal(testMessage, result.ErrorMessage);
        }

        [Fact]
        public async Task AddIssueWorklog()
        {
            const string testMessage = "Test Message";

            A.CallTo(() =>
                    _signalRService.SendRequestAndWaitForResponse(A<string>._, A<string>._, CancellationToken.None))
                .Returns(new SignalRResponse
                {
                    Received = true,
                    Message = JsonConvert.SerializeObject(new JiraResponse<WorkLogRequest>
                    {
                        ResponseCode = 200,
                        Message = testMessage
                    })
                });

            var jiraServerService = new JiraService(_signalRService, _databaseService, _logger);

            var result = await jiraServerService.AddIssueWorklog(new IntegratedUser() { AccessToken = "token" }, string.Empty, string.Empty);

            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
            Assert.Equal(testMessage, result.ErrorMessage);
        }

        [Theory]
        [InlineData(null, null, null, "Test User", null, 300, "Test Message")]
        [InlineData(null, null, null, "Test User", null, 200, null)]
        [InlineData(null, null, "Description", null, null, 300, "Test Message")]
        [InlineData(null, null, "Description", null, null, 200, null)]
        [InlineData(null, null, null, "Asignee Name", null, 300, "Test Message")]
        [InlineData(null, null, null, "Assignee Name", null, 200, null)]
        [InlineData(null, null, null, null, "Status Id", 300, "Test Message")]
        [InlineData(null, null, null, null, "Status Id", 200, null)]
        [InlineData("Summary", null, null, null, null, 300, "Test Message")]
        [InlineData("Summary", null, null, null, null, 200, null)]
        [InlineData(null, "Priority Id", null, null, null, 300, "Test Message")]
        [InlineData(null, "Priority Id", null, null, null, 200, null)]
        public async Task UpdateIssue(string summary, string priorityId, string description, string assigneeName, string statusId, int responseCode, string testMessage)
        {
            var updateJiraIssueRequest = new JiraIssueFields()
            {
                Summary = summary,
                Description = description,
                Assignee = new JiraUser() { Name = assigneeName },
                Priority = new JiraIssuePriority() { Id = priorityId },
                Status = new JiraIssueStatus() { Id = statusId }
            };
            A.CallTo(() =>
                    _signalRService.SendRequestAndWaitForResponse(A<string>._, A<string>._, CancellationToken.None))
                .Returns(new SignalRResponse
                {
                    Received = true,
                    Message = JsonConvert.SerializeObject(new JiraResponse<string>
                    {
                        ResponseCode = responseCode,
                        Message = testMessage
                    })
                });

            var jiraServerService = new JiraService(_signalRService, _databaseService, _logger);

            var result = await jiraServerService.UpdateIssue(new IntegratedUser() { AccessToken = "token" }, string.Empty, updateJiraIssueRequest);

            Assert.NotNull(result);
            Assert.Equal(testMessage, result.ErrorMessage);
        }

        [Fact]
        public async Task GetCreateMetaIssueTypes()
        {
            string description = "Description";

            A.CallTo(() =>
                    _signalRService.SendRequestAndWaitForResponse(A<string>._, A<string>._, CancellationToken.None))
                .Returns(new SignalRResponse
                {
                    Received = true,
                    Message = JsonConvert.SerializeObject(new JiraResponse<JiraPaginatedResponse<JiraIssueTypeMeta>>
                    {
                        ResponseCode = 200,
                        Response = new JiraPaginatedResponse<JiraIssueTypeMeta>()
                        {
                            IsLast = true,
                            Values = new List<JiraIssueTypeMeta>()
                            {
                                new JiraIssueTypeMeta()
                                {
                                    Id = "id",
                                    Description = description,
                                    Fields = new ExpandoObject()
                                }
                            }
                        }
                    })
                });

            var jiraServerService = new JiraService(_signalRService, _databaseService, _logger);

            var result = await jiraServerService.GetCreateMetaIssueTypes(new IntegratedUser() { AccessToken = "token" }, string.Empty);

            Assert.NotNull(result);
            Assert.IsType<JiraIssueTypeMeta>(result[0]);
            Assert.Equal(description, result[0].Description);
        }

        [Fact]
        public async Task GetCreateMetaIssueTypeFields()
        {
            dynamic exapndoObject = new ExpandoObject();
            exapndoObject.fieldId = "FieldId";

            A.CallTo(() =>
                    _signalRService.SendRequestAndWaitForResponse(A<string>._, A<string>._, CancellationToken.None))
                .Returns(new SignalRResponse
                {
                    Received = true,
                    Message = JsonConvert.SerializeObject(new JiraResponse<JiraPaginatedResponse<ExpandoObject>>
                    {
                        ResponseCode = 200,
                        Response = new JiraPaginatedResponse<ExpandoObject>()
                        {
                            IsLast = true,
                            Values = new List<ExpandoObject>()
                            {
                                exapndoObject
                            }
                        }
                    })
                });

            var jiraServerService = new JiraService(_signalRService, _databaseService, _logger);

            var result = await jiraServerService.GetCreateMetaIssueTypeFields(new IntegratedUser() { AccessToken = "token" }, string.Empty, string.Empty, string.Empty);

            Assert.IsType<ExpandoObject>(result);
        }

        [Fact]
        public async Task GetJiraTenantInfo_ThrowsNotSupportedException()
        {
            var jiraServerService = new JiraService(_signalRService, _databaseService, _logger);

            await Assert.ThrowsAsync<NotSupportedException>(
                () => jiraServerService.GetJiraTenantInfo(new IntegratedUser()));
        }
    }
}
