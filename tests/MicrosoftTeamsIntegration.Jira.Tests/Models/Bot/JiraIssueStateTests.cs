using System.Collections.Generic;
using MicrosoftTeamsIntegration.Jira.Models.Bot;
using MicrosoftTeamsIntegration.Jira.Models.Jira;
using MicrosoftTeamsIntegration.Jira.Models.Jira.Issue;
using Xunit;

namespace MicrosoftTeamsIntegration.Jira.Tests.Models.Bot;

public class JiraIssueStateTests
{
    [Fact]
    public void JiraIssueState_ShouldInitializeProperties()
    {
        var jiraIssue = new JiraIssue();
        var summary = "Test Summary";
        var selectedProject = new JiraProject();
        var selectedIssueType = new JiraIssueType();
        var availableProjects = new List<JiraProject> { new JiraProject() };
        var availableIssueTypes = new List<JiraIssueType> { new JiraIssueType() };
        var jiraId = "JIRA-123";

        var jiraIssueState = new JiraIssueState
        {
            JiraIssue = jiraIssue,
            Summary = summary,
            SelectedProject = selectedProject,
            SelectedIssueType = selectedIssueType,
            AvailableProjects = availableProjects,
            AvailableIssueTypes = availableIssueTypes,
            JiraId = jiraId
        };

        Assert.Equal(jiraIssue, jiraIssueState.JiraIssue);
        Assert.Equal(summary, jiraIssueState.Summary);
        Assert.Equal(selectedProject, jiraIssueState.SelectedProject);
        Assert.Equal(selectedIssueType, jiraIssueState.SelectedIssueType);
        Assert.Equal(availableProjects, jiraIssueState.AvailableProjects);
        Assert.Equal(availableIssueTypes, jiraIssueState.AvailableIssueTypes);
        Assert.Equal(jiraId, jiraIssueState.JiraId);
    }
}
