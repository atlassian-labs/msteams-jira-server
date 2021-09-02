using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MicrosoftTeamsIntegration.Jira.ContractResolvers;
using MicrosoftTeamsIntegration.Jira.Helpers;
using MicrosoftTeamsIntegration.Jira.Models;
using MicrosoftTeamsIntegration.Jira.Models.Jira;
using MicrosoftTeamsIntegration.Jira.Models.Jira.Issue;
using MicrosoftTeamsIntegration.Jira.Models.Jira.Meta;
using MicrosoftTeamsIntegration.Jira.Services.Interfaces;
using MicrosoftTeamsIntegration.Jira.Services.SignalR.Interfaces;
using Newtonsoft.Json;

namespace MicrosoftTeamsIntegration.Jira.Services
{
    // TODO: reorder methods according to interface declaration
    public class JiraService : IJiraService
    {
        private const int MaxResultsLimitDefault = 50;
        private readonly ISignalRService _signalRService;
        private readonly IDatabaseService _databaseService;
        private readonly JsonSerializerSettings _jiraSerializerSettings;
        private readonly ILogger<JiraService> _logger;

        public JiraService(
            ISignalRService signalRService,
            IDatabaseService databaseService,
            ILogger<JiraService> logger)
        {
            _signalRService = signalRService;
            _databaseService = databaseService;
            _logger = logger;
            _jiraSerializerSettings = new JsonSerializerSettings
            {
                ContractResolver = new JiraContractResolver()
            };
        }

        public async Task<JiraIssueSearch> Search(IntegratedUser user, SearchForIssuesRequest request)
        {
            if (request.MaxResults == null)
            {
                request.MaxResults = MaxResultsLimitDefault;
            }

            if (request.Fields == null)
            {
                request.Fields = new List<string>();
            }

            request.Fields.AddRange(GetSearchFieldsFilter());

            var response = await ProcessRequest<JiraIssueSearch>(user, "api/2/search", "POST", request);

            if (response?.JiraIssues is null || !response.JiraIssues.Any())
            {
                return response;
            }

            if (request.Fields == null || request.Fields.Contains("priority"))
            {
                var idsInOrder = await GetPriorities(user);
                response.PrioritiesIdsInOrder = idsInOrder?.Select(issue => issue.Id).Reverse().ToArray();
            }

            foreach (var jiraIssue in response.JiraIssues)
            {
                if (jiraIssue.Fields?.Votes == null || !jiraIssue.Fields.Votes.HasVoted)
                {
                    continue;
                }

                jiraIssue.Fields.Votes = await GetVotes(user, jiraIssue.Key);
            }

            return response;
        }

        public Task<JiraIssue> CreateIssue(IntegratedUser user, CreateJiraIssueRequest createJiraIssueRequest)
        {
            return ProcessRequest<JiraIssue>(user, "api/2/issue", "POST", createJiraIssueRequest);
        }

        public async Task<JiraApiActionCallResponse> UpdateIssue(IntegratedUser user, string issueIdOrKey, JiraIssueFields updateJiraIssueRequest)
        {
            dynamic updateSet = new ExpandoObject();

            bool makeUpdateRequest = false;
            if (updateJiraIssueRequest.Summary != null)
            {
                makeUpdateRequest = true;
                updateSet.summary = new[]
                {
                    new
                    {
                        set = updateJiraIssueRequest.Summary
                    }
                };
            }

            if (updateJiraIssueRequest.Description != null)
            {
                makeUpdateRequest = true;
                updateSet.description = new[]
                {
                    new
                    {
                        set = updateJiraIssueRequest.Description
                    }
                };
            }

            if (updateJiraIssueRequest.Priority?.Id != null)
            {
                makeUpdateRequest = true;
                updateSet.priority = new[]
                {
                    new
                    {
                        set = new
                        {
                            id = updateJiraIssueRequest.Priority.Id
                        }
                    }
                };
            }

            if (makeUpdateRequest)
            {
                var request = new EditIssueRequest { Update = updateSet };
                var updateIssueResponse = await ProcessRequestWithJiraApiActionCallResponse<string>(user, $"api/2/issue/{issueIdOrKey}", "PUT", request);

                if (!updateIssueResponse.IsSuccess)
                {
                    return updateIssueResponse;
                }
            }

            if (updateJiraIssueRequest.Assignee != null)
            {
                var assignRequest = new AssignIssueRequest { Name = updateJiraIssueRequest.Assignee.Name };
                var assignResponse = await ProcessRequestWithJiraApiActionCallResponse<string>(user, $"api/2/issue/{issueIdOrKey}/assignee", "PUT", assignRequest);
                if (!assignResponse.IsSuccess)
                {
                    return assignResponse;
                }
            }

            if (updateJiraIssueRequest.Status?.Id != null)
            {
                var doTransitionRequest = new DoTransitionRequest
                {
                    Transition = new DoTransitionRequestModel
                    {
                        Id = updateJiraIssueRequest.Status.Id
                    }
                };

                var doTransitionResponse = await ProcessRequestWithJiraApiActionCallResponse<string>(user, $"api/2/issue/{issueIdOrKey}/transitions", "POST", doTransitionRequest);
                if (!doTransitionResponse.IsSuccess)
                {
                    return doTransitionResponse;
                }
            }

            return new JiraApiActionCallResponse { IsSuccess = true };
        }

