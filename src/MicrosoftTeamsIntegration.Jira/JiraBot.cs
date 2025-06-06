using System;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Teams;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Schema.Teams;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MicrosoftTeamsIntegration.Artifacts.Extensions;
using MicrosoftTeamsIntegration.Jira.Dialogs;
using MicrosoftTeamsIntegration.Jira.Dialogs.Dispatcher;
using MicrosoftTeamsIntegration.Jira.Extensions;
using MicrosoftTeamsIntegration.Jira.Helpers;
using MicrosoftTeamsIntegration.Jira.Models;
using MicrosoftTeamsIntegration.Jira.Services.Interfaces;
using MicrosoftTeamsIntegration.Jira.Settings;
using Newtonsoft.Json.Linq;

namespace MicrosoftTeamsIntegration.Jira
{
    [UsedImplicitly]
    public class JiraBot : TeamsActivityHandler
    {
        private readonly JiraBotAccessors _accessors;
        private readonly IMessagingExtensionService _messagingExtensionService;
        private readonly IDatabaseService _databaseService;
        private readonly INotificationSubscriptionService _notificationSubscriptionService;
        private readonly IBotMessagesService _botMessagesService;
        private readonly IJiraService _jiraService;
        private readonly IActionableMessageService _actionableMessageService;
        private readonly DialogSet _dialogs;
        private readonly AppSettings _appSettings;
        private readonly IJiraAuthService _jiraAuthService;
        private readonly ILogger<JiraBot> _logger;
        private readonly TelemetryClient _telemetry;
        private readonly IUserTokenService _userTokenService;
        private readonly ICommandDialogReferenceService _commandDialogReferenceService;
        private readonly IBotFrameworkAdapterService _botFrameworkAdapterService;
        private readonly IAnalyticsService _analyticsService;
        private readonly IUserService _userService;
        private EventTelemetry _eventTelemetry;

        public JiraBot(
            IMessagingExtensionService messagingExtensionService,
            IDatabaseService databaseService,
            INotificationSubscriptionService notificationSubscriptionService,
            JiraBotAccessors accessors,
            IBotMessagesService botMessagesService,
            IJiraService jiraService,
            IActionableMessageService actionableMessageService,
            IOptions<AppSettings> appSettings,
            IJiraAuthService jiraAuthService,
            ILogger<JiraBot> logger,
            TelemetryClient telemetry,
            IUserTokenService userTokenService,
            ICommandDialogReferenceService commandDialogReferenceService,
            IBotFrameworkAdapterService botFrameworkAdapterService,
            IAnalyticsService analyticsService,
            IUserService userService)
        {
            _accessors = accessors;
            _messagingExtensionService = messagingExtensionService;
            _databaseService = databaseService;
            _notificationSubscriptionService = notificationSubscriptionService;
            _botMessagesService = botMessagesService;
            _jiraService = jiraService;
            _actionableMessageService = actionableMessageService;
            _appSettings = appSettings.Value;
            _jiraAuthService = jiraAuthService;
            _logger = logger;
            _telemetry = telemetry;
            _userTokenService = userTokenService;
            _commandDialogReferenceService = commandDialogReferenceService;
            _dialogs = new DialogSet(accessors.ConversationDialogState);
            _botFrameworkAdapterService = botFrameworkAdapterService;
            _analyticsService = analyticsService;
            _userService = userService;

            _dialogs.Add(
                new MainDispatcher(
                    _accessors,
                    appSettings.Value,
                    jiraService,
                    _databaseService,
                    _botMessagesService,
                    _jiraAuthService,
                    _logger,
                    _telemetry,
                    _userTokenService,
                    _commandDialogReferenceService,
                    _botFrameworkAdapterService,
                    _analyticsService,
                    _notificationSubscriptionService));
        }

        public override async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default)
        {
            var user = await _userService.TryToIdentifyUser(turnContext);
            await _accessors.User.SetAsync(turnContext, user, cancellationToken);
            _eventTelemetry = new EventTelemetry
            {
                Name = turnContext.Activity.Name ?? turnContext.Activity?.Type,
                Properties =
                {
                    { "MS_Teams_User_Id", turnContext.Activity?.From?.AadObjectId },
                    { "Jira_User_Id", user?.JiraUserAccountId },
                    { "Activity_type", turnContext.Activity?.Type }
                }
            };

            await base.OnTurnAsync(turnContext, cancellationToken);

            // Client notifying this bot took to long to respond (timed out)
            if (turnContext.Activity?.Code == EndOfConversationCodes.BotTimedOut)
            {
                _logger.LogTrace(
                    "Timeout in {ChannelId} channel: Bot took too long to respond.",
                    turnContext.Activity.ChannelId);
                return;
            }

            _telemetry.TrackEvent(_eventTelemetry);
            await _accessors.ConversationState.SaveChangesAsync(turnContext, false, cancellationToken);
            await _accessors.UserState.SaveChangesAsync(turnContext, false, cancellationToken);
        }

