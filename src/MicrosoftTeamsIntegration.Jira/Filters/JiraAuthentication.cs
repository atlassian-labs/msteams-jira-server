using Microsoft.AspNetCore.Mvc;

namespace MicrosoftTeamsIntegration.Jira.Filters
{
    public class JiraAuthentication : TypeFilterAttribute
    {
        public JiraAuthentication()
            : base(typeof(JiraAuthFilter))
        {
        }
    }
}
