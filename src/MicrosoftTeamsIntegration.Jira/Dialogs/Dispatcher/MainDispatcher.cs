using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using MicrosoftTeamsIntegration.Artifacts.Extensions;
using MicrosoftTeamsIntegration.Jira.Exceptions;
using MicrosoftTeamsIntegration.Jira.Extensions;
using MicrosoftTeamsIntegration.Jira.Helpers;
using MicrosoftTeamsIntegration.Jira.Models;
using MicrosoftTeamsIntegration.Jira.Models.Bot;
using MicrosoftTeamsIntegration.Jira.Services.Interfaces;
using MicrosoftTeamsIntegration.Jira.Settings;

namespace MicrosoftTeamsIntegration.Jira.Dialogs.Dispatcher
{
    public class MainDispatcher : ComponentDialog
    {
        private readonly JiraBotAccessors _accessors;
        private readonly AppSettings _appSettings;
        private readonly IBotMessagesService _botMessagesService;
        private readonly IJiraAuthService _jiraAuthService;
        private readonly ILogger<JiraBot> _logger;
        private readonly List<JiraActionRegexReference> _actionCommands = new List<JiraActionRegexReference>();
        private readonly TelemetryClient _telemetry;
        private readonly IUserTokenService _userTokenService;
        private readonly ICommandDialogReferenceService _commandDialogReferenceService;
        private readonly IBotFrameworkAdapterService _botFrameworkAdapter;

        public MainDispatcher(
            JiraBotAccessors accessors,
            AppSettings appSettings,
            IJiraService jiraService,
            IDatabaseService databaseService,
            IBotMessagesService botMessagesService,
            IJiraAuthService jiraAuthService,
            ILogger<JiraBot> logger,
            TelemetryClient telemetry,
            IUserTokenService userTokenService,
            ICommandDialogReferenceService commandDialogReferenceService,
            IBotFrameworkAdapterService botFrameworkAdapter)
            : base(nameof(MainDispatcher))
        {
            _accessors = accessors;
            _appSettings = appSettings;
            _botMessagesService = botMessagesService;
            _jiraAuthService = jiraAuthService;
            _logger = logger;
            _telemetry = telemetry;
            _userTokenService = userTokenService;
            _commandDialogReferenceService = commandDialogReferenceService;
            _botFrameworkAdapter = botFrameworkAdapter;

            // Add dialogs
            AddDialog(new HelpDialog(_accessors, _appSettings, _telemetry));
            AddDialog(new IssueByKeyDialog(_accessors, botMessagesService, _appSettings, _telemetry));
            AddDialog(new WatchDialog(_accessors, jiraService, botMessagesService, _appSettings, _telemetry));
            AddDialog(new UnwatchDialog(_accessors, jiraService, botMessagesService, _appSettings, _telemetry));
            AddDialog(new IssueEditDialog(_accessors, jiraService, _appSettings, _telemetry));
            AddDialog(new FindDialog(_accessors, jiraService, _appSettings, _telemetry));
            AddDialog(new VoteDialog(_accessors, jiraService, _appSettings, _telemetry));
            AddDialog(new UnvoteDialog(_accessors, jiraService, _appSettings, _telemetry));
            AddDialog(new CreateNewIssueDialog(_accessors, jiraService, _appSettings, _telemetry));
            AddDialog(new LogTimeDialog(_accessors, jiraService, _appSettings, _telemetry));
            AddDialog(new CommentDialog(_accessors, jiraService, _appSettings, _telemetry));
            AddDialog(new AssignDialog(_accessors, jiraService, _appSettings, databaseService, botMessagesService, _telemetry));
            AddDialog(new ConnectToJiraDialog(_accessors, _appSettings, botMessagesService, _telemetry, botFrameworkAdapter));
            AddDialog(new DisconnectJiraDialog(_accessors, jiraAuthService, _appSettings, _telemetry));
            AddDialog(new SignoutMsAccountDialog(_accessors, appSettings, _telemetry, botFrameworkAdapter));
        }

