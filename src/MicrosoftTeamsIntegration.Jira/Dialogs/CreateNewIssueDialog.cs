using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AdaptiveCards;
using Microsoft.ApplicationInsights;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using MicrosoftTeamsIntegration.Artifacts.Extensions;
using MicrosoftTeamsIntegration.Jira.Helpers;
using MicrosoftTeamsIntegration.Jira.Models.Bot;
using MicrosoftTeamsIntegration.Jira.Models.FetchTask;
using MicrosoftTeamsIntegration.Jira.Services.Interfaces;
using MicrosoftTeamsIntegration.Jira.Settings;

namespace MicrosoftTeamsIntegration.Jira.Dialogs
{
    public class CreateNewIssueDialog : Dialog
    {
        private readonly JiraBotAccessors _accessors;
        private readonly IJiraService _jiraService;
        private readonly AppSettings _appSettings;
        private readonly TelemetryClient _telemetry;
        private readonly IAnalyticsService _analyticsService;

        public CreateNewIssueDialog(
            JiraBotAccessors accessors,
            IJiraService jiraService,
            AppSettings appSettings,
            TelemetryClient telemetry,
            IAnalyticsService analyticsService)
            : base(nameof(CreateNewIssueDialog))
        {
            _accessors = accessors;
            _jiraService = jiraService;
            _appSettings = appSettings;
            _telemetry = telemetry;
            _analyticsService = analyticsService;
        }

        public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default)
        {
            _telemetry.TrackPageView("CreateNewIssueDialog");

            var user = await JiraBotAccessorsHelper.GetUser(_accessors, dc.Context, _appSettings, cancellationToken);
            var createIssuePermissionResponse = await _jiraService.GetMyPermissions(user, "CREATE_ISSUES", null, null);
            if (createIssuePermissionResponse != null && !createIssuePermissionResponse.Permissions.CreateIssues.HavePermission)
            {
                var errorMessage = "You don't have permissions to create issues. " +
                    "For more information contact your project administrator.";
                await dc.Context.SendActivityAsync(errorMessage, cancellationToken: cancellationToken);
                return await dc.EndDialogAsync(cancellationToken: cancellationToken);
            }

            var card = new AdaptiveCard("1.0")
            {
                Body = new List<AdaptiveElement>
                {
                    new AdaptiveTextBlock
                    {
                        Text = "Click the button to create new story, bug or task. Please note that some issue types (e.g. sub-task) are not available via bot.",
                        Wrap = true
                    }
                },

                Actions = new List<AdaptiveAction>
                {
                    new AdaptiveSubmitAction
                    {
                        Title = "Create",
                        Data = new JiraBotTeamsDataWrapper
                        {
                            FetchTaskData = new FetchTaskBotCommand(DialogMatchesAndCommands.CreateNewIssueDialogCommand),
                            TeamsData = new TeamsData
                            {
                                Type = "task/fetch"
                            }
                        }
                    }
                }
            };

            await dc.Context.SendActivityAsync(MessageFactory.Attachment(card.ToAttachment()), cancellationToken);

            _analyticsService.SendBotDialogEvent(dc.Context, "createIssueDialog", "completed");

            return await dc.EndDialogAsync(cancellationToken: cancellationToken);
        }
    }
}
