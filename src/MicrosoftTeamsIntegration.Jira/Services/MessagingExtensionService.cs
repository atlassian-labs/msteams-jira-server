using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using AdaptiveCards;
using AutoMapper;
using JetBrains.Annotations;
using Microsoft.ApplicationInsights;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Teams;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Schema.Teams;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MicrosoftTeamsIntegration.Artifacts.Extensions;
using MicrosoftTeamsIntegration.Artifacts.Services.Interfaces;
using MicrosoftTeamsIntegration.Jira.Dialogs;
using MicrosoftTeamsIntegration.Jira.Exceptions;
using MicrosoftTeamsIntegration.Jira.Helpers;
using MicrosoftTeamsIntegration.Jira.Models;
using MicrosoftTeamsIntegration.Jira.Models.Bot;
using MicrosoftTeamsIntegration.Jira.Models.FetchTask;
using MicrosoftTeamsIntegration.Jira.Models.Jira.Issue;
using MicrosoftTeamsIntegration.Jira.Models.MessageAction;
using MicrosoftTeamsIntegration.Jira.Services.Interfaces;
using MicrosoftTeamsIntegration.Jira.Settings;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Refit;

namespace MicrosoftTeamsIntegration.Jira.Services
{
    [UsedImplicitly]
    public sealed class MessagingExtensionService : IMessagingExtensionService
    {
        private const int IssueMessageLengthLimit = 1024;
        private const string CreateIssueCommand = "composeCreateCmd";
        private const string CreateCommentCommand = "composeCreateCommentCmd";
        private const int RegexTimeoutInSeconds = 2;

        private readonly AppSettings _appSettings;
        private readonly ILogger<MessagingExtensionService> _logger;
        private readonly IJiraService _jiraService;
        private readonly IMapper _mapper;
        private readonly IBotMessagesService _botMessagesService;
        private readonly IDistributedCacheService _distributedCacheService;
        private readonly TelemetryClient _telemetry;

        public MessagingExtensionService(
            IOptions<AppSettings> appSettings,
            ILogger<MessagingExtensionService> logger,
            IJiraService jiraService,
            IMapper mapper,
            IBotMessagesService botMessagesService,
            IDistributedCacheService distributedCacheService,
            TelemetryClient telemetry)
        {
            _appSettings = appSettings.Value;
            _logger = logger;
            _jiraService = jiraService;
            _mapper = mapper;
            _botMessagesService = botMessagesService;
            _distributedCacheService = distributedCacheService;
            _telemetry = telemetry;
        }