        protected override async Task<DialogTurnResult> OnBeginDialogAsync(DialogContext innerDc, object options, CancellationToken cancellationToken = default)
        {
            try
            {
                return await MainDispatchAsync(innerDc, cancellationToken);
            }
            catch (UnauthorizedException)
            {
                return await BuildConnectCard(innerDc);
            }
            catch (ForbiddenException ex)
            {
                await innerDc.Context.SendActivityAsync(ex.Message, cancellationToken: cancellationToken);
                return await innerDc.EndDialogAsync(cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                await innerDc.Context.SendActivityAsync(BotMessages.SomethingWentWrong, cancellationToken: cancellationToken);
                return await innerDc.EndDialogAsync(cancellationToken: cancellationToken);
            }
        }

        protected override async Task<DialogTurnResult> OnContinueDialogAsync(DialogContext innerDc, CancellationToken cancellationToken = default)
        {
            try
            {
                return await MainDispatchAsync(innerDc, cancellationToken);
            }
            catch (UnauthorizedException)
            {
                return await BuildConnectCard(innerDc);
            }
            catch (ForbiddenException ex)
            {
                await innerDc.Context.SendActivityAsync(ex.Message, cancellationToken: cancellationToken);
                return await innerDc.EndDialogAsync(cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                await innerDc.Context.SendActivityAsync(BotMessages.SomethingWentWrong, cancellationToken: cancellationToken);
                return await innerDc.EndDialogAsync(cancellationToken: cancellationToken);
            }
        }

        // This method examines the incoming turn property to determine:
        // 1. If the requested operation is permissible - e.g. if user is in middle of a dialog,
        //     then an out of order reply should not be allowed.
        // 2. Calls any outstanding dialogs to continue.
        // 3. If results is no-match from outstanding dialog .OR. if there are no outstanding dialogs,
        //    decide which child dialog should begin and start it.
        protected async Task<DialogTurnResult> MainDispatchAsync(DialogContext innerDc, CancellationToken cancellationToken)
        {
            var context = innerDc.Context;
            var (allowed, reason) = IsRequestedOperationPossible(innerDc);
            if (!allowed)
            {
                await context.SendActivityAsync(reason, cancellationToken: cancellationToken);

                // clear jira issue state
                await _accessors.JiraIssueState.SetAsync(innerDc.Context, new JiraIssueState(), cancellationToken);

                // Nothing to do here. End main dialog.
                var user = await _accessors.User.GetAsync(context, () => new IntegratedUser());
                return await innerDc.EndDialogAsync(cancellationToken: cancellationToken);
            }

            // Continue outstanding dialogs.
            var dialogTurnResult = await innerDc.ContinueDialogAsync(cancellationToken);

            // This will only be empty if there is no active dialog in the stack.
            // Removing check for dialogTurnStatus here will break successful cancellation of child dialogs.
            if (!context.Responded && dialogTurnResult != null && dialogTurnResult.Status != DialogTurnStatus.Complete)
            {
                // No one has responded so start the right child dialog.
                dialogTurnResult = await BeginChildDialogAsync(innerDc, cancellationToken);
            }

            if (dialogTurnResult == null)
            {
                return await innerDc.EndDialogAsync(cancellationToken: cancellationToken);
            }

            // Examine result from dc.continue() or from the call to beginChildDialog().
            switch (dialogTurnResult.Status)
            {
                case DialogTurnStatus.Complete:
                    // The active dialog finished successfully.
                    await innerDc.EndDialogAsync(cancellationToken: cancellationToken);
                    break;

                case DialogTurnStatus.Waiting:
                    // The active dialog is waiting for a response from the user, so do nothing
                    break;

                case DialogTurnStatus.Cancelled:
                    // The active dialog's stack has been canceled
                    await innerDc.CancelAllDialogsAsync(cancellationToken: cancellationToken);
                    break;
            }

            return dialogTurnResult;
        }

        protected async Task<DialogTurnResult> BeginChildDialogAsync(DialogContext dc, CancellationToken cancellationToken)
        {
            var activity = dc.Context.Activity;

            // determine whether we have text/html content from user, such as ME message and any html
            if (activity.IsHtmlMessage())
            {
                // parse card html to find issue key
                activity.Text = _botMessagesService.HandleHtmlMessageFromUser(activity);
                return await dc.BeginDialogAsync(nameof(IssueByKeyDialog), cancellationToken: cancellationToken);
            }

            var activityText = activity.RemoveRecipientMention();

            // if we received empty (null) message - end this dialog
            // we can receive null when adaptive card is sent from ME in personal chat
            if (activityText == null)
            {
                return await dc.EndDialogAsync();
            }

            // run following dialog if request is a valid jira issue url
            if (activityText.IsJiraIssueUrl())
            {
                return await dc.BeginDialogAsync(nameof(IssueByKeyDialog), cancellationToken: cancellationToken);
            }

            var isAuth = await IsJiraConnected(dc, cancellationToken);

            if (isAuth)
            {
                var user = await _accessors.User.GetAsync(dc.Context, () => new IntegratedUser());
                await _accessors.User.SetAsync(dc.Context, user);
            }

            return await RunCommandAsync(activityText, isAuth, dc);
        }

        private async Task<DialogTurnResult> RunCommandAsync(string activityText, bool isAuth, DialogContext dc)
        {
            var actionReference = _commandDialogReferenceService.GetActionReference(activityText);
            if (actionReference != null)
            {
                if (dc.Context.Activity.IsGroupConversation() && !actionReference.IsTeamAction)
                {
                    var commandName = !string.IsNullOrEmpty(actionReference.CommandName) ? actionReference.CommandName : "this";
                    await dc.Context.SendActivityAsync(
                        $"Sorry, the {commandName} command  is available in personal chat only.");
                    return await dc.EndDialogAsync();
                }

                if (!isAuth && actionReference.RequireAuthentication)
                {
                    return await BuildConnectCard(dc);
                }

                return await dc.BeginDialogAsync(actionReference.DialogName);
            }

            await dc.Context.SendActivityAsync(
                $"Sorry, I didn't understand '{activityText.Trim()}'. Type help to explore commands.");
            return await dc.EndDialogAsync();
        }

        private async Task<bool> IsJiraConnected(DialogContext dc, CancellationToken cancellationToken)
        {
            var user = await _accessors.User.GetAsync(dc.Context, () => new IntegratedUser(), cancellationToken);
            var token = await _userTokenService.GetUserTokenAsync(dc.Context, _appSettings.OAuthConnectionName, null, cancellationToken);

            return await _jiraAuthService.IsJiraConnected(user) && !string.IsNullOrEmpty(token?.Token);
        }

        // Method to evaluate if the requested user operation is possible.
        private static (bool Allowed, string Reason) IsRequestedOperationPossible(DialogContext dc)
        {
            (bool Allowed, string Reason) outcome = (true, string.Empty);

            var cancelCommandRegexp = new Regex(JiraConstants.CancelCommandRegex, RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);

            if (cancelCommandRegexp.IsMatch(dc.Context.Activity.RemoveRecipientMention() ?? string.Empty))
            {
                outcome.Allowed = false;
                outcome.Reason = BotMessages.OperationCancelled;

                return outcome;
            }

            return outcome;
        }

        private async Task<DialogTurnResult> BuildConnectCard(DialogContext dc)
        {
            var attachments = new List<Attachment>
            {
                new HeroCard(BotMessages.ConnectDialogCardTitle)
                {
                    Buttons = new List<CardAction>
                    {
                        new CardAction(ActionTypes.ImBack, "Connect", value: "connect")
                    }
                }.ToAttachment()
            };

            var message = Activity.CreateMessageActivity();
            message.Attachments = attachments;

            await dc.Context.SendToDirectConversationAsync(message);

            // if command was revoked from ME card - send InvokeResponse
            if (dc.Context.Activity.Type == ActivityTypes.Invoke)
            {
                await dc.Context.SendActivityAsync(new Activity { Value = null, Type = ActivityTypesEx.InvokeResponse });
            }

            return await dc.EndDialogAsync();
        }
    }
}
