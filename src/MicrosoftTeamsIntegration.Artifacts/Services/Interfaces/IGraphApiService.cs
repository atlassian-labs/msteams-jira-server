using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Kiota.Abstractions;
using MicrosoftTeamsIntegration.Artifacts.Models.GraphApi;

namespace MicrosoftTeamsIntegration.Artifacts.Services.Interfaces
{
    public interface IGraphApiService
    {
        Task SendActivityNotificationAsync(GraphServiceClient graphClient, ActivityNotification notification, string userId, CancellationToken cancellationToken);
        Task<TeamsApp> GetApplicationAsync(GraphServiceClient graphClient, string appId, CancellationToken cancellationToken);
    }
}
