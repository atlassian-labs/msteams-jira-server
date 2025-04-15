using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.Bot.Builder.Dialogs;
using MicrosoftTeamsIntegration.Jira.Helpers;
using MicrosoftTeamsIntegration.Jira.Services.Interfaces;
using MicrosoftTeamsIntegration.Jira.Settings;

namespace MicrosoftTeamsIntegration.Jira.Dialogs;

public class NotificationsDialog : Dialog
{
    private readonly JiraBotAccessors _accessors;
    private readonly IBotMessagesService _botMessagesService;
    private readonly AppSettings _appSettings;
    private readonly TelemetryClient _telemetry;

    public NotificationsDialog(
        JiraBotAccessors accessors,
        IBotMessagesService botMessagesService,
        AppSettings appSettings,
        TelemetryClient telemetry)
        : base(nameof(NotificationsDialog))
    {
        _accessors = accessors;
        _botMessagesService = botMessagesService;
        _appSettings = appSettings;
        _telemetry = telemetry;
    }

    public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default)
    {
        _telemetry.TrackPageView("NotificationsDialog");

        var user = await JiraBotAccessorsHelper.GetUser(_accessors, dc.Context, _appSettings, cancellationToken);

        // if user was not connected, do nothing
        if (user == null)
        {
            return await dc.EndDialogAsync(cancellationToken: cancellationToken);
        }

        await _botMessagesService.SendConfigureNotificationsCard(dc.Context, cancellationToken);

        return await dc.EndDialogAsync(cancellationToken: cancellationToken);
    }
}
