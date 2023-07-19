using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using MicrosoftTeamsIntegration.Jira.Dialogs;
using MicrosoftTeamsIntegration.Jira.Models.Bot;
using MicrosoftTeamsIntegration.Jira.Services.Interfaces;

namespace MicrosoftTeamsIntegration.Jira.Services
{
    public class CommandDialogReferenceService : ICommandDialogReferenceService
    {
        private readonly List<JiraActionRegexReference> _actionCommands = new List<JiraActionRegexReference>();

        public CommandDialogReferenceService()
        {
            BuildCommandDialogReferences();
        }

        public JiraActionRegexReference GetActionReference(string searchCommand)
        {
            return _actionCommands
                .FirstOrDefault(command => Regex.IsMatch(searchCommand, command.Regex, RegexOptions.Compiled | RegexOptions.IgnoreCase));
        }

        private void BuildCommandDialogReferences()
        {
            const string regexPrefix = @"^(\s*)";

            _actionCommands.Add(new JiraActionRegexReference(
                nameof(HelpDialog),
                DialogMatchesAndCommands.HelpDialogCommand,
                $"{regexPrefix}{DialogMatchesAndCommands.HelpDialogCommand}",
                isPersonal: true,
                isTeamAction: true,
                requireAuthentication: false));
            _actionCommands.Add(new JiraActionRegexReference(
                nameof(ConnectToJiraDialog),
                DialogMatchesAndCommands.ConnectToJiraDialogCommand,
                $"{regexPrefix}{DialogMatchesAndCommands.ConnectToJiraDialogCommand}",
                isPersonal: true,
                isTeamAction: true,
                requireAuthentication: false));
            _actionCommands.Add(new JiraActionRegexReference(
                    nameof(WatchDialog),
                    DialogMatchesAndCommands.WatchDialogCommand,
                    $"{regexPrefix}{DialogMatchesAndCommands.WatchDialogCommand}",
                    isPersonal: true,
                    isTeamAction: true));
            _actionCommands.Add(new JiraActionRegexReference(
                    nameof(UnwatchDialog),
                    DialogMatchesAndCommands.UnwatchDialogCommand,
                    $"{regexPrefix}{DialogMatchesAndCommands.UnwatchDialogCommand}",
                    isPersonal: true,
                    isTeamAction: true));
            _actionCommands.Add(new JiraActionRegexReference(
                    nameof(AssignDialog),
                    DialogMatchesAndCommands.AssignDialogCommand,
                    $"{regexPrefix}{DialogMatchesAndCommands.AssignDialogCommand}",
                    isPersonal: true,
                    isTeamAction: true));
            _actionCommands.Add(new JiraActionRegexReference(
                    nameof(FindDialog),
                    DialogMatchesAndCommands.FindDialogCommand,
                    $"{regexPrefix}{DialogMatchesAndCommands.FindDialogCommand}",
                    isPersonal: true,
                    isTeamAction: false));
            _actionCommands.Add(new JiraActionRegexReference(
                    nameof(VoteDialog),
                    DialogMatchesAndCommands.VoteDialogCommand,
                    $"{regexPrefix}{DialogMatchesAndCommands.VoteDialogCommand}",
                    isPersonal: true,
                    isTeamAction: true));
            _actionCommands.Add(new JiraActionRegexReference(
                    nameof(UnvoteDialog),
                    DialogMatchesAndCommands.UnvoteDialogCommand,
                    $"{regexPrefix}{DialogMatchesAndCommands.UnvoteDialogCommand}",
                    isPersonal: true,
                    isTeamAction: true));
            _actionCommands.Add(new JiraActionRegexReference(
                    nameof(IssueEditDialog),
                    DialogMatchesAndCommands.IssueEditDialogCommand,
                    $"{regexPrefix}{DialogMatchesAndCommands.IssueEditDialogCommand}",
                    isPersonal: true,
                    isTeamAction: true));
            _actionCommands.Add(new JiraActionRegexReference(
                    nameof(CreateNewIssueDialog),
                    DialogMatchesAndCommands.CreateNewIssueDialogCommand,
                    $"{regexPrefix}{DialogMatchesAndCommands.CreateNewIssueDialogCommand}",
                    isPersonal: true,
                    isTeamAction: true));
            _actionCommands.Add(new JiraActionRegexReference(
                    nameof(LogTimeDialog),
                    DialogMatchesAndCommands.LogTimeDialogCommand,
                    $"{regexPrefix}{DialogMatchesAndCommands.LogTimeDialogCommand}",
                    isPersonal: true));
            _actionCommands.Add(new JiraActionRegexReference(
                    nameof(CommentDialog),
                    DialogMatchesAndCommands.CommentDialogCommand,
                    $"{regexPrefix}{DialogMatchesAndCommands.CommentDialogCommand}",
                    isPersonal: true,
                    isTeamAction: true));
            _actionCommands.Add(new JiraActionRegexReference(
                    nameof(IssueByKeyDialog),
                    string.Empty,
                    JiraConstants.JiraIssueStrictMatchRegex,
                    isPersonal: true,
                    isTeamAction: false));
            _actionCommands.Add(new JiraActionRegexReference(
                    nameof(DisconnectJiraDialog),
                    DialogMatchesAndCommands.DisconnectJiraDialogCommand,
                    $"{regexPrefix}{DialogMatchesAndCommands.DisconnectJiraDialogCommand}",
                    isPersonal: true,
                    isTeamAction: true,
                    requireAuthentication: false));
            _actionCommands.Add(new JiraActionRegexReference(
                nameof(SignoutMsAccountDialog),
                DialogMatchesAndCommands.SignoutMsAccountDialogCommand,
                $"{regexPrefix}{DialogMatchesAndCommands.SignoutMsAccountDialogCommand}",
                isPersonal: true,
                isTeamAction: true,
                requireAuthentication: false));
        }
    }
}
