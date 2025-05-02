using System.Threading;
using System.Threading.Tasks;
using AdaptiveCards;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using MicrosoftTeamsIntegration.Jira.Models;
using MicrosoftTeamsIntegration.Jira.Models.Notifications;

namespace MicrosoftTeamsIntegration.Jira.Services.Interfaces
{
    public interface IBotMessagesService
    {
        string HandleHtmlMessageFromUser(Activity activity);
        Task HandleConversationUpdates(ITurnContext turnContext, CancellationToken cancellationToken);
        Task BuildAndUpdateJiraIssueCard(ITurnContext turnContext, IntegratedUser user, string issueKey);
        Task<AdaptiveCard> SearchIssueAndBuildIssueCard(ITurnContext turnContext, IntegratedUser user, string jiraIssueKey);
        Task SendAuthorizationCard(ITurnContext turnContext, string jiraUrl, CancellationToken cancellationToken = default);
        Task SendConnectCard(ITurnContext turnContext, CancellationToken cancellationToken = default);
        Task SendConfigureNotificationsCard(ITurnContext turnContext, CancellationToken cancellationToken = default);
        Task SendNotificationCard(ITurnContext turnContext, NotificationMessage notificationMessage, CancellationToken cancellationToken = default);
        AdaptiveCard BuildConfigureNotificationsCard(ITurnContext turnContext);
        AdaptiveCard BuildNotificationConfigurationSummaryCard(NotificationSubscription subscription, bool showSuccessMessage = false);
        AdaptiveCard BuildHelpCard(ITurnContext turnContext);
    }
}
