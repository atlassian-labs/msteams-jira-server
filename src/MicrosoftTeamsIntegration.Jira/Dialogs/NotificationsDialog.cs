using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using MicrosoftTeamsIntegration.Artifacts.Extensions;
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
    private readonly INotificationSubscriptionService _notificationSubscriptionService;

    public NotificationsDialog(
        JiraBotAccessors accessors,
        IBotMessagesService botMessagesService,
        AppSettings appSettings,
        TelemetryClient telemetry,
        INotificationSubscriptionService notificationSubscriptionService)
        : base(nameof(NotificationsDialog))
    {
        _accessors = accessors;
        _botMessagesService = botMessagesService;
        _appSettings = appSettings;
        _telemetry = telemetry;
        _notificationSubscriptionService = notificationSubscriptionService;
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

        if (dc.Context.Activity.IsGroupConversation())
        {
            await _botMessagesService.SendConfigureNotificationsCard(dc.Context, cancellationToken);
        }
        else
        {
            var personalSubscription = await _notificationSubscriptionService.GetNotificationSubscription(user);
            if (personalSubscription != null && personalSubscription.IsActive && personalSubscription.EventTypes.Length != 0)
            {
                var adaptiveCard = _botMessagesService.BuildNotificationConfigurationSummaryCard(personalSubscription);
                var message = MessageFactory.Attachment(adaptiveCard.ToAttachment());
                await dc.Context.SendToDirectConversationAsync(message, cancellationToken: cancellationToken);
            }
            else
            {
                await _botMessagesService.SendConfigureNotificationsCard(dc.Context, cancellationToken);
            }
        }

        return await dc.EndDialogAsync(cancellationToken: cancellationToken);
    }
}
