using MicrosoftTeamsIntegration.Jira.Dialogs;
using MicrosoftTeamsIntegration.Jira.Services;
using Xunit;

namespace MicrosoftTeamsIntegration.Jira.Tests.Services
{
    public class CommandDialogReferenceServiceTests
    {
        [Theory]
        [InlineData(DialogMatchesAndCommands.ConnectToJiraDialogCommand, nameof(ConnectToJiraDialog))]
        [InlineData(DialogMatchesAndCommands.HelpDialogCommand, nameof(HelpDialog))]
        [InlineData(DialogMatchesAndCommands.WatchDialogCommand, nameof(WatchDialog))]
        [InlineData(DialogMatchesAndCommands.UnwatchDialogCommand, nameof(UnwatchDialog))]
        [InlineData(DialogMatchesAndCommands.AssignDialogCommand, nameof(AssignDialog))]
        [InlineData(DialogMatchesAndCommands.FindDialogCommand, nameof(FindDialog))]
        [InlineData(DialogMatchesAndCommands.VoteDialogCommand, nameof(VoteDialog))]
        [InlineData(DialogMatchesAndCommands.UnvoteDialogCommand, nameof(UnvoteDialog))]
        [InlineData(DialogMatchesAndCommands.IssueEditDialogCommand, nameof(IssueEditDialog))]
        [InlineData(DialogMatchesAndCommands.CreateNewIssueDialogCommand, nameof(CreateNewIssueDialog))]
        [InlineData(DialogMatchesAndCommands.LogTimeDialogCommand, nameof(LogTimeDialog))]
        [InlineData(DialogMatchesAndCommands.CommentDialogCommand, nameof(CommentDialog))]
        [InlineData(DialogMatchesAndCommands.DisconnectJiraDialogCommand, nameof(DisconnectJiraDialog))]
        [InlineData(DialogMatchesAndCommands.SignoutMsAccountDialogCommand, nameof(SignoutMsAccountDialog))]
        public void GetActionReference(string command, string dialogName)
        {
            var _service = new CommandDialogReferenceService();
            var result = _service.GetActionReference(command);
            Assert.Equal(dialogName, result.DialogName);
        }
    }
}
