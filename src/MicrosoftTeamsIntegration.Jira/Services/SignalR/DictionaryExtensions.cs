using System;
using System.Threading.Tasks;
using NonBlocking;

namespace MicrosoftTeamsIntegration.Jira.Services.SignalR
{
    public static class DictionaryExtensions
    {
        public static string GetLog(this ConcurrentDictionary<Guid, TaskCompletionSource<string>> dic)
        {
            if (dic == null || dic.Count == 0)
            {
                return "No client responses available.";
            }

            var s = string.Empty;
            foreach (var key in dic.Keys)
            {
                s += key + ", ";
            }

            return s.TrimEnd(',', ' ');
        }
    }
}