        public async Task<FetchTaskResponseEnvelope> HandleMessagingExtensionFetchTask(ITurnContext turnContext, IntegratedUser user)
        {
            var composeExtensionQuery = SafeCast<MessagingExtensionQuery>(turnContext.Activity.Value);
            string errorMessage;

            if (composeExtensionQuery?.CommandId == null)
            {
                errorMessage = "ComposeExtension/fetchTask request does not contain a command id.";

                _logger.LogError(errorMessage);

                return BuildSubmitActionMessageResponse(errorMessage);
            }

            if (composeExtensionQuery.CommandId.Equals(CreateIssueCommand, StringComparison.OrdinalIgnoreCase))
            {
                var jiraId = user.JiraServerId;
                var application = "jiraServerCompose";

                // the url has this format (domain/#/segment/segment;param1=value1;param2=value2) since we read params as snapshot.params on the client side
                var url = $"{_appSettings.BaseUrl}/#/issues/create;jiraUrl={Uri.EscapeDataString(jiraId)};application={application};returnIssueOnSubmit=true;source=messagingExtension";

                var issueDescription = GetRequestsContent(turnContext.Activity);
                if (!string.IsNullOrEmpty(issueDescription))
                {
                    url += $";description={issueDescription}";
                }

                // get comment metadata only in case we have initial description
                var msgMetadata = !string.IsNullOrEmpty(issueDescription) ? await GetMessageMetadata(turnContext) : null;
                if (msgMetadata != null)
                {
                    var metadataRef = Guid.NewGuid().ToString();
                    await _distributedCacheService.Set(metadataRef, msgMetadata);
                    url += $";metadataRef={metadataRef}";
                }

                return new FetchTaskResponseEnvelope
                {
                    Task = new FetchTaskResponse
                    {
                        Type = FetchTaskType.Continue,
                        Value = new FetchTaskResponseInfo
                        {
                            Title = "Create an issue",
                            Height = 522,
                            Width = 610,
                            Url = url,
                            FallbackUrl = url
                        }
                    }
                };
            }

            if (composeExtensionQuery.CommandId.Equals(CreateCommentCommand, StringComparison.OrdinalIgnoreCase))
            {
                var jiraUrl = user.JiraInstanceUrl;
                var jiraId = user.JiraServerId;
                var application = "jiraServerCompose";
                var issueComment = GetRequestsContent(turnContext.Activity);

                // get comment metadata only in case we have original comment
                var msgMetadata = !string.IsNullOrEmpty(issueComment) ? await GetMessageMetadata(turnContext) : null;

                var url = $"{_appSettings.BaseUrl}/#/issues/createComment;jiraUrl={Uri.EscapeDataString(jiraUrl)};jiraId={jiraId};application={application};comment={issueComment};source=messagingExtension";

                if (msgMetadata != null)
                {
                    var metadataRef = Guid.NewGuid().ToString();
                    await _distributedCacheService.Set(metadataRef, msgMetadata);
                    url += $";metadataRef={metadataRef}";
                }

                return new FetchTaskResponseEnvelope
                {
                    Task = new FetchTaskResponse
                    {
                        Type = FetchTaskType.Continue,
                        Value = new FetchTaskResponseInfo
                        {
                            Title = "Add a comment from the message",
                            Height = 600,
                            Width = 600,
                            Url = url,
                            FallbackUrl = url
                        }
                    }
                };
            }

            errorMessage = "ComposeExtension/fetchTask command id is invalid.";
            _logger.LogError(errorMessage);

            return BuildSubmitActionMessageResponse(errorMessage);
        }

        public FetchTaskResponseEnvelope HandleBotFetchTask(ITurnContext turnContext, IntegratedUser user)
        {
            FetchTaskBotCommand fetchTaskCommand = null;
            var value = turnContext.Activity?.Value as JObject;
            var data = value?["data"];
            if (data != null)
            {
                var dataWrapperObject = data.ToObject<JiraBotTeamsDataWrapper>();
                fetchTaskCommand = dataWrapperObject?.FetchTaskData;
            }

            var response = BuildTaskModuleResponse(turnContext, user, fetchTaskCommand);
            if (response != null)
            {
                return response;
            }

            var errorMessage = "Bot task/fetch command id is invalid.";
            _logger.LogError(errorMessage);

            return BuildSubmitActionMessageResponse(errorMessage);
        }

