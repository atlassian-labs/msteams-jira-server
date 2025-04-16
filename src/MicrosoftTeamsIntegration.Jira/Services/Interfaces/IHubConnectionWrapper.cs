using System.Threading;
using System.Threading.Tasks;

namespace MicrosoftTeamsIntegration.Jira.Services.Interfaces;

public interface IHubConnectionWrapper
{
    Task StartAsync(CancellationToken cancellationToken);
    Task StopAsync(CancellationToken cancellationToken);
}
