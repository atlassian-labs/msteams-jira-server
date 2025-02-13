using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using MicrosoftTeamsIntegration.Jira.Services.Interfaces;
using MicrosoftTeamsIntegration.Jira.Settings;

namespace MicrosoftTeamsIntegration.Jira.Dialogs
{
    public class ConnectToJiraDialog : ComponentDialog
    {
        private const string MainWaterfall = "connectToJiraWaterfall";

        private readonly AppSettings _appSettings;
        private readonly IBotMessagesService _botMessagesService;
        private readonly TelemetryClient _telemetry;
        private readonly IBotFrameworkAdapterService _botFrameworkAdapterService;

        public ConnectToJiraDialog(
            JiraBotAccessors accessors,
            AppSettings appSettings,
            IBotMessagesService botMessagesService,
            TelemetryClient telemetry,
            IBotFrameworkAdapterService botFrameworkAdapterService)
            : base(nameof(ConnectToJiraDialog))
        {
            _appSettings = appSettings;
            _botMessagesService = botMessagesService;
            _telemetry = telemetry;
            _botFrameworkAdapterService = botFrameworkAdapterService;

            var waterfallSteps = new WaterfallStep[]
            {
                OnSigninMsAccountAsync,
                OnSendJiraAuthCardAsync
            };

            AddDialog(new OAuthPrompt(
                nameof(OAuthPrompt),
                new OAuthPromptSettings
                {
                    ConnectionName = appSettings.OAuthConnectionName,
                    Text = "Sign in with Microsoft account",
                    Title = "Sign In",
                    Timeout = 300000, // User has 5 minutes to login (1000 * 60 * 5)
                }));
            AddDialog(new WaterfallDialog(MainWaterfall, waterfallSteps));
        }

        protected override async Task<DialogTurnResult> OnBeginDialogAsync(DialogContext innerDc, object options, CancellationToken cancellationToken = default)
        {
            _telemetry.TrackPageView("ConnectToJiraDialog");

            await _botFrameworkAdapterService.SignOutUserAsync(innerDc.Context, _appSettings.OAuthConnectionName, cancellationToken);

            return await innerDc.BeginDialogAsync(MainWaterfall, cancellationToken: cancellationToken);
        }

        private async Task<DialogTurnResult> OnSigninMsAccountAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.BeginDialogAsync(nameof(OAuthPrompt), cancellationToken: cancellationToken);
        }

        private async Task<DialogTurnResult> OnSendJiraAuthCardAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            _telemetry.TrackPageView("ConnectToJiraDialog::SendJiraAuthCard");

            await _botMessagesService.SendAuthorizationCard(stepContext.Context, null, cancellationToken);
            return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
        }
    }
}