        public bool TryValidateMessagingExtensionQueryLink(ITurnContext turnContext, IntegratedUser user, out string jiraIssueIdOrKey)
        {
            AppBasedLinkQuery request;
            jiraIssueIdOrKey = null;

            try
            {
                request = JsonConvert.DeserializeObject<AppBasedLinkQuery>(turnContext.Activity?.Value?.ToString());
            }
            catch
            {
                return false;
            }

            // return false if url is null or empty or cannot be normalized
            if (string.IsNullOrWhiteSpace(request.Url) || !request.Url.TryToNormalizeJiraUrl(out var normalziedUrl))
            {
                return false;
            }

            // return false if current user's jira instance url is different from the requested one
            if (user == null || !normalziedUrl.StartsWith(user.JiraInstanceUrl, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // return false if jira issue id or key cannot be extracted from the requested url
            if (!request.Url.TryExtractJiraIdOrKeyFromUrl(out jiraIssueIdOrKey))
            {
                return false;
            }

            return true;
        }

        public bool TryValidateMessageExtensionFetchTask(ITurnContext turnContext, IntegratedUser user, out MessagingExtensionResponse response)
        {
            response = null;

            if (user == null)
            {
                response = BuildCardAction("auth", "Authorize in Jira", turnContext.Activity.Conversation.TenantId);

                return false;
            }

            var composeExtensionQuery = SafeCast<MessagingExtensionQuery>(turnContext.Activity.Value);
            if (composeExtensionQuery?.CommandId == null)
            {
                _logger.LogError("ComposeExtension/fetchTask request does not contain a command id.");
                return false;
            }

            if (!(composeExtensionQuery.CommandId.Equals(CreateIssueCommand, StringComparison.OrdinalIgnoreCase) ||
                composeExtensionQuery.CommandId.Equals(CreateCommentCommand, StringComparison.OrdinalIgnoreCase)))
            {
                _logger.LogError($"ComposeExtension/fetchTask request contains an invalid command id: \"{composeExtensionQuery.CommandId}\".");

                return false;
            }

            return true;
        }

        public async Task<object> HandleMessagingExtensionSubmitActionAsync(ITurnContext turnContext, IntegratedUser user)
        {
            var composeExtensionQuery = SafeCast<MessagingExtensionQuery>(turnContext.Activity.Value);
            string errorMessage;

            if (composeExtensionQuery?.CommandId == null)
            {
                errorMessage = "ComposeExtension/submitAction request does not contain a command id.";
                _logger.LogError(errorMessage);

                return BuildSubmitActionMessageResponse(errorMessage);
            }

            if (composeExtensionQuery.CommandId.Equals(CreateIssueCommand, StringComparison.OrdinalIgnoreCase))
            {
                CreateIssueSubmitActionRequest request;

                try
                {
                    request = JsonConvert.DeserializeObject<CreateIssueSubmitActionRequest>(turnContext.Activity?.Value?.ToString());
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);

                    return BuildSubmitActionMessageResponse("ComposeExtension/submitAction request data is invalid.");
                }

                if (string.IsNullOrEmpty(request?.Data))
                {
                    errorMessage = "ComposeExtension/submitAction request data issue key is invalid.";

                    _logger.LogError(errorMessage);

                    return BuildSubmitActionMessageResponse(errorMessage);
                }

                try
                {
                    var issue = await _jiraService.GetIssueByIdOrKey(user, request.Data);

                    return TranslateJiraIssueToMessagingExtensionResponse(issue, user, true);
                }
                catch (ApiException apiException)
                    when (apiException.StatusCode == HttpStatusCode.Unauthorized || apiException.StatusCode == HttpStatusCode.Forbidden)
                {
                    return BuildCardAction("auth", "Authorize in Jira", turnContext.Activity.Conversation.TenantId, user.JiraInstanceUrl);
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, exception.Message);

                    return BuildSubmitActionMessageResponse("Something went wrong while fetching the issue.");
                }
            }

            errorMessage = "ComposeExtension/submitAction command id is invalid.";

            _logger.LogError(errorMessage);

            return BuildSubmitActionMessageResponse(errorMessage);
        }

        public async Task<MessagingExtensionResponse> HandleMessagingExtensionQueryLinkAsync(ITurnContext turnContext, IntegratedUser user, string jiraIssueIdOrKey)
        {
            if (string.IsNullOrWhiteSpace(jiraIssueIdOrKey))
            {
                throw new ArgumentNullException(nameof(jiraIssueIdOrKey));
            }

            var tenantId = turnContext.Activity.Conversation.TenantId;

            if (user == null)
            {
                return BuildCardAction("auth", "Authorize in Jira", tenantId);
            }

            var userJiraInstanceUrl = user.JiraInstanceUrl;

            try
            {
                var issue = await _jiraService.GetIssueByIdOrKey(user, jiraIssueIdOrKey);

                _telemetry.TrackPageView("IssueQueryLink");

                _logger.LogError($"ComposeExtensionQueryLink: Try to create card for user {user?.JiraUserAccountId}");

                return TranslateJiraIssueToMessagingExtensionResponse(issue, user, true);
            }
            catch (ApiException apiException)
                when (apiException.StatusCode == HttpStatusCode.Unauthorized || apiException.StatusCode == HttpStatusCode.Forbidden)
            {
                _logger.LogError($"ComposeExtensionQueryLink: Authorizaton error");

                return BuildCardAction("auth", "Authorize in Jira", tenantId, userJiraInstanceUrl);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, $"ComposeExtensionQueryLink: Error: {exception.Message}");
            }

