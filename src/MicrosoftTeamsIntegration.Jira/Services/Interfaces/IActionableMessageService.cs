using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using MicrosoftTeamsIntegration.Jira.Models;

namespace MicrosoftTeamsIntegration.Jira.Services.Interfaces
{
    public interface IActionableMessageService
    {
        Task<bool> HandleConnectorCardActionQuery(ITurnContext context, IntegratedUser user);
        Task HandleSuccessfulConnection(ITurnContext context);
    }
}
