using System.Reflection;
using MicrosoftTeamsIntegration.Jira.Models.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace MicrosoftTeamsIntegration.Jira.ContractResolvers
{
    public class JiraContractResolver : DefaultContractResolver
    {
        public static readonly JiraContractResolver Instance = new JiraContractResolver();

        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            var property = base.CreateProperty(member, memberSerialization);
            if (member.GetCustomAttribute(typeof(JiraServerAttribute), true) != null)
            {
                property.ShouldSerialize = instance => true;
            }

            return property;
        }
    }
}
