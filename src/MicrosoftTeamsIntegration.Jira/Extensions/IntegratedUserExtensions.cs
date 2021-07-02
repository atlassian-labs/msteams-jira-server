using MicrosoftTeamsIntegration.Artifacts.Extensions;
using MicrosoftTeamsIntegration.Jira.Models;

namespace MicrosoftTeamsIntegration.Jira.Extensions
{
    public static class IntegratedUserExtensions
    {
        public static bool HasJiraAuthInfo(this IntegratedUser user)
        {
            return user != null;
        }
    }
}
