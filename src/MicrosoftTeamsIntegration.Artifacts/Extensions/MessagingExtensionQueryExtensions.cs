using System;
using JetBrains.Annotations;
using Microsoft.Bot.Schema.Teams;

namespace MicrosoftTeamsIntegration.Artifacts.Extensions
{
    [PublicAPI]
    public static class MessagingExtensionQueryExtensions
    {
        public static string GetQueryParameterByName(this MessagingExtensionQuery extensionQuery, string parameterName)
        {
            if (extensionQuery?.Parameters == null || extensionQuery.Parameters.Count == 0)
            {
                return string.Empty;
            }

            var parameter = extensionQuery.Parameters[0];
            if (!string.Equals(parameter.Name, parameterName, StringComparison.OrdinalIgnoreCase))
            {
                return string.Empty;
            }

            return (parameter.Value != null ? parameter.Value.ToString() : string.Empty)!;
        }
    }
}
