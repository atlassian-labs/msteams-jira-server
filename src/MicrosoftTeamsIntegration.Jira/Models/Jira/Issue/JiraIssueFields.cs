using System;
using System.Collections.Generic;
using System.Dynamic;
using MicrosoftTeamsIntegration.Jira.Models.Attributes;
using Newtonsoft.Json;

namespace MicrosoftTeamsIntegration.Jira.Models.Jira.Issue
{
    public class JiraIssueFields
    {
        [JsonProperty("issuetype")]
        public JiraIssueType Type { get; set; }

        [JsonProperty("timespent")]
        public string Timespent { get; set; }

        [JsonProperty("project")]
        public JiraProject Project { get; set; }

        [JsonProperty("lastViewed")]
        public DateTime? LastViewed { get; set; }

        [JsonProperty("watches")]
        public JiraIssueWatches Watches { get; set; }

        [JsonProperty("created")]
        public DateTime Created { get; set; }

        [JsonProperty("priority")]
        public JiraIssuePriority Priority { get; set; }

        [JsonProperty("assignee")]
        public JiraUser Assignee { get; set; }

        [JsonProperty("updated")]
        public DateTime Updated { get; set; }

        [JsonProperty("status")]
        public JiraIssueStatus Status { get; set; }

        [JsonProperty("summary")]
        public string Summary { get; set; }

        [JsonProperty("creator")]
        public JiraUser Creator { get; set; }

        [JsonProperty("reporter")]
        public JiraUser Reporter { get; set; }

        [JsonProperty("duedate")]
        public DateTime? DueDate { get; set; }

        [JsonProperty("votes")]
        public JiraIssueVotes Votes { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("resolutiondate")]
        public DateTime? ResolutionDate { get; set; }

        [JsonProperty("parent")]
        public JiraIssue Parent { get; set; }

        [JsonProperty("subtasks")]
        public List<JiraIssue> Subtasks { get; set; }

        [JsonProperty("fields")]
        public ExpandoObject Fields { get; set; }

        [JsonProperty("editIssueMetadata")]
        public ExpandoObject EditIssueMetadata { get; set; }
    }
}