            return MessagingExtensionHelper.BuildMessageResponse($"We didn't find an issue with id or key \"{jiraIssueIdOrKey}\".");
        }

        public async Task<MessagingExtensionResponse> HandleMessagingExtensionQuery(ITurnContext turnContext, IntegratedUser user)
        {
            if (user == null
                || string.Equals(turnContext.Activity.Name, "composeExtension/querySettingUrl", StringComparison.InvariantCultureIgnoreCase))
            {
                return BuildCardAction("auth", "Authorize in Jira", turnContext.Activity.Conversation.TenantId);
            }

            var composeExtensionQuery = SafeCast<MessagingExtensionQuery>(turnContext.Activity.Value);

            // Check to make sure a query was actually made
            if (composeExtensionQuery?.CommandId == null || composeExtensionQuery.Parameters?.Count == 0)
            {
                return null;
            }

            var isInitialRun = false;
            var initialRunParameter = MessagingExtensionHelper.GetQueryParameterByName(composeExtensionQuery, "initialRun");

            // situation where the incoming payload was received from the config popup
            if (composeExtensionQuery.State.HasValue())
            {
                initialRunParameter = "true";
            }

            if (string.Equals(initialRunParameter, "true", StringComparison.OrdinalIgnoreCase))
            {
                isInitialRun = true;
            }

            var userJiraInstanceUrl = user.JiraInstanceUrl;
            try
            {
                var searchForIssuesRequest = JiraIssueSearchHelper.PrepareSearchParameter(composeExtensionQuery, isInitialRun);

                var maxResults = composeExtensionQuery.QueryOptions?.Count ?? 25;
                if (isInitialRun)
                {
                    maxResults = 10;
                }

                searchForIssuesRequest.MaxResults = maxResults;

                var apiResponse = await _jiraService.Search(user, searchForIssuesRequest);
                if (apiResponse?.ErrorMessages != null && apiResponse.ErrorMessages.Any())
                {
                    return MessagingExtensionHelper.BuildMessageResponse(apiResponse.ErrorMessages.FirstOrDefault());
                }

                var messagingExtensionResponse = TranslateSearchApiResponseToMessagingExtensionResponse(apiResponse, user);
                if (messagingExtensionResponse != null)
                {
                    return messagingExtensionResponse;
                }
            }
            catch (ApiException apiException)
                when (apiException.StatusCode == HttpStatusCode.Unauthorized || apiException.StatusCode == HttpStatusCode.Forbidden)
            {
                return BuildCardAction("auth", "Authorize in Jira", turnContext.Activity.Conversation.TenantId, userJiraInstanceUrl);
            }
            catch (UnauthorizedException ex)
            {
                // Jira Server addon is not installed or access token was revoked
                return BuildCardAction("auth", ex.Message, turnContext.Activity.Conversation.TenantId);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, exception.Message);
            }

