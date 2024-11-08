using System;
using System.Runtime.Serialization;

namespace MicrosoftTeamsIntegration.Jira.Exceptions
{
    [Serializable]
    public class BadRequestException : Exception
    {
        public BadRequestException(string message)
            : base(message)
        {
        }
    }
}
