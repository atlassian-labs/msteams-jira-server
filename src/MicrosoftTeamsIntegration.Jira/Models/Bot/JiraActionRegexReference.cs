namespace MicrosoftTeamsIntegration.Jira.Models.Bot
{
    public sealed class JiraActionRegexReference
    {
        public JiraActionRegexReference(string dialogName, string commandName, string regex, bool isPersonal = false, bool isTeamAction = false, bool requireAuthentication = true)
        {
            DialogName = dialogName;
            CommandName = commandName;
            Regex = regex;
            IsPersonalAction = isPersonal;
            IsTeamAction = isTeamAction;
            RequireAuthentication = requireAuthentication;
        }

        public string DialogName { get; }
        public string CommandName { get; }
        public string Regex { get; }
        public bool IsPersonalAction { get; }
        public bool IsTeamAction { get; }
        public bool RequireAuthentication { get; }
    }
}
