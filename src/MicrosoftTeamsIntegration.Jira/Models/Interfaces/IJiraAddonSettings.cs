namespace MicrosoftTeamsIntegration.Jira.Models.Interfaces
{
    public interface IJiraAddonSettings
    {
        string GetErrorMessage(string jiraInstance);
        bool AddonIsInstalled { get; }
        string JiraInstanceUrl { get; set; }
        string Version { get; set; }
    }
}