        protected override Task OnConversationUpdateActivityAsync(
            ITurnContext<IConversationUpdateActivity> turnContext,
            CancellationToken cancellationToken)
            => _botMessagesService.HandleConversationUpdates(turnContext, cancellationToken);

        protected override async Task<TaskModuleResponse> OnTeamsTaskModuleFetchAsync(
            ITurnContext<IInvokeActivity> turnContext,
            TaskModuleRequest taskModuleRequest,
            CancellationToken cancellationToken)
        {
            await HandleInvoke(turnContext, cancellationToken);
            return new TaskModuleResponse();
        }

        protected override async Task<MessagingExtensionResponse> OnTeamsAppBasedLinkQueryAsync(
            ITurnContext<IInvokeActivity> turnContext,
            AppBasedLinkQuery query,
            CancellationToken cancellationToken)
        {
            await HandleInvoke(turnContext, cancellationToken);
            return new MessagingExtensionResponse();
        }

        protected override async Task<MessagingExtensionActionResponse> OnTeamsMessagingExtensionFetchTaskAsync(
            ITurnContext<IInvokeActivity> turnContext,
            MessagingExtensionAction action,
            CancellationToken cancellationToken)
        {
            await HandleInvoke(turnContext, cancellationToken);
            return new MessagingExtensionActionResponse();
        }

        protected override async Task<MessagingExtensionResponse> OnTeamsMessagingExtensionQueryAsync(
            ITurnContext<IInvokeActivity> turnContext,
            MessagingExtensionQuery query,
            CancellationToken cancellationToken)
        {
            await HandleInvoke(turnContext, cancellationToken);
            return new MessagingExtensionResponse();
        }

        protected override async Task<MessagingExtensionActionResponse> OnTeamsMessagingExtensionSubmitActionAsync(
            ITurnContext<IInvokeActivity> turnContext,
            MessagingExtensionAction action,
            CancellationToken cancellationToken)
        {
            return await OnTeamsMessagingExtensionFetchTaskAsync(turnContext, action, cancellationToken);
        }

        protected override async Task<TaskModuleResponse> OnTeamsTaskModuleSubmitAsync(
            ITurnContext<IInvokeActivity> turnContext,
            TaskModuleRequest taskModuleRequest,
            CancellationToken cancellationToken)
        {
            return await OnTeamsTaskModuleFetchAsync(turnContext, taskModuleRequest, cancellationToken);
        }

        protected override async Task OnTeamsO365ConnectorCardActionAsync(
            ITurnContext<IInvokeActivity> turnContext,
            O365ConnectorCardActionQuery query,
            CancellationToken cancellationToken)
        {
            await HandleInvoke(turnContext, cancellationToken);
        }

        protected override async Task<InvokeResponse> OnInvokeActivityAsync(
            ITurnContext<IInvokeActivity> turnContext,
            CancellationToken cancellationToken)
        {
            await HandleCommand(turnContext, cancellationToken);
            return await base.OnInvokeActivityAsync(turnContext, cancellationToken);
        }

        protected override async Task OnMessageActivityAsync(
            ITurnContext<IMessageActivity> turnContext,
            CancellationToken cancellationToken)
        {
            await HandleCommand(turnContext, cancellationToken);

            // Run the DialogSet - let the framework identify the current state of the dialog from
            // the dialog stack and figure out what (if any) is the active dialog.
            var dc = await _dialogs.CreateContextAsync(turnContext, cancellationToken);

            var dialogTurnResult = await dc.ContinueDialogAsync(cancellationToken);

            // Begin main dialog if no outstanding dialogs / no one responded.
            if (!dc.Context.Responded && dialogTurnResult.Status != DialogTurnStatus.Complete)
            {
                await dc.BeginDialogAsync(nameof(MainDispatcher), cancellationToken: cancellationToken);
            }
        }

