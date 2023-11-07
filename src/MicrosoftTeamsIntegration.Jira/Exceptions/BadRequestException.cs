using System;

namespace MicrosoftTeamsIntegration.Jira.Exceptions
{
    public class BadRequestException : Exception
    {
        public BadRequestException(string message)
            : base(message)
        {
        }
    }
}
