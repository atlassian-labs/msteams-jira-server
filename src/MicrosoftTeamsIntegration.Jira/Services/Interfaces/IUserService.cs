using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using MicrosoftTeamsIntegration.Jira.Models;

namespace MicrosoftTeamsIntegration.Jira.Services.Interfaces;

public interface IUserService
{
    Task<IntegratedUser> TryToIdentifyUser(ITurnContext context);
}
