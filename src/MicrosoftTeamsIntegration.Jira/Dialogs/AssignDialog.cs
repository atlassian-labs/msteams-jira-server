using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using MicrosoftTeamsIntegration.Artifacts.Extensions;
using MicrosoftTeamsIntegration.Jira.Extensions;
using MicrosoftTeamsIntegration.Jira.Helpers;
using MicrosoftTeamsIntegration.Jira.Models;
using MicrosoftTeamsIntegration.Jira.Models.Bot;
using MicrosoftTeamsIntegration.Jira.Models.Jira.Issue;
using MicrosoftTeamsIntegration.Jira.Services;
using MicrosoftTeamsIntegration.Jira.Services.Interfaces;
using MicrosoftTeamsIntegration.Jira.Settings;

namespace MicrosoftTeamsIntegration.Jira.Dialogs
{
    public class AssignDialog : JiraIssueDependentDialog
    {
        private const string AssignJiraIssueWaterfall = "assignJiraIssueWaterfall";

        private readonly JiraBotAccessors _accessors;
        private readonly IJiraService _jiraService;
        private readonly AppSettings _appSettings;
        private readonly IBotMessagesService _botMessagesService;
        private readonly TelemetryClient _telemetry;
        private readonly IAnalyticsService _analyticsService;

        public AssignDialog(
            JiraBotAccessors accessors,
            IJiraService jiraService,
            AppSettings appSettings,
            IBotMessagesService botMessagesService,
            TelemetryClient telemetry,
            IAnalyticsService analyticsService)
            : base(nameof(AssignDialog), accessors, jiraService, appSettings)
        {
            _accessors = accessors;
            _jiraService = jiraService;
            _appSettings = appSettings;
            _botMessagesService = botMessagesService;
            _telemetry = telemetry;
            _analyticsService = analyticsService;

            var waterfallSteps = new WaterfallStep[]
            {
                OnStartAssigningProcessingAsync
            };
            AddDialog(new WaterfallDialog(AssignJiraIssueWaterfall, waterfallSteps));
        }

        protected override async Task<DialogTurnResult> ProcessJiraIssueAsync(
            DialogContext dc,
            IntegratedUser user,
            JiraIssue jiraIssue)
        {
            return await dc.ReplaceDialogAsync(AssignJiraIssueWaterfall);
        }

        private async Task<DialogTurnResult> OnStartAssigningProcessingAsync(
            WaterfallStepContext stepContext,
            CancellationToken cancellationToken = default)
        {
            _telemetry.TrackPageView("AssignDialog");

            var jiraIssueState =
                await _accessors.JiraIssueState.GetAsync(
                    stepContext.Context,
                    () => new JiraIssueState(),
                    cancellationToken);

            var jiraIssue = jiraIssueState.JiraIssue;

            return await AssignJiraIssueToUser(stepContext, CurrentUser, jiraIssue);
        }

        private async Task<DialogTurnResult> AssignJiraIssueToUser(
            DialogContext dc,
            IntegratedUser user,
            JiraIssue jiraIssue)
        {
            CurrentUser = await JiraBotAccessorsHelper.GetUser(_accessors, dc.Context, _appSettings);

            var issueKey = jiraIssue.Key;
            var invokedFromCard = dc.Context.Activity?.Value != null;

            var userNameOrAccountId = await _jiraService.GetUserNameOrAccountId(user);

            if (!jiraIssue.IsAssignedToUser(userNameOrAccountId))
            {
                await HandleIssueAssignment(dc, user, jiraIssue, userNameOrAccountId, invokedFromCard);
            }
            else
            {
                await HandleAlreadyAssignedIssue(dc, user, issueKey, invokedFromCard);
            }

            return await dc.EndDialogAsync();
        }

        private async Task HandleIssueAssignment(
            DialogContext dc,
            IntegratedUser user,
            JiraIssue jiraIssue,
            string userNameOrAccountId,
            bool invokedFromCard)
        {
            var issueKey = jiraIssue.Key;
            var response = await _jiraService.Assign(CurrentUser, issueKey, userNameOrAccountId);

            if (response.IsSuccess)
            {
                await HandleSuccessfulAssignment(dc, user, issueKey, invokedFromCard);
            }
            else
            {
                await HandleFailedAssignment(dc, response.ErrorMessage, invokedFromCard);
            }
        }

        private async Task HandleSuccessfulAssignment(
            DialogContext dc,
            IntegratedUser user,
            string issueKey,
            bool invokedFromCard)
        {
            if (CurrentUser.MsTeamsUserId == user.MsTeamsUserId)
            {
                if (invokedFromCard)
                {
                    await _botMessagesService.BuildAndUpdateJiraIssueCard(dc.Context, user, issueKey);
                }
                else
                {
                    await dc.Context.SendActivityAsync($"You have been assigned {issueKey}.");
                }
            }
            else
            {
                await dc.Context.SendActivityAsync($"{issueKey} has been assigned.");
            }

            _analyticsService.SendBotDialogEvent(dc.Context, "assign", "completed");
        }

        private async Task HandleFailedAssignment(DialogContext dc, string errorMessage, bool invokedFromCard)
        {
            if (invokedFromCard)
            {
                await dc.Context.SendToDirectConversationAsync(errorMessage);
            }
            else
            {
                await dc.Context.SendActivityAsync(errorMessage);
            }

            _analyticsService.SendBotDialogEvent(dc.Context, "assign", "failed", errorMessage);
        }

        private async Task HandleAlreadyAssignedIssue(
            DialogContext dc,
            IntegratedUser user,
            string issueKey,
            bool invokedFromCard)
        {
            var replyText = CurrentUser.MsTeamsUserId == user.MsTeamsUserId
                ? $"You're already assigned to {issueKey}."
                : $"{issueKey} is already assigned.";

            if (invokedFromCard)
            {
                await dc.Context.SendToDirectConversationAsync(replyText);
            }
            else
            {
                await dc.Context.SendActivityAsync(replyText);
            }
        }
    }
}
