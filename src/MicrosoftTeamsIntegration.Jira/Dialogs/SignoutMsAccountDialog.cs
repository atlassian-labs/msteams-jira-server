using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using MicrosoftTeamsIntegration.Jira.Services.Interfaces;
using MicrosoftTeamsIntegration.Jira.Settings;

namespace MicrosoftTeamsIntegration.Jira.Dialogs
{
    public class SignoutMsAccountDialog : ComponentDialog
    {
        private readonly JiraBotAccessors _accessors;
        private readonly AppSettings _appSettings;
        private readonly TelemetryClient _telemetry;
        private readonly IBotFrameworkAdapterService _botFrameworkAdapterService;

        public SignoutMsAccountDialog(
            JiraBotAccessors accessors,
            AppSettings appSettings,
            TelemetryClient telemetry,
            IBotFrameworkAdapterService botFrameworkAdapterService)
            : base(nameof(SignoutMsAccountDialog))
        {
            _accessors = accessors;
            _appSettings = appSettings;
            _telemetry = telemetry;
            _botFrameworkAdapterService = botFrameworkAdapterService;
        }

        protected override async Task<DialogTurnResult> OnBeginDialogAsync(DialogContext innerDc, object options, CancellationToken cancellationToken = default)
        {
            _telemetry.TrackPageView("SignOutMSAccountDialog");

            // The bot adapter encapsulates the authentication processes.
            await _botFrameworkAdapterService.SignOutUserAsync(innerDc.Context, _appSettings.OAuthConnectionName, null, cancellationToken);
            await innerDc.Context.SendActivityAsync(MessageFactory.Text("You have been signed out."), cancellationToken);
            return await innerDc.CancelAllDialogsAsync(cancellationToken);
        }
    }
}
