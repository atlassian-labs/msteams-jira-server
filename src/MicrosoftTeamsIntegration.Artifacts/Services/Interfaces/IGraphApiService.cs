using System.Threading;
using System.Threading.Tasks;
using Microsoft.Graph;
using MicrosoftTeamsIntegration.Artifacts.Models.GraphApi;

namespace MicrosoftTeamsIntegration.Artifacts.Services.Interfaces
{
    public interface IGraphApiService
    {
        Task SendActivityNotificationAsync(IGraphServiceClient graphClient, ActivityNotification notification, string userId, CancellationToken cancellationToken);
        Task<TeamsApp> GetApplicationAsync(IGraphServiceClient graphClient, string appId, CancellationToken cancellationToken);
    }
}
