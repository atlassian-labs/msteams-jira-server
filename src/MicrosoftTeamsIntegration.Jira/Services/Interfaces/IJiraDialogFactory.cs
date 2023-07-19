using MicrosoftTeamsIntegration.Jira.Models;

namespace MicrosoftTeamsIntegration.Jira.Services.Interfaces
{
    public interface IJiraDialogFactory
    {
        T CreateUserSpecificDialog<T>(IntegratedUser user);
    }
}
