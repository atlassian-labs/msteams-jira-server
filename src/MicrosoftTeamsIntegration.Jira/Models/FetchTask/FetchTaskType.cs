using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MicrosoftTeamsIntegration.Jira.Models.FetchTask
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum FetchTaskType
    {
        [EnumMember(Value = "message")]
        Message,

        [EnumMember(Value = "continue")]
        Continue
    }
}
