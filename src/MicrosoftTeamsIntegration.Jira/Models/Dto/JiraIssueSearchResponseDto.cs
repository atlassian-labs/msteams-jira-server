using Newtonsoft.Json;

namespace MicrosoftTeamsIntegration.Jira.Models.Dto
{
#pragma warning disable SA1401
    public sealed class JiraIssueSearchResponseDto
    {
        [JsonProperty("total")]
        public int Total;

        [JsonProperty("issues")]
        public JiraIssueDto[] JiraIssues;

        [JsonProperty("errorMessages")]
        public string[] ErrorMessages;

        [JsonProperty("prioritiesIdsInOrder")]
        public string[] PrioritiesIdsInOrder { get; set; }

        [JsonProperty("pageSize")]
        public int PageSize { get; set; }
    }
#pragma warning restore SA1401
}