        public Task<List<JiraProject>> GetProjects(IntegratedUser user, bool getAvatars)
        {
            var projectsRequest = new
            {
                getAvatars
            };
            return ProcessRequest<List<JiraProject>>(user, "api/2/project", "GET", projectsRequest);
        }

        public Task<JiraProject> GetProject(IntegratedUser user, string projectKey)
        {
            return ProcessRequest<JiraProject>(user, $"api/2/project/{projectKey}", "GET");
        }

        public Task<List<JiraIssuePriority>> GetPriorities(IntegratedUser user)
        {
            return ProcessRequest<List<JiraIssuePriority>>(user, "api/2/priority", "GET");
        }

        public Task<JiraApiActionCallResponse> UpdatePriority(IntegratedUser user, string issueIdOrKey, string priorityId)
        {
            var priorityUpdateSet = new
            {
                priority = new[]
                {
                    new
                    {
                        set = new
                        {
                            id = priorityId
                        }
                    }
                }
            };
            var request = new EditIssueRequest { Update = priorityUpdateSet };
            return ProcessRequestWithJiraApiActionCallResponse<string>(user, $"api/2/issue/{issueIdOrKey}", "PUT", request);
        }

        public Task<List<JiraIssueStatus>> GetStatuses(IntegratedUser user)
        {
            return ProcessRequest<List<JiraIssueStatus>>(user, "api/2/status", "GET");
        }

        public async Task<List<JiraIssueStatus>> GetStatusesByProject(IntegratedUser user, string projectIdOrKey)
        {
            var response = await ProcessRequest<List<JiraStatusesResponse>>(user, $"api/2/project/{projectIdOrKey}/statuses", "GET");
            var statuses = new List<JiraIssueStatus>();

            foreach (var statusesResponse in response)
            {
                var newIssueStatuses = statusesResponse.Statuses.Where(x => statuses.All(y => y.Id != x.Id));
                statuses.AddRange(newIssueStatuses);
            }

            return statuses;
        }

        public Task<MyselfInfo> GetDataAboutMyself(IntegratedUser user)
        {
            return ProcessRequest<MyselfInfo>(user, "api/2/myself", "GET");
        }

        public Task<JiraTenantInfo> GetJiraTenantInfo(IntegratedUser user)
        {
            // Jira Server does not have the concept of tenant info (i.e., cloud ID)
            throw new NotSupportedException();
        }

        public Task<List<JiraIssueFilter>> GetFilters(IntegratedUser user)
        {
            return ProcessRequest<List<JiraIssueFilter>>(user, "api/2/filter", "GET");
        }

        public Task<List<JiraIssueFilter>> SearchFilters(IntegratedUser user, string filterName)
        {
            var searchFilterRequest = new
            {
                filterName
            };

            return ProcessRequest<List<JiraIssueFilter>>(user, "api/2/filter", "GET", searchFilterRequest);
        }

        public Task<JiraIssueFilter> GetFilter(IntegratedUser user, string filterId)
        {
            return ProcessRequest<JiraIssueFilter>(user, $"api/2/filter/{filterId}", "GET");
        }

