using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using MicrosoftTeamsIntegration.Artifacts.Extensions;
using MicrosoftTeamsIntegration.Jira.Models;
using MicrosoftTeamsIntegration.Jira.Models.Jira.Issue;
using MicrosoftTeamsIntegration.Jira.Services.Interfaces;
using MicrosoftTeamsIntegration.Jira.Settings;

namespace MicrosoftTeamsIntegration.Jira.Dialogs
{
    public class WatchDialog : JiraIssueDependentDialog
    {
        private readonly IJiraService _jiraService;
        private readonly IBotMessagesService _botMessagesService;
        private readonly TelemetryClient _telemetry;

        public WatchDialog(
            JiraBotAccessors accessors,
            IJiraService jiraService,
            IBotMessagesService botMessagesService,
            AppSettings appSettings,
            TelemetryClient telemetry)
            : base(nameof(WatchDialog), accessors, jiraService, appSettings)
        {
            _jiraService = jiraService;
            _botMessagesService = botMessagesService;
            _telemetry = telemetry;
        }

        protected override async Task<DialogTurnResult> ProcessJiraIssueAsync(DialogContext dc, IntegratedUser user, JiraIssue jiraIssue)
        {
            _telemetry.TrackPageView("WatchDialog");

            var issueKey = jiraIssue.Key;
            var isWatching = jiraIssue.Fields.Watches?.IsWatching == true;

            var invokedFromCard = dc.Context.Activity?.Value != null;
            var isGroupConversation = dc.Context.Activity != null && dc.Context.Activity.Conversation.IsGroup.GetValueOrDefault();
            var isMessagingExtension = dc.Context.Activity != null && dc.Context.Activity.Type == ActivityTypes.Invoke;

            if (!isWatching)
            {
                var response = await _jiraService.Watch(user, issueKey);
                if (response.IsSuccess)
                {
                    if (invokedFromCard)
                    {
                        // if action is invoked from a team chat or ME - send message to personal chat without card re-rendering
                        if (isGroupConversation || isMessagingExtension)
                        {
                            await dc.Context.SendToDirectConversationAsync($"You've started watching {issueKey}.");
                        }
                        else
                        {
                            await _botMessagesService.BuildAndUpdateJiraIssueCard(dc.Context, user, issueKey);
                        }
                    }
                    else
                    {
                        // if action is from command prompt - send reply to that chat this action was invoked from
                        await dc.Context.SendActivityAsync($"You've started watching {issueKey}.");
                    }
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
                }
            }
            else
            {
                if (invokedFromCard)
                {
                    await dc.Context.SendToDirectConversationAsync($"You are already watching {issueKey}.");
                }
                else
                {
                    await dc.Context.SendActivityAsync($"You are already watching {issueKey}.");
                }
            }

            // if command was revoked from ME card - send InvokeResponse
            if (isMessagingExtension)
            {
                await dc.Context.SendActivityAsync(new Activity { Value = null, Type = ActivityTypesEx.InvokeResponse });
            }

            return await dc.EndDialogAsync();
        }
    }
}
