using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Web;
using JetBrains.Annotations;

namespace MicrosoftTeamsIntegration.Artifacts.Extensions
{
    [PublicAPI]
    public static class StringExtensions
    {
        public static string NormalizeUtterance(this string utterance)
            => utterance?
                   .Trim()
               ?? string.Empty;

        public static string AddOrUpdateGetParameter(this string url, string paramName, string paramValue)
        {
            if (string.IsNullOrEmpty(url))
            {
                return string.Empty;
            }

            var uriBuilder = new UriBuilder(url);
            var query = HttpUtility.ParseQueryString(uriBuilder.Query);

            if (query.AllKeys.Contains(paramName))
            {
                query[paramName] = paramValue;
            }
            else
            {
                query.Add(paramName, paramValue);
            }

            uriBuilder.Query = query.ToString();

            return uriBuilder.Uri.ToString();
        }

        public static bool HasValue(this string s) => !string.IsNullOrEmpty(s);

        public static bool IsContainsQuitCondition(this string s) => s.Equals("cancel", StringComparison.InvariantCultureIgnoreCase)
                                                                 || s.Equals("back", StringComparison.InvariantCultureIgnoreCase)
                                                                 || s.Equals("undo", StringComparison.InvariantCultureIgnoreCase)
                                                                 || s.Equals("reset", StringComparison.InvariantCultureIgnoreCase);

        #nullable disable
        public static bool TryParseJson<T>(this string value, out T result)
        {
            try
            {
                result = JsonSerializer.Deserialize<T>(value);
                return true;
            }
            catch (Exception)
            {
                result = default;
                return false;
            }
        }
        #nullable enable

        public static string SanitizeForAzureKeys(this string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }

            var remap = new Dictionary<string, string>() { { "/", "|s|" }, { @"\", "|b|" }, { "#", "|h|" }, { "?", "|q|" } };
            return input.Trim()
                .Replace("/", remap["/"], StringComparison.InvariantCultureIgnoreCase)
                .Replace(@"\", remap[@"\"], StringComparison.InvariantCultureIgnoreCase)
                .Replace("#", remap["#"], StringComparison.InvariantCultureIgnoreCase)
                .Replace("?", remap["?"], StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
