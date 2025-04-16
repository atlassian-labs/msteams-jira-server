using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using MicrosoftTeamsIntegration.Jira.Services.Interfaces;

namespace MicrosoftTeamsIntegration.Jira.Services.SignalR;

public class HubConnectionWrapper : IHubConnectionWrapper
{
    private readonly HubConnection _hubConnection;

    public HubConnectionWrapper(HubConnection hubConnection)
    {
        _hubConnection = hubConnection;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return _hubConnection.StartAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return _hubConnection.StopAsync(cancellationToken);
    }
}