        public Task<List<JiraIssueFilter>> GetFavouriteFilters(IntegratedUser user)
        {
            return ProcessRequest<List<JiraIssueFilter>>(user, "api/2/filter/favourite", "GET");
        }

        public Task<JiraPermissionsResponse> GetMyPermissions(IntegratedUser user, string permissions, string issueId, string projectKey)
        {
            return ProcessRequest<JiraPermissionsResponse>(user, $"api/2/mypermissions?projectKey={projectKey}&issueId={issueId}", "GET");
        }

        public Task<JiraIssue> GetIssueByIdOrKey(IntegratedUser user, string issueIdOrKey)
        {
            return ProcessRequest<JiraIssue>(user, $"api/2/issue/{issueIdOrKey}", "GET");
        }

        public Task<JiraApiActionCallResponse> UpdateSummary(IntegratedUser user, string issueIdOrKey, string summary)
        {
            var summaryUpdateSet = new
            {
                summary = new[]
                {
                    new
                    {
                        set = summary
                    }
                }
            };

            var request = new EditIssueRequest { Update = summaryUpdateSet };
            return ProcessRequestWithJiraApiActionCallResponse<string>(user, $"api/2/issue/{issueIdOrKey}", "PUT", request);
        }

        public Task<JiraApiActionCallResponse> UpdateDescription(IntegratedUser user, string issueIdOrKey, string description)
        {
            var descriptionUpdateSet = new
            {
                description = new[]
                {
                    new
                    {
                        set = description
                    }
                }
            };

            var request = new EditIssueRequest { Update = descriptionUpdateSet };
            return ProcessRequestWithJiraApiActionCallResponse<string>(user, $"api/2/issue/{issueIdOrKey}", "PUT", request);
        }

        public Task<JiraTransitionsResponse> GetTransitions(IntegratedUser user, string issueIdOrKey)
        {
            return ProcessRequest<JiraTransitionsResponse>(user, $"api/2/issue/{issueIdOrKey}/transitions", "GET");
        }

        public Task<JiraApiActionCallResponse> DoTransition(IntegratedUser user, string issueIdOrKey, DoTransitionRequest doTransitionRequest)
        {
            return ProcessRequestWithJiraApiActionCallResponse<string>(user, $"api/2/issue/{issueIdOrKey}/transitions", "POST", doTransitionRequest);
        }

        public Task<JiraComment> AddCommentAndGetItBack(IntegratedUser user, string issueIdOrKey, string comment)
        {
            return ProcessRequest<JiraComment>(user, $"api/2/issue/{issueIdOrKey}/comment", "POST", new CommentIssueRequest(comment));
        }

        public Task<JiraComment> UpdateComment(IntegratedUser user, string issueIdOrKey, string commentId, string comment)
        {
            return ProcessRequest<JiraComment>(user, $"api/2/issue/{issueIdOrKey}/comment/{commentId}", "PUT", new CommentIssueRequest(comment));
        }

        public Task<JiraUser[]> SearchUsers(IntegratedUser user, string username)
        {
            return ProcessRequest<JiraUser[]>(
                user,
                $"api/2/user/search?username={username}&maxResults=50&startAt=0",
                "GET");
        }

        public Task<JiraUser[]> SearchAssignableMultiProject(IntegratedUser user, string projectKey, string username)
        {
            return ProcessRequest<JiraUser[]>(
                user,
                $"api/2/user/assignable/multiProjectSearch?username={username}&projectKeys={projectKey}&maxResults=50&startAt=0",
                "GET");
        }

        public Task<JiraUser[]> SearchAssignable(IntegratedUser user, string issueIdOrKey, string projectKey, string username)
        {
            return ProcessRequest<JiraUser[]>(
                user,
                $"api/2/user/assignable/search?username={username}&project={projectKey}&issueKey={issueIdOrKey}&maxResults=50&startAt=0",
                "GET");
        }

