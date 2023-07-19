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
        private readonly AppSettings _appSettings;
        private readonly TelemetryClient _telemetry;

        public UnvoteDialog(
            JiraBotAccessors accessors,
            IJiraService jiraService,
            AppSettings appSettings,
            TelemetryClient telemetry)
            : base(nameof(UnvoteDialog), accessors, jiraService, appSettings)
        {
            _jiraService = jiraService;
            _appSettings = appSettings;
            _telemetry = telemetry;
        }

        protected override async Task<DialogTurnResult> ProcessJiraIssueAsync(DialogContext stepContext, IntegratedUser user, JiraIssue jiraIssue)
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
                        await stepContext.Context.SendActivityAsync("Your vote has been removed.");
                    }
                    else
                    {
                        await stepContext.Context.SendActivityAsync(response.ErrorMessage);
                    }
                }
                else
                {
                    await stepContext.Context.SendActivityAsync($"You have not voted for the issue {issueKey} yet.");
                }
            }
            else
            {
                if (jiraIssue.IsResolved())
                {
                    await stepContext.Context.SendActivityAsync("You cannot unvote for a resolved issue.");
                }

                if (jiraIssue.IsUserReporter(userNameOrAccountId))
                {
                    await stepContext.Context.SendActivityAsync("You cannot unvote for an issue you have reported.");
                }
            }

            return await stepContext.EndDialogAsync();
        }
    }
}
