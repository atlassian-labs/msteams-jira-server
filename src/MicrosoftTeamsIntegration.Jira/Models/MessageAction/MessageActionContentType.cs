using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MicrosoftTeamsIntegration.Jira.Models.MessageAction
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum MessageActionContentType
    {
        [EnumMember(Value = "html")]
        Html,

        [EnumMember(Value = "text")]
        Text
    }
}
