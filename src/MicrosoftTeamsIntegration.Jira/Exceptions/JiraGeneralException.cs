using System;

namespace MicrosoftTeamsIntegration.Jira.Exceptions
{
    public class JiraGeneralException : Exception
    {
        public JiraGeneralException(string message)
            : base(message)
        {
        }
    }
}
