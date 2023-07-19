using MicrosoftTeamsIntegration.Jira.Dialogs;
using Xunit;

namespace MicrosoftTeamsIntegration.Jira.Tests.Dialogs
{
    public class DialogMatchesAndCommandsTests
    {
        [Fact]
        public void CheckCommands()
        {
            Assert.Equal("watch", DialogMatchesAndCommands.WatchDialogCommand);
            Assert.Equal("unwatch", DialogMatchesAndCommands.UnwatchDialogCommand);

            Assert.Equal("assign", DialogMatchesAndCommands.AssignDialogCommand);

            Assert.Equal("log", DialogMatchesAndCommands.LogTimeDialogCommand);
            Assert.Equal("connect", DialogMatchesAndCommands.ConnectToJiraDialogCommand);
            Assert.Equal("disconnect", DialogMatchesAndCommands.DisconnectJiraDialogCommand);

            Assert.Equal("find", DialogMatchesAndCommands.FindDialogCommand);

            Assert.Equal("priority", DialogMatchesAndCommands.PriorityDialogCommand);

            Assert.Equal("comment", DialogMatchesAndCommands.CommentDialogCommand);

            Assert.Equal("description", DialogMatchesAndCommands.DescriptionDialogCommand);

            Assert.Equal("summary", DialogMatchesAndCommands.SummaryDialogCommand);

            Assert.Equal("vote", DialogMatchesAndCommands.VoteDialogCommand);
            Assert.Equal("unvote", DialogMatchesAndCommands.UnvoteDialogCommand);

            Assert.Equal("help", DialogMatchesAndCommands.HelpDialogCommand);

            Assert.Equal("create", DialogMatchesAndCommands.CreateNewIssueDialogCommand);
            Assert.Equal("edit", DialogMatchesAndCommands.IssueEditDialogCommand);

            Assert.Equal("cancel", DialogMatchesAndCommands.CancelCommand);

            Assert.Equal("o365connector.card.", DialogMatchesAndCommands.O365ConnectorCardPrefix);
            Assert.Equal(DialogMatchesAndCommands.O365ConnectorCardPrefix + "post.action.",
                DialogMatchesAndCommands.O365ConnectorCardPostActionPrefix);

            Assert.Equal("editIssue", DialogMatchesAndCommands.EditIssueTaskModuleCommand);

            Assert.Equal("signout", DialogMatchesAndCommands.SignoutMsAccountDialogCommand);
        }
    }
}