        public async Task<JiraApiActionCallResponseWithContent<string>> Assign(IntegratedUser user, string issueIdOrKey, string assigneeName)
        {
            var requestBody = new AssignIssueRequest { Name = assigneeName };
            var response = await SendRequestAndGetRawResponse(user, $"api/2/issue/{issueIdOrKey}/assignee", "PUT", requestBody);

            var responseObj = new JsonDeserializer(_logger).Deserialize<JiraResponse<JiraApiActionCallResponse>>(response);
            if (JiraHelpers.IsResponseForTheUser(responseObj))
            {
                var issueResponse = await GetIssueByIdOrKey(user, issueIdOrKey);
                return new JiraApiActionCallResponseWithContent<string>
                {
                    IsSuccess = true,
                    Content = issueResponse.Fields.Assignee?.Name
                };
            }

            return new JiraApiActionCallResponseWithContent<string>
            {
                IsSuccess = false,
                Content = null,
                ErrorMessage = responseObj.Message
            };
        }

        public Task<JiraWatcher> GetWatchers(IntegratedUser user, string issueIdOrKey)
        {
            return ProcessRequest<JiraWatcher>(
                user,
                $"api/2/issue/{issueIdOrKey}/watchers",
                "GET");
        }

        public async Task<JiraApiActionCallResponse> Watch(IntegratedUser user, string issueIdOrKey)
        {
            var userName = await GetUserNameOrAccountId(user);
            return await ProcessRequestWithJiraApiActionCallResponse<string>(user, $"api/2/issue/{issueIdOrKey}/watchers", "POST", userName);
        }

        public async Task<JiraApiActionCallResponse> Unwatch(IntegratedUser user, string issueIdOrKey)
        {
            var userName = await GetUserNameOrAccountId(user);
            return await ProcessRequestWithJiraApiActionCallResponse<string>(user, $"api/2/issue/{issueIdOrKey}/watchers?username={userName}", "DELETE");
        }

        public Task<JiraApiActionCallResponse> Vote(IntegratedUser user, string issueIdOrKey)
        {
            return ProcessRequestWithJiraApiActionCallResponse<string>(user, $"api/2/issue/{issueIdOrKey}/votes", "POST");
        }

        public Task<JiraApiActionCallResponse> Unvote(IntegratedUser user, string issueIdOrKey)
        {
            return ProcessRequestWithJiraApiActionCallResponse<string>(user, $"api/2/issue/{issueIdOrKey}/votes", "DELETE");
        }

        public Task<JiraApiActionCallResponse> AddIssueWorklog(IntegratedUser user, string issueIdOrKey, string timeSpent)
        {
            var request = new WorkLogRequest { TimeSpent = timeSpent };
            return ProcessRequestWithJiraApiActionCallResponse<WorkLogRequest>(user, $"api/2/issue/{issueIdOrKey}/worklog", "POST", request);
        }

        public Task<List<JiraIssueType>> GetTypes(IntegratedUser user)
        {
            return ProcessRequest<List<JiraIssueType>>(user, "api/2/issuetype", "GET");
        }

        public Task<JiraIssueEditMeta> GetIssueEditMeta(IntegratedUser user, string issueIdOrKey)
        {
            return ProcessRequest<JiraIssueEditMeta>(user, $"api/2/issue/{issueIdOrKey}/editmeta", "GET");
        }

        public Task<JiraApiActionCallResponse> AddComment(IntegratedUser user, string issueKey, string comment)
        {
            var request = new CommentIssueRequest { Comment = comment };
            return ProcessRequestWithJiraApiActionCallResponse<CommentIssueRequest>(user, $"api/2/issue/{issueKey}/comment", "POST", request);
        }

