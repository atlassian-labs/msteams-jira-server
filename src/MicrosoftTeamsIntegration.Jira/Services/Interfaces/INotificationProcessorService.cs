using System.Reactive;
using System.Threading.Tasks;
using MicrosoftTeamsIntegration.Jira.Models.Notifications;

namespace MicrosoftTeamsIntegration.Jira.Services.Interfaces;

public interface INotificationProcessorService
{
    public Task ProcessNotification(NotificationMessage notification);
}
