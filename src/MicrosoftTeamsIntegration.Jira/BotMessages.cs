namespace MicrosoftTeamsIntegration.Jira
{
    public static class BotMessages
    {
        public const string WelcomeMessage = "Hi, I’m here to help you manage your Jira issues. To begin with, please type 'connect' to connect to your Jira.";

        public const string SomethingWentWrong = "Oops something went wrong, please try again.";
        public const string OperationCancelled = "Operation cancelled.";
        public const string NotValidOption = "Message was not a valid option.";
        public const string PleaseRepeat = "Sorry, I didn't understand.";

        public const string ConnectDialogCardTitle = "In order to use bot, please connect it first and rerun the command.";

        public const string JiraDisconnectDialogConfirmPrompt = "Are you sure you want to disconnect?";
        public const string JiraDisconnectDialogNotConnected = "You are not connected to any Jira at the moment.";
        public const string JiraDisconnectAtlassianLogOut = "If you'd like to switch to different Atlassian account, please log out first.";
    }
}
