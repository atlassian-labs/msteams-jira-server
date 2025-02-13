using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using MicrosoftTeamsIntegration.Artifacts.Extensions;
using MicrosoftTeamsIntegration.Jira.Models;
using MicrosoftTeamsIntegration.Jira.Models.Jira.Issue;
using MicrosoftTeamsIntegration.Jira.Services;
using MicrosoftTeamsIntegration.Jira.Services.Interfaces;
using MicrosoftTeamsIntegration.Jira.Settings;

namespace MicrosoftTeamsIntegration.Jira.Dialogs
{
    public class UnwatchDialog : JiraIssueDependentDialog
    {
        private readonly IJiraService _jiraService;
        private readonly IBotMessagesService _botMessagesService;
        private readonly TelemetryClient _telemetry;
        private readonly IAnalyticsService _analyticsService;

        public UnwatchDialog(
            JiraBotAccessors accessors,
            IJiraService jiraService,
            IBotMessagesService botMessagesService,
            AppSettings appSettings,
            TelemetryClient telemetry,
            IAnalyticsService analyticsService)
            : base(nameof(UnwatchDialog), accessors, jiraService, appSettings)
        {
            _jiraService = jiraService;
            _botMessagesService = botMessagesService;
            _telemetry = telemetry;
            _analyticsService = analyticsService;
        }

        protected override async Task<DialogTurnResult> ProcessJiraIssueAsync(DialogContext dc, IntegratedUser user, JiraIssue jiraIssue)
        {
            _telemetry.TrackPageView("UnwatchDialog");

            var issueKey = jiraIssue.Key;
            var isWatching = jiraIssue.Fields.Watches?.IsWatching == true;
            var invokedFromCard = dc.Context.Activity?.Value != null;

            var isGroupConversation = dc.Context.Activity != null && dc.Context.Activity.Conversation.IsGroup.GetValueOrDefault();
            var isMessagingExtension = dc.Context.Activity != null && dc.Context.Activity.Type == ActivityTypes.Invoke;

            if (isWatching)
            {
                var response = await _jiraService.Unwatch(user, issueKey);
                if (response.IsSuccess)
                {
                    // if command is sent by clicking the button - re-render adaptive card
                    if (invokedFromCard)
                    {
                        if (isGroupConversation || isMessagingExtension)
                        {
                            await dc.Context.SendToDirectConversationAsync($"You've stopped watching {issueKey}.");
                        }
                        else
                        {
                            await _botMessagesService.BuildAndUpdateJiraIssueCard(dc.Context, user, issueKey);
                        }
                    }
                    else
                    {
                        // if action is from command prompt - send reply to that chat this action was invoked from
                        await dc.Context.SendActivityAsync($"You've stopped watching {issueKey}");
                    }

                    _analyticsService.SendBotDialogEvent(dc.Context, "unwatch", "completed");
                }
                else
                {
                    if (invokedFromCard)
                    {
                        await dc.Context.SendToDirectConversationAsync(response.ErrorMessage);
                    }
                    else
                    {
                        await dc.Context.SendActivityAsync(response.ErrorMessage);
                    }

                    _analyticsService.SendBotDialogEvent(dc.Context, "unwatch", "failed", response.ErrorMessage);
                }
            }
            else
            {
                await dc.Context.SendActivityAsync("Looks like you weren't watching this issue, please check if it is the right issue key.");
                _analyticsService.SendBotDialogEvent(dc.Context, "unwatch", "completed");
            }

            if (isMessagingExtension)
            {
                await dc.Context.SendActivityAsync(new Activity { Value = null, Type = ActivityTypesEx.InvokeResponse });
            }

            return await dc.EndDialogAsync();
        }
    }
}
