using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using MicrosoftTeamsIntegration.Artifacts.Extensions;
using MicrosoftTeamsIntegration.Artifacts.Models;
using MicrosoftTeamsIntegration.Artifacts.Services.Interfaces;
using MicrosoftTeamsIntegration.Jira.Exceptions;
using MicrosoftTeamsIntegration.Jira.Extensions;
using MicrosoftTeamsIntegration.Jira.Filters;
using MicrosoftTeamsIntegration.Jira.Models;
using MicrosoftTeamsIntegration.Jira.Models.Dto;
using MicrosoftTeamsIntegration.Jira.Models.Jira;
using MicrosoftTeamsIntegration.Jira.Models.Jira.Issue;
using MicrosoftTeamsIntegration.Jira.Services.Interfaces;
using MicrosoftTeamsIntegration.Jira.Settings;
using Refit;

namespace MicrosoftTeamsIntegration.Jira.Controllers
{
    [JiraAuthentication]
    [Route("api")]
    public class JiraApiController : BaseApiController
    {
        private readonly IDatabaseService _databaseService;
        private readonly AppSettings _appSettings;
        private readonly IJiraService _jiraService;
        private readonly IMapper _mapper;
        private readonly IJiraAuthService _jiraAuthService;
        private readonly IDistributedCacheService _distributedCacheService;
        private readonly ILogger<JiraApiController> _logger;
        private readonly ClientAppOptions _clientAppOptions;

        public JiraApiController(
            IDatabaseService databaseService,
            IOptions<AppSettings> appSettings,
            IJiraService jiraService,
            IMapper mapper,
            IJiraAuthService jiraAuthService,
            IDistributedCacheService distributedCacheService,
            ILogger<JiraApiController> logger,
            IOptions<ClientAppOptions> clientAppOptions)
        {
            _databaseService = databaseService;
            _jiraService = jiraService;
            _mapper = mapper;
            _jiraAuthService = jiraAuthService;
            _appSettings = appSettings.Value;
            _distributedCacheService = distributedCacheService;
            _logger = logger;
            _clientAppOptions = clientAppOptions.Value;
        }

        [HttpGet("personalScope/url")]
        public async Task<IActionResult> GetJiraUrlForPersonalScope()
        {
            var msTeamsUserId = GetUserOid();
            var user = await _databaseService.GetJiraServerUserWithConfiguredPersonalScope(msTeamsUserId);

            var isJiraConnected = await _jiraAuthService.IsJiraConnected(user);
            var jiraUrl = isJiraConnected ? user?.JiraServerId : string.Empty;

            return Ok(new
            {
                jiraUrl
            });
        }

        [HttpGet("addon-status")]
        public async Task<IActionResult> GetJiraAddonStatus(string jiraUrl)
        {
            var msTeamsUserId = GetUserOid();
            var jiraServerId = jiraUrl;

            if (jiraUrl.HasValue())
            {
                jiraUrl = Uri.UnescapeDataString(jiraUrl);
            }

            var result = await _databaseService.GetJiraServerAddonStatus(jiraServerId, msTeamsUserId);

            var addonStatus = AddonStatus.NotInstalled;

            if (result != null && result.AddonIsInstalled)
            {
                addonStatus = AddonStatus.Installed;
            }

            if (result != null && result.AddonIsInstalled && result.AddonIsConnected)
            {
                addonStatus = AddonStatus.Connected;
            }

            return Ok(new { AddonStatus = addonStatus, AddonVersion = result?.Version });
        }

        [HttpGet("myself")]
        public async Task<IActionResult> GetDataAboutMyself(string jiraUrl)
        {
            var result = new MyselfInfo();
            var user = await GetAndVerifyUser(jiraUrl);
            result = await _jiraService.GetDataAboutMyself(user);
            result.JiraServerInstanceUrl = user.JiraInstanceUrl;

            return Ok(result);
        }

