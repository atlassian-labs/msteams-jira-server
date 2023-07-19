using System;

namespace MicrosoftTeamsIntegration.Jira.Exceptions
{
    public class UnauthorizedException : Exception
    {
        public UnauthorizedException()
            : base(JiraConstants.UserNotAuthorizedMessage)
        {
        }
    }
}
