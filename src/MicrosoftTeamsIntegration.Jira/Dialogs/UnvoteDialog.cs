using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.Bot.Builder.Dialogs;
using MicrosoftTeamsIntegration.Jira.Extensions;
using MicrosoftTeamsIntegration.Jira.Models;
using MicrosoftTeamsIntegration.Jira.Models.Jira.Issue;
using MicrosoftTeamsIntegration.Jira.Services.Interfaces;
using MicrosoftTeamsIntegration.Jira.Settings;

namespace MicrosoftTeamsIntegration.Jira.Dialogs
{
    public class UnvoteDialog : JiraIssueDependentDialog
    {
        private readonly IJiraService _jiraService;
        private readonly TelemetryClient _telemetry;
        private readonly IAnalyticsService _analyticsService;

        public UnvoteDialog(
            JiraBotAccessors accessors,
            IJiraService jiraService,
            AppSettings appSettings,
            TelemetryClient telemetry,
            IAnalyticsService analyticsService)
            : base(nameof(UnvoteDialog), accessors, jiraService, appSettings)
        {
            _jiraService = jiraService;
            _telemetry = telemetry;
            _analyticsService = analyticsService;
        }

        protected override async Task<DialogTurnResult> ProcessJiraIssueAsync(DialogContext dc, IntegratedUser user, JiraIssue jiraIssue)
        {
            _telemetry.TrackPageView("UnvoteDialog");

            var issueKey = jiraIssue.Key;
            var userNameOrAccountId = await _jiraService.GetUserNameOrAccountId(user);

            if (jiraIssue.AllowsToVote(userNameOrAccountId))
            {
                if (jiraIssue.IsVotedByUser())
                {
                    var response = await _jiraService.Unvote(CurrentUser, issueKey);
                    if (response.IsSuccess)
                    {
                        await dc.Context.SendActivityAsync("Your vote has been removed.");
                        _analyticsService.SendBotDialogEvent(dc.Context, "unvote", "completed");
                    }
                    else
                    {
                        await dc.Context.SendActivityAsync(response.ErrorMessage);
                        _analyticsService.SendBotDialogEvent(dc.Context, "unvote", "failed", response.ErrorMessage);
                    }
                }
                else
                {
                    await dc.Context.SendActivityAsync($"You have not voted for the issue {issueKey} yet.");
                    _analyticsService.SendBotDialogEvent(dc.Context, "unvote", "completed");
                }
            }
            else
            {
                if (jiraIssue.IsResolved())
                {
                    await dc.Context.SendActivityAsync("You cannot unvote for a resolved issue.");
                }

                if (jiraIssue.IsUserReporter(userNameOrAccountId))
                {
                    await dc.Context.SendActivityAsync("You cannot unvote for an issue you have reported.");
                }

                _analyticsService.SendBotDialogEvent(dc.Context, "unvote", "completed");
            }

            return await dc.EndDialogAsync();
        }
    }
}
