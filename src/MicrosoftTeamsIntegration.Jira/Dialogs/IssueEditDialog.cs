using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.Bot.Builder.Dialogs;
using MicrosoftTeamsIntegration.Jira.Models;
using MicrosoftTeamsIntegration.Jira.Models.Jira.Issue;
using MicrosoftTeamsIntegration.Jira.Services.Interfaces;
using MicrosoftTeamsIntegration.Jira.Settings;

namespace MicrosoftTeamsIntegration.Jira.Dialogs
{
    public class IssueEditDialog : JiraIssueDependentDialog
    {
        private readonly TelemetryClient _telemetry;
        private readonly IAnalyticsService _analyticsService;

        public IssueEditDialog(
            JiraBotAccessors accessors,
            IJiraService jiraService,
            AppSettings appSettings,
            TelemetryClient telemetry,
            IAnalyticsService analyticsService)
            : base(nameof(IssueEditDialog), accessors, jiraService, appSettings)
        {
            _telemetry = telemetry;
            _analyticsService = analyticsService;
        }

        protected override async Task<DialogTurnResult> ProcessJiraIssueAsync(DialogContext dc, IntegratedUser user, JiraIssue jiraIssue)
        {
            _telemetry.TrackPageView("IssueEditDialog");

            _analyticsService.SendBotDialogEvent(dc.Context, "editIssue", "completed");

            // Continue with IssueByKeyDialog
            return await dc.ReplaceDialogAsync(nameof(IssueByKeyDialog));
        }
    }
}