        protected override async Task OnSignInInvokeAsync(
            ITurnContext<IInvokeActivity> turnContext,
            CancellationToken cancellationToken)
        {
            if (!string.IsNullOrEmpty(turnContext.Activity.Name)
                && turnContext.Activity.Name.Equals("signin/verifyState", StringComparison.OrdinalIgnoreCase))
            {
                var dc = await _dialogs.CreateContextAsync(turnContext, cancellationToken);
                if (dc.ActiveDialog != null)
                {
                    await new MainDispatcher(
                        _accessors,
                        _appSettings,
                        _jiraService,
                        _databaseService,
                        _botMessagesService,
                        _jiraAuthService,
                        _logger,
                        _telemetry,
                        _userTokenService,
                        _commandDialogReferenceService,
                        _botFrameworkAdapterService,
                        _analyticsService,
                        _notificationSubscriptionService).RunAsync(
                        turnContext,
                        _accessors.ConversationDialogState,
                        cancellationToken);
                }
                else
                {
                    var activityValue = turnContext.Activity.Value as JObject;
                    var state = activityValue?.GetValue("state")?.ToString();

                    // if the sign in dialog was closed
                    if (state == "CancelledByUser")
                    {
                        return;
                    }

                    var accessToken = await _userTokenService.GetUserTokenAsync(
                        turnContext,
                        _appSettings.OAuthConnectionName,
                        null,
                        cancellationToken: cancellationToken);
                    if (accessToken?.Token != null)
                    {
                        await _actionableMessageService.HandleSuccessfulConnection(turnContext);
                        await _botMessagesService.SendConfigureNotificationsCard(turnContext, cancellationToken);
                        _analyticsService.SendBotDialogEvent(turnContext, "connectToJira", "completed");
                    }
                }
            }
        }

        private async Task HandleInvoke(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            var user = await _accessors.User.GetAsync(turnContext, () => null, cancellationToken);
            var accessToken = await _userTokenService.GetUserTokenAsync(turnContext, cancellationToken);
            if (accessToken is null)
            {
                var link = await _userTokenService.GetSignInLink(turnContext, cancellationToken);
                link += "&width=460&height=640";

                var response =
                    MessagingExtensionHelper.BuildCardActionResponse("auth", "Sign in with Microsoft account", link);
                await BuildInvokeResponse(turnContext, HttpStatusCode.OK, response, cancellationToken);
            }
            else
            {
                if (user != null)
                {
                    user.AccessToken = accessToken.Token;
                    await _accessors.User.SetAsync(turnContext, user, cancellationToken);
                }

                await ProcessInvokeRequest(turnContext, user, cancellationToken);
            }
        }

        private async Task HandleCommand(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            var activity = turnContext.Activity;
            var value = activity?.Value as JObject;

            var command = ExtractCommandFromValue(value);
            if (command == null)
            {
                return;
            }

            // Add command to telemetry
            AddCommandToTelemetry(command);

            // Handle Cancel Command
            if (IsCancelCommand(command, activity))
            {
                await HandleCancelCommand(turnContext, activity, cancellationToken);
                return;
            }

            // Handle Comment Command
            command = HandleCommentCommand(command, value, activity, turnContext, cancellationToken);

            // Assign the command to the Activity.Text to allow dialogs to work as usual
            AssignCommandToActivityText(activity, command, turnContext);
        }

        private static string ExtractCommandFromValue(JObject value)
        {
            return value?["command"]?.ToObject<string>();
        }

        private void AddCommandToTelemetry(string command)
        {
            _eventTelemetry.Properties.Add("Activity_command", command);
        }

        private static bool IsCancelCommand(string command, Activity activity)
        {
            return string.Equals(
                       command,
                       DialogMatchesAndCommands.CancelCommand,
                       StringComparison.InvariantCultureIgnoreCase)
                   && activity.Type == ActivityTypes.Invoke;
        }

        private static async Task HandleCancelCommand(
            ITurnContext turnContext,
            Activity activity,
            CancellationToken cancellationToken)
        {
            if (activity.Type == ActivityTypes.Invoke)
            {
                await turnContext.SendActivityAsync(
                    new Activity { Value = null, Type = ActivityTypesEx.InvokeResponse }, cancellationToken);
            }
        }

        private static string HandleCommentCommand(
            string command,
            JObject value,
            Activity activity,
            ITurnContext turnContext,
            CancellationToken cancellationToken)
        {
            var regexPrefix = @"^(\s*)";
            var regex = new Regex(
                $"{regexPrefix}{DialogMatchesAndCommands.CommentDialogCommand}",
                RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);
            var match = regex.Match(command);

            if (!match.Success)
            {
                return command;
            }

            var commentText = value?["commentText"]?.ToString();
            command = $"{command} {commentText}";

            if (string.IsNullOrEmpty(commentText?.Trim()))
            {
                // If the command was revoked from ME card, send InvokeResponse
                if (activity.Type == ActivityTypes.Invoke)
                {
                    _ = turnContext.SendActivityAsync(
                        new Activity { Value = null, Type = ActivityTypesEx.InvokeResponse }, cancellationToken);
                }

                return command;
            }

            return command;
        }

        private static void AssignCommandToActivityText(Activity activity, string command, ITurnContext turnContext)
        {
            if (string.IsNullOrEmpty(activity.Text) && !string.IsNullOrEmpty(command))
            {
                turnContext.Activity.Text = command;
            }
        }

