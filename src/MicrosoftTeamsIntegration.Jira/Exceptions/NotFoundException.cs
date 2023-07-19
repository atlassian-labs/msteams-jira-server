using System;

namespace MicrosoftTeamsIntegration.Jira.Exceptions
{
    public class NotFoundException : Exception
    {
        public NotFoundException(string message)
            : base(message)
        {
        }
    }
}
