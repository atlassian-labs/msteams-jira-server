using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using MicrosoftTeamsIntegration.Artifacts.Extensions;
using MicrosoftTeamsIntegration.Jira.Helpers;
using MicrosoftTeamsIntegration.Jira.Services.Interfaces;
using MicrosoftTeamsIntegration.Jira.Settings;

namespace MicrosoftTeamsIntegration.Jira.Dialogs
{
    public class IssueByKeyDialog : Dialog
    {
        private readonly JiraBotAccessors _accessors;
        private readonly IBotMessagesService _botMessagesService;
        private readonly AppSettings _appSettings;
        private readonly TelemetryClient _telemetry;

        public IssueByKeyDialog(
            JiraBotAccessors accessors,
            IBotMessagesService botMessagesService,
            AppSettings appSettings,
            TelemetryClient telemetry)
            : base(nameof(IssueByKeyDialog))
        {
            _accessors = accessors;
            _botMessagesService = botMessagesService;
            _appSettings = appSettings;
            _telemetry = telemetry;
        }

        public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default)
        {
            _telemetry.TrackPageView("IssueByKeyDialog");

            var user = await JiraBotAccessorsHelper.GetUser(_accessors, dc.Context, _appSettings, cancellationToken);
            var text = dc.Context.Activity.RemoveRecipientMention();

            // if user was not connected, do nothing
            if (user == null)
            {
                return await dc.EndDialogAsync(cancellationToken: cancellationToken);
            }

            if (!text.TryGetJiraKeyIssue(out var jiraIssueKey))
            {
                return await dc.EndDialogAsync(
                    null,
                    cancellationToken);
            }

            var card = await _botMessagesService.SearchIssueAndBuildIssueCard(dc.Context, user, jiraIssueKey);

            if (card != null)
            {
                var message = MessageFactory.Attachment(card.ToAttachment());
                await dc.Context.SendActivityAsync(message, cancellationToken);
                return await dc.EndDialogAsync(cancellationToken: cancellationToken);
            }

            await dc.Context.SendActivityAsync("I couldn't find an issue.", cancellationToken: cancellationToken);
            return await dc.EndDialogAsync(cancellationToken: cancellationToken);
        }
    }
}