            return MessagingExtensionHelper.BuildMessageResponse("We didn't find any matches.");
        }

        public async Task<FetchTaskResponseEnvelope> HandleTaskSubmitActionAsync(ITurnContext turnContext, IntegratedUser user)
        {
            FetchTaskBotCommand fetchTaskCommand = null;
            var value = turnContext.Activity?.Value as JObject;
            var data = value?["data"];
            if (data != null)
            {
                fetchTaskCommand = data.ToObject<FetchTaskBotCommand>();
            }

            if (fetchTaskCommand != null)
            {
                try
                {
                    switch (fetchTaskCommand.CommandName)
                    {
                        case "showMessageCard":
                            var updatedCard = new AdaptiveCard("1.0")
                            {
                                Body = new List<AdaptiveElement>
                                {
                                    new AdaptiveTextBlock
                                    {
                                        Text = fetchTaskCommand.CustomText,
                                        Wrap = true
                                    }
                                }
                            };
                            var updatedMessage = turnContext.Activity.CreateReply();
                            updatedMessage.Id = fetchTaskCommand.ReplyToActivityId;
                            updatedMessage.Attachments.Add(updatedCard.ToAttachment());

                            await turnContext.UpdateActivityAsync(updatedMessage);
                            break;
                        case "showIssueCard":
                            var card = await _botMessagesService.SearchIssueAndBuildIssueCard(turnContext, user, fetchTaskCommand.IssueKey);
                            if (card != null)
                            {
                                var message = turnContext.Activity.CreateReply();
                                message.Id = fetchTaskCommand.ReplyToActivityId;
                                message.Attachments.Add(card.ToAttachment());
                                await turnContext.UpdateActivityAsync(message);
                            }

                            break;
                        default:
                            break;
                    }
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "Cannot process task module submit task");
                }
            }

            return BuildTaskModuleResponse(turnContext, user, fetchTaskCommand);
        }

        private MessagingExtensionResponse TranslateSearchApiResponseToMessagingExtensionResponse(JiraIssueSearch response, IntegratedUser user)
        {
            if (response?.JiraIssues == null || !response.JiraIssues.Any())
            {
                return null;
            }

            var epicFieldName = JiraIssueSearchHelper.GetEpicFieldNameFromSchema(response.Schema);

            var attachments = new List<MessagingExtensionAttachment>(response.JiraIssues.Length);
            attachments.AddRange(response.JiraIssues.Select(jiraIssue => new BotAndMessagingExtensionJiraIssue
            {
                IsMessagingExtension = true,
                JiraIssue = jiraIssue,
                JiraInstanceUrl = user.JiraInstanceUrl,
                EpicFieldName = epicFieldName
            })
            .Select(model => _mapper.Map<MessagingExtensionAttachment>(model, _ => { })));

            return MessagingExtensionHelper.BuildMessagingExtensionQueryResult(attachments);
        }

        private MessagingExtensionResponse TranslateJiraIssueToMessagingExtensionResponse(JiraIssue issue, IntegratedUser user, bool isQueryLinkRequest)
        {
            var epicFieldName = JiraIssueSearchHelper.GetEpicFieldNameFromSchema(issue.Schema);

            var meIssue = new BotAndMessagingExtensionJiraIssue
            {
                IsMessagingExtension = true,
                JiraIssue = issue,
                JiraInstanceUrl = user.JiraInstanceUrl,
                EpicFieldName = epicFieldName
            };

            var attachment = _mapper.Map<MessagingExtensionAttachment>(meIssue, opts =>
            {
                if (isQueryLinkRequest)
                {
                    opts.Items.Add("previewIconPath", $"{_appSettings.BaseUrl}/icons/jira.png");
                    opts.Items.Add("isQueryLinkRequest", true);
                }
            });

            var attachments = new List<MessagingExtensionAttachment>
            {
                attachment
            };

            return MessagingExtensionHelper.BuildMessagingExtensionQueryResult(attachments);
        }

        private MessagingExtensionResponse BuildCardAction(string type, string title, string tenantId, string jiraUrl = null)
        {
            var composeApp = "jiraServerCompose";
            var url = $"{_appSettings.BaseUrl}/#/config;application={composeApp};tenantId={tenantId}";

            if (jiraUrl.HasValue())
            {
                url += $";predefinedJiraUrl={Uri.EscapeDataString(jiraUrl)}";
            }

            url += "?width=800&height=600";

            return MessagingExtensionHelper.BuildCardActionResponse(type, title, url);
        }

        private FetchTaskResponseEnvelope BuildSubmitActionMessageResponse(string message)
        {
            return new FetchTaskResponseEnvelope
            {
                Task = new FetchTaskResponse
                {
                    Type = FetchTaskType.Message,
                    Value = message
                }
            };
        }

        private FetchTaskResponseEnvelope BuildTaskModuleResponse(ITurnContext turnContext, IntegratedUser user, FetchTaskBotCommand fetchTaskCommand)
        {
            if (fetchTaskCommand != null)
            {
                var url = string.Empty;
                var taskModuleTitle = string.Empty;
                var taskModuleWidth = 610;
                var taskModuleHeight = 522;

                var jiraId = user?.JiraServerId;
                if (!string.IsNullOrEmpty(jiraId))
                {
                    jiraId = Uri.EscapeDataString(jiraId);
                }

                var application = "jiraServerCompose";

                if (fetchTaskCommand.CommandName.Equals(DialogMatchesAndCommands.EditIssueTaskModuleCommand, StringComparison.OrdinalIgnoreCase))
                {
                    url = $"{_appSettings.BaseUrl}/#/issues/edit;jiraUrl={jiraId};issueId={fetchTaskCommand.IssueId};issueKey={fetchTaskCommand.IssueKey};application={application};source=bot;replyToActivityId={turnContext.Activity.ReplyToId}";
                    taskModuleTitle = "Edit the issue";
                    taskModuleWidth = 710;
                }

                if (fetchTaskCommand.CommandName.Equals(DialogMatchesAndCommands.CreateNewIssueDialogCommand, StringComparison.OrdinalIgnoreCase))
                {
                    url = $"{_appSettings.BaseUrl}/#/issues/create;jiraUrl={Uri.EscapeDataString(jiraId)};application={application};returnIssueOnSubmit=false;source=bot;replyToActivityId={turnContext.Activity.ReplyToId}";
                    taskModuleTitle = "Create an issue";
                }

                return new FetchTaskResponseEnvelope
                {
                    Task = new FetchTaskResponse
                    {
                        Type = FetchTaskType.Continue,
                        Value = new FetchTaskResponseInfo
                        {
                            Title = taskModuleTitle,
                            Height = taskModuleHeight,
                            Width = taskModuleWidth,
                            Url = url,
                            FallbackUrl = url
                        }
                    }
                };
            }

            return null;
        }

        private string GetRequestsContent(Activity activity)
        {
            var request = default(MessageActionRequest);

            try
            {
                request = JsonConvert.DeserializeObject<MessageActionRequest>(activity?.Value?.ToString());
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, exception.Message);
            }

            // list of unsupported strings that can come as part of message payload content
            var unsupportedStrings = new List<string>() { @"<attachment[^>]*>.*<\/attachment[^>]*>" };

            // append issue default description in case if message action payload is not null
            // and its content type is text, since we don't know how to properly parse html
            if (!string.IsNullOrWhiteSpace(request?.Payload?.Body?.Content))
            {
                var content = request.Payload?.Body?.Content;

                try
                {
                    if (request.Payload.Body.ContentType == MessageActionContentType.Text)
                    {
                        // remove all unsupported strings as we need to take just original message
                        foreach (var unsupportedStr in unsupportedStrings)
                        {
                            content = new Regex(
                                unsupportedStr,
                                RegexOptions.IgnoreCase,
                                TimeSpan.FromSeconds(RegexTimeoutInSeconds)).Replace(content, string.Empty);
                        }
                    }
                    else if (request.Payload.Body.ContentType == MessageActionContentType.Html)
                    {
                        // remove all tags from html text
                        content = new Regex(
                            "<.*?>",
                            RegexOptions.IgnoreCase,
                            TimeSpan.FromSeconds(RegexTimeoutInSeconds)).Replace(content, string.Empty).Trim();

                        // decode html symbols to display it as plain text
                        content = HttpUtility.HtmlDecode(content);
                    }
                    else
                    {
                        return string.Empty;
                    }
                }
                catch (RegexMatchTimeoutException regexException)
                {
                    _logger.LogWarning(
                        regexException,
                        $"Matching of regex took longer then expected. String to match: {content}");
                }

                // if content of message action payload is more than the limit, crop it
                if (content.Length > IssueMessageLengthLimit)
                {
                    var croppedContent = content.Substring(0, IssueMessageLengthLimit - 3);
                    content = croppedContent + "...";
                }

                return Uri.EscapeDataString(content);
            }

            return string.Empty;
        }

        private DateTimeOffset GetRequestsTimestamp(Activity activity)
        {
            var request = default(MessageActionRequest);

            try
            {
                request = JsonConvert.DeserializeObject<MessageActionRequest>(activity?.Value?.ToString());
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, exception.Message);
            }

            // get message creation timestamp
            if (request?.Payload?.CreatedDateTime != null)
            {
                try
                {
                    var offset = activity.LocalTimestamp.Value.Offset;
                    var dateTime = request.Payload.CreatedDateTime.Add(offset);
                    return new DateTimeOffset(dateTime);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Cannot get metadata timestamp." + e.Message);
                }
            }

            return DateTimeOffset.MinValue;
        }

        private DateTime GetCreatedDateTime(Activity activity)
        {
            var request = default(MessageActionRequest);

            try
            {
                request = JsonConvert.DeserializeObject<MessageActionRequest>(activity?.Value?.ToString());
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, exception.Message);
            }

            if (request?.Payload?.CreatedDateTime != null)
            {
                return request.Payload.CreatedDateTime;
            }

            return DateTime.Now;
        }

        private string GetRequestsUser(Activity activity)
        {
            var request = default(MessageActionRequest);

            try
            {
                request = JsonConvert.DeserializeObject<MessageActionRequest>(activity?.Value?.ToString());
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }

            // get message author
            if (!string.IsNullOrWhiteSpace(request?.Payload?.From?.User?.DisplayName))
            {
                return request.Payload.From.User.DisplayName;
            }

            return string.Empty;
        }

        private string GetRequestsApplication(Activity activity)
        {
            var request = default(MessageActionRequest);

            try
            {
                request = JsonConvert.DeserializeObject<MessageActionRequest>(activity?.Value?.ToString());
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }

            if (string.IsNullOrWhiteSpace(request?.Payload?.From?.Application?.DisplayName))
            {
                return string.Empty;
            }

            return request?.Payload?.From?.Application?.DisplayName;
        }

        private string GetLinkToMessage(Activity activity)
        {
            var request = default(MessageActionRequest);

            try
            {
                request = JsonConvert.DeserializeObject<MessageActionRequest>(activity?.Value?.ToString());
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }

            if (string.IsNullOrWhiteSpace(request?.Payload?.LinkToMessage))
            {
                return string.Empty;
            }

            return request?.Payload?.LinkToMessage;
        }

        private async Task<MessageMetadata> GetMessageMetadata(ITurnContext turnContext)
        {
            var messageMetadata = new MessageMetadata();

            try
            {
                messageMetadata.ConversationId = turnContext.Activity.Conversation.Id;
                messageMetadata.ConversationType = turnContext.Activity.Conversation.ConversationType;
                messageMetadata.Author = GetRequestsUser(turnContext.Activity);
                messageMetadata.Application = GetRequestsApplication(turnContext.Activity);
                messageMetadata.Locale = turnContext.Activity.Locale;
                messageMetadata.CreatedDateTime = GetCreatedDateTime(turnContext.Activity);

                var channelId = turnContext.Activity.TeamsGetChannelId();
                messageMetadata.ChannelId = channelId;

                try
                {
                    var allTeamChannels = await TeamsInfo.GetTeamChannelsAsync(turnContext);
                    messageMetadata.Channel = allTeamChannels.FirstOrDefault(x => x.Id == channelId)?.Name ?? "General";

                    var teamDetails = await TeamsInfo.GetTeamDetailsAsync(turnContext);
                    messageMetadata.TeamId = teamDetails.Id;
                    messageMetadata.Team = teamDetails.Name;
                }
                catch
                {
                    // ignore exception
                }

                // get original message creation timestamp
                var timestamp = GetRequestsTimestamp(turnContext.Activity);
                if (timestamp != DateTimeOffset.MinValue)
                {
                    messageMetadata.Timestamp = timestamp;
                }

                messageMetadata.DeepLink = GetLinkToMessage(turnContext.Activity);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }

            return messageMetadata;
        }

        private static T SafeCast<T>(object value)
        {
            var obj = value as JObject;
            if (obj == null)
            {
                throw new Exception($"expected type '{value.GetType().Name}'");
            }

            return obj.ToObject<T>();
        }
    }
}
