using System.Threading.Tasks;
using MicrosoftTeamsIntegration.Jira.Models;
using MicrosoftTeamsIntegration.Jira.Models.Notifications;

namespace MicrosoftTeamsIntegration.Jira.Services.Interfaces
{
    public interface IActivityFeedSenderService
    {
        Task GenerateActivityNotification(IntegratedUser user, NotificationFeedEvent notificationFeedEvent);
    }
}