        public async Task<string> GetUserNameOrAccountId(IntegratedUser user)
        {
            try
            {
                var userInfo = await GetDataAboutMyself(user);
                return userInfo?.Name;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<List<JiraIssueAutocompleteData>> GetFieldAutocompleteData(IntegratedUser user, string fieldName)
        {
            var result = await ProcessRequest<JiraIssueAutocompleteResult>(
                user,
                $"api/2/jql/autocompletedata/suggestions?fieldName={fieldName}",
                "GET");
            return result.Results;
        }

        public async Task<List<JiraIssueBoard>> GetAllBoards(IntegratedUser user, string projectKeyOrId, string type, string name)
        {
            var result = await ProcessRequest<JiraAgileResultFor<JiraIssueBoard>>(
                user,
                $"agile/1.0/board?projectKeyOrId={projectKeyOrId}&type={type}&name={name}",
                "GET");
            return result.Values;
        }

        public async Task<List<JiraIssueSprint>> GetAllSprints(IntegratedUser user, string boardId)
        {
            var result = await ProcessRequest<JiraAgileResultFor<JiraIssueSprint>>(
                user,
                $"agile/1.0/board/{boardId}/sprint",
                "GET");
            return result.Values;
        }

        public async Task<List<JiraIssueEpic>> GetAllEpics(IntegratedUser user, string boardId)
        {
            var result = await ProcessRequest<JiraAgileResultFor<JiraIssueEpic>>(
                user,
                $"agile/1.0/board/{boardId}/epic",
                "GET");
            return result.Values;
        }

        public async Task<List<JiraIssueSprint>> GetAllSprintsForProject(IntegratedUser user, string projectKeyOrId)
        {
            var boards = await GetAllBoards(user, projectKeyOrId, null, null);
            var sprints = new List<JiraIssueSprint>();

            foreach (var board in boards)
            {
                try
                {
                    sprints.AddRange(await GetAllSprints(user, board.Id));
                }
                catch (Exception)
                {
                }
            }

            return sprints;
        }

        public async Task<List<JiraIssueEpic>> GetAllEpicsForProject(IntegratedUser user, string projectKeyOrId)
        {
            var boards = await GetAllBoards(user, projectKeyOrId, null, null);
            var epics = new List<JiraIssueEpic>();

            foreach (var board in boards)
            {
                try
                {
                    epics.AddRange(await GetAllEpics(user, board.Id));
                }
                catch (Exception)
                {
                }
            }

            return epics;
        }

        public async Task<List<JiraIssueTypeMeta>> GetCreateMetaIssueTypes(IntegratedUser user, string projectKeyOrId)
        {
            var endpointUrl = "api/2/issue/createmeta";
            var canUseNewCreateMetaEndpoints = await CanUseNewCreateMetaEndpoints(user);

            if (!canUseNewCreateMetaEndpoints)
            {
                var requestUrl = $"{endpointUrl}?projectKeys={projectKeyOrId}&expand=projects.issuetypes";
                var response = await ProcessRequest<JiraIssueCreateMeta>(user, requestUrl, "GET");
                var project = response?.Projects.FirstOrDefault();

                return project?.IssueTypes;
            }

            return await ProcessPaginatedRequestRecursively<JiraIssueTypeMeta>(user, $"{endpointUrl}/{projectKeyOrId}/issuetypes", "GET");
        }

        public async Task<ExpandoObject> GetCreateMetaIssueTypeFields(IntegratedUser user, string projectKeyOrId, string issueTypeId, string issueTypeName)
        {
            var endpointUrl = "api/2/issue/createmeta";
            var canUseNewCreateMetaEndpoints = await CanUseNewCreateMetaEndpoints(user);

            if (!canUseNewCreateMetaEndpoints)
            {
                var requestUrl = $"{endpointUrl}?projectKeys={projectKeyOrId}&issuetypeNames={issueTypeName}&expand=projects.issuetypes.fields";
                var response = await ProcessRequest<JiraIssueCreateMeta>(user, requestUrl, "GET");
                var project = response?.Projects.FirstOrDefault();
                var issuetype = project?.IssueTypes.FirstOrDefault();

                return issuetype?.Fields;
            }

            var result = await ProcessPaginatedRequestRecursively<ExpandoObject>(user, $"{endpointUrl}/{projectKeyOrId}/issuetypes/{issueTypeId}", "GET");

            // build dynamic object from list of dynamic objects to follow the model returned from legacy API endpoints
            dynamic fields = new ExpandoObject();
            var fieldsDict = fields as IDictionary<string, object>;
            foreach (var res in result)
            {
                if (((IDictionary<string, object>)res).ContainsKey("fieldId"))
                {
                    fieldsDict[((dynamic)res).fieldId] = res;
                }
            }

            return fields;
        }

        public async Task<JiraCapabilities> GetJiraCapabilities(IntegratedUser user)
        {
            var result = await ProcessRequest<JiraCapabilitiesResponse>(user, "capabilities", "GET");
            return result?.Capabilities;
        }

        public Task<List<JiraProject>> GetProjectsByName(IntegratedUser user, bool getAvatars, string filterName)
        {
            return GetProjects(user, true);
        }

        private Task<JiraIssueVotes> GetVotes(IntegratedUser user, string issueIdOrKey)
        {
            return ProcessRequest<JiraIssueVotes>(user, $"api/2/issue/{issueIdOrKey}/votes", "GET");
        }

        private async Task<T> ProcessRequest<T>(IntegratedUser user, string requestUrl, string requestType, object requestBody = null)
        {
            var response = await SendRequestAndGetRawResponse(user, requestUrl, requestType, requestBody);
            var responseObj = new JsonDeserializer(_logger).Deserialize<JiraResponse<T>>(response);

            if (responseObj == null)
            {
                _logger.LogDebug($"Jira server response returned incorrect object. Expected object: {typeof(T)}. Response: {response}");
                return default;
            }

            if (JiraHelpers.IsResponseForTheUser(responseObj))
            {
                return responseObj.Response;
            }

            await JiraHelpers.HandleJiraServerError(_databaseService, _logger, responseObj.ResponseCode, responseObj.Message, user.MsTeamsUserId, user.JiraServerId);
            return default;
        }

        private async Task<List<T>> ProcessPaginatedRequestRecursively<T>(IntegratedUser user, string requestUrl, string requestType, int startAt = 0, int maxResults = 50)
        {
            var url = $"{requestUrl}?startAt={startAt}&maxResults={maxResults}";
            var response = await ProcessRequest<JiraPaginatedResponse<T>>(user, url, requestType);

            var result = response?.Values;
            if (response != null && response.IsLast)
            {
                return result;
            }

            var nextResult = await ProcessPaginatedRequestRecursively<T>(user, requestUrl, requestType, startAt + maxResults, maxResults);
            result?.AddRange(nextResult);

            return result;
        }

        private async Task<JiraApiActionCallResponse> ProcessRequestWithJiraApiActionCallResponse<T>(IntegratedUser user, string requestUrl, string requestType, object requestBody = null)
        {
            var response = await SendRequestAndGetRawResponse(user, requestUrl, requestType, requestBody);
            var responseObj = new JsonDeserializer(_logger).Deserialize<JiraResponse<T>>(response);

            return new JiraApiActionCallResponse
            {
                IsSuccess = responseObj.ResponseCode == 200,
                ErrorMessage = responseObj.Message
            };
        }

        private async Task<string> SendRequestAndGetRawResponse(IntegratedUser user, string requestUrl, string requestType, object requestBody = null)
        {
            var serverRequest = new JiraRequest
            {
                AccessToken = user.AccessToken ?? throw new NullReferenceException(nameof(user.AccessToken)),
                JiraId = user.JiraServerId,
                TeamsId = user.MsTeamsUserId,
                RequestUrl = requestUrl,
                RequestType = requestType,
                RequestBody = string.Empty
            };
            if (requestBody != null)
            {
                serverRequest.RequestBody = JsonConvert.SerializeObject(requestBody);
            }

            var message = JsonConvert.SerializeObject(serverRequest, _jiraSerializerSettings);
            var response = await _signalRService.SendRequestAndWaitForResponse(user.JiraServerId, message, CancellationToken.None);
            if (response.Received)
            {
                return response.Message;
            }

            await JiraHelpers.HandleJiraServerError(_databaseService, _logger, (int)HttpStatusCode.InternalServerError, response.Message, serverRequest.TeamsId, serverRequest.JiraId);
            return default;
        }

        private async Task<bool> CanUseNewCreateMetaEndpoints(IntegratedUser user)
        {
            var jiraCapabilities = await GetJiraCapabilities(user);
            return !string.IsNullOrEmpty(jiraCapabilities.ListIssueTypeFields) &&
                   !string.IsNullOrEmpty(jiraCapabilities.ListProjectIssueTypes);
        }

        private IEnumerable<string> GetSearchFieldsFilter()
        {
            return new List<string>()
            {
                "assignee",
                "created",
                "creator",
                "issuetype",
                "priority",
                "reporter",
                "status",
                "summary",
                "updated",
                "watches",
                "votes"
            };
        }
    }
}
