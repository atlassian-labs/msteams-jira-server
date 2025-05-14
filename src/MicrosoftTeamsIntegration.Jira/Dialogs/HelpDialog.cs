using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using MicrosoftTeamsIntegration.Artifacts.Extensions;
using MicrosoftTeamsIntegration.Jira.Services.Interfaces;
using MicrosoftTeamsIntegration.Jira.Settings;

namespace MicrosoftTeamsIntegration.Jira.Dialogs
{
    public class HelpDialog : Dialog
    {
        private readonly TelemetryClient _telemetry;
        private readonly IAnalyticsService _analyticsService;
        private readonly IBotMessagesService _botMessagesService;

        public HelpDialog(
            JiraBotAccessors accessors,
            AppSettings appSettings,
            TelemetryClient telemetry,
            IAnalyticsService analyticsService,
            IBotMessagesService botMessagesService)
            : base(nameof(HelpDialog))
        {
            _telemetry = telemetry;
            _analyticsService = analyticsService;
            _botMessagesService = botMessagesService;
        }

        public override async Task<DialogTurnResult> BeginDialogAsync(
            DialogContext dc,
            object options = null,
            CancellationToken cancellationToken = default)
        {
            _telemetry.TrackPageView("HelpDialog");

            var card = _botMessagesService.BuildHelpCard(dc.Context);
            var message = MessageFactory.Attachment(card.ToAttachment());

            await dc.Context.SendActivityAsync(message, cancellationToken: cancellationToken);
            _analyticsService.SendBotDialogEvent(dc.Context, "help", "completed");
            return await dc.EndDialogAsync(cancellationToken: cancellationToken);
        }
    }
}