        private async Task ProcessInvokeRequest(
            ITurnContext turnContext,
            IntegratedUser user,
            CancellationToken cancellationToken)
        {
            if (turnContext.Activity.IsComposeExtensionQueryLink())
            {
                _logger.LogError("ComposeExtensionQueryLink: Start processing...");

                var isValid =
                    _messagingExtensionService.TryValidateMessagingExtensionQueryLink(
                        turnContext,
                        user,
                        out var jiraIssueIdOrKey);

                if (!isValid)
                {
                    // do nothing and let teams process request on its own
                    // in case user provides an invalid url
                    return;
                }

                _logger.LogError("ComposeExtensionQueryLink: Build card...");

                var response =
                    await _messagingExtensionService.HandleMessagingExtensionQueryLinkAsync(
                        turnContext,
                        user,
                        jiraIssueIdOrKey);
                await BuildInvokeResponse(turnContext, HttpStatusCode.OK, response, cancellationToken);

                _logger.LogError("ComposeExtensionQueryLink: End processing...");
            }
            else if (turnContext.Activity.IsComposeExtensionFetchTask())
            {
                await TryToHandleFetchTask(turnContext, user, cancellationToken);
            }
            else if (turnContext.Activity.IsBotFetchTask())
            {
                var composeExtensionQuery = SafeCast<MessagingExtensionQuery>(turnContext.Activity.Value);

                // if we have command id, try to handle bot fetch task like compose extension fetch task
                if (!string.IsNullOrEmpty(composeExtensionQuery?.CommandId))
                {
                    await TryToHandleFetchTask(turnContext, user, cancellationToken);
                }
                else
                {
                    var response = await _messagingExtensionService.HandleBotFetchTask(turnContext, user);
                    await BuildInvokeResponse(turnContext, HttpStatusCode.OK, response, cancellationToken);
                }
            }
            else if (turnContext.Activity.IsComposeExtensionSubmitAction())
            {
                var response =
                    await _messagingExtensionService.HandleMessagingExtensionSubmitActionAsync(turnContext, user);
                await BuildInvokeResponse(turnContext, HttpStatusCode.OK, response, cancellationToken);
            }
            else if (turnContext.Activity.IsTaskSubmitAction())
            {
                var response = await _messagingExtensionService.HandleTaskSubmitActionAsync(turnContext, user);
                await BuildInvokeResponse(turnContext, HttpStatusCode.OK, response, cancellationToken);
            }
            else if (turnContext.Activity.IsRequestMessagingExtensionQuery())
            {
                _telemetry.TrackPageView("MessagingExtensionQuery");

                _analyticsService.SendBotDialogEvent(turnContext, "messagingExtensionQuery", "completed");

                // we should avoid falling into this if after the previous one
                var response = await _messagingExtensionService.HandleMessagingExtensionQuery(turnContext, user);
                await BuildInvokeResponse(turnContext, HttpStatusCode.OK, response, cancellationToken);
            }

            if (turnContext.Activity.IsO365ConnectorCardActionQuery())
            {
                var isSuccess = await _actionableMessageService.HandleConnectorCardActionQuery(turnContext, user);
                if (isSuccess)
                {
                    await BuildInvokeResponse(turnContext, HttpStatusCode.OK, cancellationToken: cancellationToken);
                }
            }
        }

        private async Task TryToHandleFetchTask(
            ITurnContext turnContext,
            IntegratedUser user,
            CancellationToken cancellationToken)
        {
            var isValid =
                _messagingExtensionService.TryValidateMessageExtensionFetchTask(
                    turnContext,
                    user,
                    out var validationResponse);

            if (!isValid)
            {
                await _botMessagesService.SendConnectCard(turnContext, cancellationToken);

                // if validation response is not null - return it in response, do nothing otherwise
                // since teams ignores any other type of response except of task and auth
                if (validationResponse != null)
                {
                    await BuildInvokeResponse(turnContext, HttpStatusCode.OK, validationResponse, cancellationToken);
                }
            }
            else
            {
                var response =
                    await _messagingExtensionService.HandleMessagingExtensionFetchTask(turnContext, user);

                await BuildInvokeResponse(turnContext, HttpStatusCode.OK, response, cancellationToken);
            }
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

        private static async Task BuildInvokeResponse(
            ITurnContext turnContext,
            HttpStatusCode statusCode,
            object body = null,
            CancellationToken cancellationToken = default)
        {
            await turnContext.SendActivityAsync(
                new Activity
                {
                    Value = new InvokeResponse
                    {
                        Body = body,
                        Status = (int)statusCode
                    },
                    Type = ActivityTypesEx.InvokeResponse
                },
                cancellationToken);
        }
    }
}
