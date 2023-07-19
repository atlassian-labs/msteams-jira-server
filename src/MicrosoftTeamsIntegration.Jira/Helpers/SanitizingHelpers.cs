using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MicrosoftTeamsIntegration.Jira.Helpers
{
    public static class SanitizingHelpers
    {
        public const string Replacement = "(DELETED)";
        public const string SanitizationErrorMessage = "Error during sanitization. Deleted the whole payload.";

        private static readonly List<string> FieldsToProcess = new List<string> { "token" };

        public static string SanitizeMessage(string source)
        {
            try
            {
                var json = JObject.Parse(source);
                foreach (var token in FieldsToProcess.Select(field => json.SelectToken(field)))
                {
                    token?.Replace(Replacement);
                }

                return json.ToString(Formatting.None);
            }
            catch (Exception)
            {
                return SanitizationErrorMessage;
            }
        }
    }
}