        [HttpGet("tenant-info")]
        public async Task<IActionResult> GetJiraTenantInfo(string jiraUrl)
        {
            var result = new JiraTenantInfo();
            try
            {
                var user = await GetAndVerifyUser(jiraUrl);
                result = await _jiraService.GetJiraTenantInfo(user);
                return Ok(result);
            }
            catch (Exception)
            {
                // Returning an empty object
                return Ok(result);
            }
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search(string jiraUrl, string jql, int? startAt)
        {
            var user = await GetAndVerifyUser(jiraUrl);
            var pageSize = _clientAppOptions.ResultItemsPerPage;

            var searchRequest = new SearchForIssuesRequest
            {
                Jql = Uri.UnescapeDataString(jql),
                MaxResults = pageSize,
                StartAt = startAt.GetValueOrDefault(0)
            };
            var result = await _jiraService.Search(user, searchRequest);

            var model = _mapper.Map<JiraIssueSearchResponseDto>(result);
            model.PageSize = pageSize;

            return Ok(model);
        }

        [HttpGet("projects")]
        public async Task<IActionResult> Projects(string jiraUrl, bool? getAvatars)
        {
            var user = await GetAndVerifyUser(jiraUrl);
            var getAvatarsValue = getAvatars ?? false;
            var result = await _jiraService.GetProjects(user, getAvatarsValue);
            return Ok(result);
        }

        [HttpGet("project")]
        public async Task<IActionResult> Project(string jiraUrl, string projectKey)
        {
            var user = await GetAndVerifyUser(jiraUrl);
            var result = await _jiraService.GetProject(user, projectKey);
            return Ok(result);
        }

        [HttpGet("statuses")]
        public async Task<IActionResult> Statuses(string jiraUrl)
        {
            var user = await GetAndVerifyUser(jiraUrl);
            var result = await _jiraService.GetStatuses(user);
            result = result.DistinctBy(s => s.Name)
                .OrderBy(s => s.Name)
                .ToList();
            return Ok(result);
        }

        [HttpGet("project-statuses")]
        public async Task<IActionResult> GetStatusesByProject(string jiraUrl, string projectIdOrKey)
        {
            var user = await GetAndVerifyUser(jiraUrl);
            var result = await _jiraService.GetStatusesByProject(user, projectIdOrKey);
            return Ok(result);
        }

        [HttpGet("priorities")]
        public async Task<IActionResult> Priorities(string jiraUrl)
        {
            var user = await GetAndVerifyUser(jiraUrl);
            var result = await _jiraService.GetPriorities(user);
            return Ok(result);
        }

        [HttpGet("types")]
        public async Task<IActionResult> Types(string jiraUrl)
        {
            var user = await GetAndVerifyUser(jiraUrl);
            var result = await _jiraService.GetTypes(user);
            return Ok(result);
        }

        [HttpGet("filters")]
        public async Task<IActionResult> Filters(string jiraUrl)
        {
            var user = await GetAndVerifyUser(jiraUrl);
            var result = await _jiraService.GetFilters(user);
            return Ok(result);
        }

        [HttpGet("projects-all")]
        public async Task<IActionResult> GetProjects(string jiraUrl, bool? getAvatars)
        {
            var user = await GetAndVerifyUser(jiraUrl);
            var getAvatarsValue = getAvatars ?? false;
            var result = await _jiraService.GetProjects(user, getAvatarsValue);
            return Ok(result);
        }

        [HttpGet("projects-search")]
        public async Task<IActionResult> SearchProjects(string jiraUrl, string filterName = null, bool getAvatars = false)
        {
            var user = await GetAndVerifyUser(jiraUrl);
            var result = await _jiraService.FindProjects(user, filterName, getAvatars);
            return Ok(result);
        }

        [HttpGet("filters-search")]
        public async Task<IActionResult> SearchFilters(string jiraUrl, string filterName = null)
        {
            var user = await GetAndVerifyUser(jiraUrl);
            var result = await _jiraService.SearchFilters(user, filterName);
            return Ok(result);
        }

        [HttpGet("filter")]
        public async Task<IActionResult> GetFilter(string jiraUrl, string filterId)
        {
            var user = await GetAndVerifyUser(jiraUrl);
            var result = await _jiraService.GetFilter(user, filterId);
            return Ok(result);
        }

        [HttpGet("favourite-filters")]
        public async Task<IActionResult> FavouriteFilters(string jiraUrl)
        {
            var user = await GetAndVerifyUser(jiraUrl);
            var result = await _jiraService.GetFavouriteFilters(user);
            return Ok(result);
        }

        [HttpGet("auth/url")]
        public async Task<IActionResult> GetAuthUrl(string jiraUrl, string application, bool? staticTabChangeUrl)
        {
            var msTeamsUserId = GetUserOid();
            var msTeamsTenantId = GetUserTid();
            string redirectUrl;

            if (jiraUrl.HasValue())
            {
                jiraUrl = Uri.UnescapeDataString(jiraUrl);
            }

            if (string.IsNullOrEmpty(jiraUrl) || staticTabChangeUrl.HasValue)
            {
                redirectUrl = $"/#/config;application={application}";
                if (jiraUrl.HasValue())
                {
                    redirectUrl += $";predefinedJiraUrl={Uri.EscapeDataString(jiraUrl)}";
                }

                redirectUrl += "?width=800&height=600";
            }
            else
            {
                var user = await _databaseService.GetOrCreateUser(msTeamsUserId, msTeamsTenantId, jiraUrl);
                redirectUrl = GetJiraAuthUrlForUser(user);
                redirectUrl = Uri.EscapeDataString(redirectUrl);
            }

            if (application.HasValue() && jiraUrl.HasValue())
            {
                await _databaseService.UpdateUserActiveJiraInstanceForPersonalScope(msTeamsUserId, jiraUrl);
            }

            return Ok(new
            {
                jiraAuthUrl = redirectUrl
            });
        }

        [HttpGet("issue")]
        public async Task<IActionResult> GetIssueByIdOrKey(string jiraUrl, string issueIdOrKey)
        {
            var user = await GetAndVerifyUser(jiraUrl);
            var result = await _jiraService.GetIssueByIdOrKey(user, issueIdOrKey);

            return Ok(result);
        }

        [HttpPost("issue")]
        public async Task<IActionResult> CreateIssue(string jiraUrl, [FromBody] CreateJiraIssueRequest model)
        {
            if (((dynamic)model).Fields.summary.Length > 254)
            {
                return Ok(
                    new JiraApiActionCallResponse
                    {
                        IsSuccess = false,
                        ErrorMessage = "Summary must be less than 255 characters"
                    });
            }

            if (model.MetadataRef.HasValue())
            {
                var metadataMessage = await GetDefaultMetadataMessage(model.MetadataRef);

                if (!string.IsNullOrEmpty(metadataMessage))
                {
                    ((dynamic)model).Fields.description +=
                        "\r\n\r\n" +
                        metadataMessage;
                }
            }

            var user = await GetAndVerifyUser(jiraUrl);
            var createdIssue = await _jiraService.CreateIssue(user, model);

            return Ok(new JiraApiActionCallResponseWithContent<JiraIssue>
            {
                IsSuccess = createdIssue != null,
                Content = createdIssue
            });
        }

        [HttpPut("issue")]
        public async Task<IActionResult> UpdateIssue(string jiraUrl, string issueIdOrKey, [FromBody] JiraIssueFields model)
        {
            if (model.Summary != null && model.Summary.Length > 254)
            {
                return Ok(
                    new JiraApiActionCallResponse
                    {
                        IsSuccess = false,
                        ErrorMessage = "Summary must be less than 255 characters"
                    });
            }

            var user = await GetAndVerifyUser(jiraUrl);
            var result = await _jiraService.UpdateIssue(user, issueIdOrKey, model);

            return Ok(result);
        }

        [HttpGet("issue/createmeta/issuetypes")]
        public async Task<IActionResult> GetCreateMetaIssueTypes(string jiraUrl, string projectKeyOrId)
        {
            var user = await GetAndVerifyUser(jiraUrl);
            var result = await _jiraService.GetCreateMetaIssueTypes(user, projectKeyOrId);

            return Ok(result);
        }

        [HttpGet("issue/createmeta/fields")]
        public async Task<IActionResult> GetCreateMetaIssueTypeFields(string jiraUrl, string projectKeyOrId, string issueTypeId, string issueTypeName)
        {
            var user = await GetAndVerifyUser(jiraUrl);
            var result = await _jiraService.GetCreateMetaIssueTypeFields(user, projectKeyOrId, issueTypeId, issueTypeName);

            return Ok(result);
        }

        [HttpGet("issue/editmeta")]
        public async Task<IActionResult> GetIssueEditMeta(string jiraUrl, string issueIdOrKey)
        {
            var user = await GetAndVerifyUser(jiraUrl);
            var result = await _jiraService.GetIssueEditMeta(user, issueIdOrKey);

            return Ok(result);
        }

        [HttpPut("issue/description")]
        public async Task<IActionResult> UpdateIssueDescription(string jiraUrl, string issueIdOrKey, [FromBody] UpdateIssueDescriptionRequestModel model)
        {
            var user = await GetAndVerifyUser(jiraUrl);
            var result = await _jiraService.UpdateDescription(user, issueIdOrKey, model.Description);
            return Ok(result);
        }

        [HttpPost("addComment")]
        public async Task<IActionResult> AddComment(string jiraUrl, string issueIdOrKey, string comment)
        {
            var user = await GetAndVerifyUser(jiraUrl);
            var result = await _jiraService.AddComment(user, issueIdOrKey, comment);
            return Ok(result);
        }

        [HttpPost("addCommentAndGetItBack")]
        public async Task<IActionResult> AddCommentAndGetItBack([FromBody] AddIssueCommentModel model)
        {
            var user = await GetAndVerifyUser(model.JiraUrl);

            if (model.MetadataRef.HasValue())
            {
                var metadataMessage = await GetDefaultMetadataMessage(model.MetadataRef);

                if (!string.IsNullOrEmpty(metadataMessage))
                {
                    model.Comment +=
                        "\r\n\r\n" +
                        metadataMessage;
                }
            }

            var result = await _jiraService.AddCommentAndGetItBack(user, model.IssueIdOrKey, model.Comment);
            return Ok(result);
        }

        [HttpPut("updateComment")]
        public async Task<IActionResult> UpdateComment([FromBody] UpdateIssueCommentModel model)
        {
            var user = await GetAndVerifyUser(model.JiraUrl);
            var result = await _jiraService.UpdateComment(user, model.IssueIdOrKey, model.CommentId, model.Comment);
            return Ok(result);
        }

        [HttpPut("issue/summary")]
        public async Task<IActionResult> UpdateSummary(string jiraUrl, string issueIdOrKey, string summary)
        {
            var user = await GetAndVerifyUser(jiraUrl);
            var result = await _jiraService.UpdateSummary(user, issueIdOrKey, summary);

            return Ok(result);
        }

        [HttpPut("issue/assignee")]
        public async Task<IActionResult> Assignee(string jiraUrl, string issueIdOrKey, string assigneeAccountIdOrName)
        {
            var user = await GetAndVerifyUser(jiraUrl);
            var result = await _jiraService.Assign(user, issueIdOrKey, assigneeAccountIdOrName);

            return Ok(result);
        }

        [HttpPut]
        public async Task<IActionResult> AssigneToMe(string jiraUrl, string issueIdOrKey)
        {
            var user = await GetAndVerifyUser(jiraUrl);
            var result = await _jiraService.Assign(user, issueIdOrKey, user.JiraUserAccountId);

            return Ok(result);
        }

        [HttpGet("issue/searchAssignable")]
        public async Task<IActionResult> SearchAssignable(string jiraUrl, string issueKey, string projectKey, string query = "")
        {
            var user = await GetAndVerifyUser(jiraUrl);
            var username = query ?? string.Empty;
            var result = await _jiraService.SearchAssignable(user, issueKey, projectKey, username);

            return Ok(result);
        }

        [HttpGet("user/assignable/multiProjectSearch")]
        public async Task<IActionResult> SearchAssignableMultiProject(string jiraUrl, string projectKey, string username = "")
        {
            var user = await GetAndVerifyUser(jiraUrl);
            var result = await _jiraService.SearchAssignableMultiProject(user, projectKey, username ?? string.Empty);

            return Ok(result);
        }

        [HttpGet("user/search")]
        public async Task<IActionResult> SearchUsers(string jiraUrl, string username = "")
        {
            var user = await GetAndVerifyUser(jiraUrl);
            var result = await _jiraService.SearchUsers(user, username ?? string.Empty);

            return Ok(result);
        }

        [HttpPut("issue/updatePriority")]
        public async Task<IActionResult> UpdatePriority(string jiraUrl, string issueIdOrKey, [FromBody] JiraIssuePriority priority)
        {
            var user = await GetAndVerifyUser(jiraUrl);
            var result = await _jiraService.UpdatePriority(user, issueIdOrKey, priority.Id);

            return Ok(result);
        }

        [HttpGet("issue/transitions")]
        public async Task<IActionResult> GetTransitions(string jiraUrl, string issueIdOrKey)
        {
            var user = await GetAndVerifyUser(jiraUrl);
            var result = await _jiraService.GetTransitions(user, issueIdOrKey);

            return Ok(result);
        }

        [HttpPost("issue/transitions")]
        public async Task<IActionResult> DoTransition(string jiraUrl, string issueIdOrKey, [FromBody] DoTransitionRequest model)
        {
            var user = await GetAndVerifyUser(jiraUrl);
            var result = await _jiraService.DoTransition(user, issueIdOrKey, model);

            return Ok(result);
        }

        [HttpGet("mypermissions")]
        public async Task<IActionResult> GetMyPermissions(string jiraUrl, string permissions, string issueId, string projectKey)
        {
            var user = await GetAndVerifyUser(jiraUrl);
            var result = await _jiraService.GetMyPermissions(user, permissions, issueId, projectKey);

            return Ok(result);
        }

        [HttpPost("submit-login-info")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
        [ProducesDefaultResponseType]
        public async Task<IActionResult> SubmitLoginInfo([FromBody] JiraAuthParamMessage authParamMessage)
        {
            if (string.IsNullOrEmpty(authParamMessage.VerificationCode))
            {
                var msTeamsUserId = GetUserOid();
                var jiraServerId = authParamMessage.JiraId;
                var addonStatus = await _databaseService.GetJiraServerAddonStatus(jiraServerId, msTeamsUserId);
                var shouldReturnError = addonStatus == null || string.IsNullOrEmpty(addonStatus.Version);

                if (shouldReturnError)
                {
                    var errorMessage = $"Please contact your Jira Server administrator and ask him to install Jira addon application.";
                    var error = new ApiError(errorMessage);
                    return BadRequest(error);
                }
            }

            var result = new JiraAuthResponse();
            try
            {
                result = await _jiraAuthService.SubmitOauthLoginInfo(GetUserOid(), GetUserTid(), GetUserAccessToken(), authParamMessage.JiraId, authParamMessage.VerificationCode, authParamMessage.RequestToken);

                return Ok(result);
            }
            catch (UnauthorizedException)
            {
                result.IsSuccess = false;
                result.Message = JiraConstants.UserNotAuthorizedMessage;

                return Ok(result);
            }
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout(string jiraId)
        {
            var result = new JiraAuthResponse();
            try
            {
                var user = await GetAndVerifyUser(jiraId);
                result = await _jiraAuthService.Logout(user);
                return Ok(result);
            }
            catch (UnauthorizedException)
            {
                result.IsSuccess = false;
                result.Message = JiraConstants.UserNotAuthorizedMessage;

                return Ok(result);
            }
        }

        [HttpPost("save-jira-server-id")]
        public async Task<IActionResult> SaveJiraServerId(string jiraServerId)
        {
            var msTeamsUserId = GetUserOid();

            await _databaseService.UpdateJiraServerUserActiveJiraInstanceForPersonalScope(msTeamsUserId, jiraServerId);

            return Ok(new { isSuccess = true, message = string.Empty });
        }

        [HttpGet("validate-connection")]
        public async Task<IActionResult> ValidateConnection(string jiraServerId)
        {
            var user = await GetAndVerifyUser(jiraServerId);
            var addonStatus = await _databaseService.GetJiraServerAddonSettingsByJiraId(jiraServerId);
            if (addonStatus == null)
            {
                return Ok(new { IsSuccess = false });
            }

            if (addonStatus.AddonIsInstalled)
            {
                // await _databaseService.GetUserByTeamsUserIdAndJiraUrl(user.MsTeamsUserId, user.JiraServerId);
                if (user.HasJiraAuthInfo())
                {
                    return Ok(new { IsSuccess = true });
                }
            }

            return Ok(new { IsSuccess = false });
        }

        [HttpGet("issue/autocompletedata")]
        public async Task<IActionResult> GetFieldAutocompleteData(string jiraUrl, string fieldName = "")
        {
            var user = await GetAndVerifyUser(jiraUrl);
            var result = await _jiraService.GetFieldAutocompleteData(user, fieldName ?? string.Empty);

            return Ok(result);
        }

        [HttpGet("issue/sprint")]
        public async Task<IActionResult> GetSprints(string jiraUrl, string projectKeyOrId)
        {
            var user = await GetAndVerifyUser(jiraUrl);
            var result = await _jiraService.GetAllSprintsForProject(user, projectKeyOrId);

            return Ok(result);
        }

        [HttpGet("issue/epic")]
        public async Task<IActionResult> GetEpics(string jiraUrl, string projectKeyOrId)
        {
            var user = await GetAndVerifyUser(jiraUrl);
            var result = await _jiraService.GetAllEpicsForProject(user, projectKeyOrId);

            return Ok(result);
        }

        private async Task<IntegratedUser> GetAndVerifyUser(string jiraUrl)
        {
            if (jiraUrl.HasValue())
            {
                jiraUrl = Uri.UnescapeDataString(jiraUrl);
            }

            var msTeamsUserId = GetUserOid();
            var user = await _databaseService.GetUserByTeamsUserIdAndJiraUrl(msTeamsUserId, jiraUrl);
            var isJiraConnected = await _jiraAuthService.IsJiraConnected(user);

            if (!isJiraConnected)
            {
                throw new UnauthorizedException();
            }

            if (user != null)
            {
                user.AccessToken = GetUserAccessToken();
            }

            if (user.HasJiraAuthInfo())
            {
                return user;
            }

            if (user is null)
            {
                throw new UnauthorizedException();
            }

            var addonStatus = await _databaseService.GetJiraServerAddonSettingsByJiraId(jiraUrl);

            var message = user.HasJiraAuthInfo()
                ? addonStatus.GetErrorMessage(jiraUrl)
                : JiraConstants.UserNotAuthorizedMessage;

            var response = new HttpResponseMessage(HttpStatusCode.Forbidden)
            {
                Content = new StringContent(message)
            };
            var exception = await ApiException.Create(
                new HttpRequestMessage(),
                HttpMethod.Post,
                response);
            throw exception;
        }

        private string GetJiraAuthUrlForUser(IntegratedUser user)
        {
            if (user.HasJiraAuthInfo())
            {
                return "/loginResult.html";
            }

            var msIdToken = string.Empty;
            if (Request.Headers.TryGetValue(HeaderNames.Authorization, out var value))
            {
                msIdToken = value.ToString();
                if (msIdToken.HasValue())
                {
                    msIdToken = msIdToken.Substring("Bearer ".Length);
                }
            }

            var state = Guid.NewGuid().ToString();
            Response.Cookies.Append("state", state, new CookieOptions()
            {
                SameSite = Microsoft.AspNetCore.Http.SameSiteMode.None,
                Secure = true,
                Expires = DateTimeOffset.UtcNow.AddMinutes(10),
                IsEssential = true
            });

            var postBack = HttpUtility.UrlEncode($"{user.JiraInstanceUrl}/plugins/servlet/ac/{_appSettings.AddonKey}/proxy?ac.accessToken={msIdToken}&ac.state={state}");
            return $"{_appSettings.IdentityServiceUrl}/login?continue={postBack}";
        }

        private async Task<string> GetDefaultMetadataMessage(string metadataRef)
        {
            try
            {
                var messageMetadata = await _distributedCacheService.Get<MessageMetadata>(metadataRef);
                var message = messageMetadata.Message;

                // if we don't have message send in metadata, build default message
                if (string.IsNullOrEmpty(message))
                {
                    if (!string.IsNullOrEmpty(messageMetadata.DeepLink))
                    {
                        var timestampStr = messageMetadata.Timestamp != DateTimeOffset.MinValue ?
                            $" at {messageMetadata.Timestamp.DateTime.ToString(new CultureInfo(messageMetadata.Locale))}" :
                            string.Empty;
                        var authorStr = !string.IsNullOrEmpty(messageMetadata.Author) ?
                            $" from {messageMetadata.Author}" :
                            string.Empty;
                        message = $"[View original message{authorStr} posted in {messageMetadata.Team}/{messageMetadata.Channel}{timestampStr}|{messageMetadata.DeepLink}]";
                    }
                }

                return message;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, exception.Message);
            }

            // if we cannot get metadata, return empty string
            return string.Empty;
        }
    }
}
