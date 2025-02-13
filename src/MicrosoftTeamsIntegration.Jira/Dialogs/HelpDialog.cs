using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.Bot.Builder.Dialogs;
using MicrosoftTeamsIntegration.Artifacts.Extensions;
using MicrosoftTeamsIntegration.Jira.Services.Interfaces;
using MicrosoftTeamsIntegration.Jira.Settings;

namespace MicrosoftTeamsIntegration.Jira.Dialogs
{
    public class HelpDialog : Dialog
    {
        private readonly TelemetryClient _telemetry;
        private readonly IAnalyticsService _analyticsService;

        public HelpDialog(JiraBotAccessors accessors, AppSettings appSettings, TelemetryClient telemetry, IAnalyticsService analyticsService)
            : base(nameof(HelpDialog))
        {
            _telemetry = telemetry;
            _analyticsService = analyticsService;
        }

        public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default)
        {
            _telemetry.TrackPageView("HelpDialog");

            var message = "Here’s a list of the commands I can process:\n\n";
            var isGroup = dc.Context.Activity.IsGroupConversation();
            var jiraWording = "Jira Data Center instance";

            message += (isGroup
                            ? string.Empty :
                            "type an issue key (e.g. **MP-47**) to view issue card with actions") + "\n\n" +
                       (isGroup
                           ? string.Empty
                           : $"**{DialogMatchesAndCommands.ConnectToJiraDialogCommand}** - connect a {jiraWording} to your Microsoft Teams account") + "\n\n" +
                       (isGroup
                           ? string.Empty
                           : $"**{DialogMatchesAndCommands.CreateNewIssueDialogCommand}** - create a new issue") + "\n\n" +
                       (isGroup
                           ? string.Empty :
                           $"**{DialogMatchesAndCommands.FindDialogCommand}** - obtain issue(s) by a summary search phrase or an issue key (e.g. **{DialogMatchesAndCommands.FindDialogCommand} MP-47** or **{DialogMatchesAndCommands.FindDialogCommand} search_phrase**)") + "\n\n" +
                       (isGroup
                           ? string.Empty
                           : $"**{DialogMatchesAndCommands.AssignDialogCommand}** - assign the issue to yourself (e.g. **{DialogMatchesAndCommands.AssignDialogCommand} MP-47**)") + "\n\n" +
                       (isGroup
                           ? string.Empty
                           : $"**{DialogMatchesAndCommands.IssueEditDialogCommand}** - open the issue card to change priority, summary, and description of the issue") + "\n\n" +
                       (isGroup
                           ? string.Empty
                           : $"**{DialogMatchesAndCommands.LogTimeDialogCommand}** - log time spent on the issue") + "\n\n" +
                       $"**{DialogMatchesAndCommands.WatchDialogCommand}** - start watching the issue (e.g. **{DialogMatchesAndCommands.WatchDialogCommand} MP-47**)\n\n" +
                       $"**{DialogMatchesAndCommands.UnwatchDialogCommand}** - stop watching the issue (e.g. **{DialogMatchesAndCommands.UnwatchDialogCommand} MP-47**)\n\n" +
                       $"**{DialogMatchesAndCommands.VoteDialogCommand}** - vote on the issue\n\n" +
                       $"**{DialogMatchesAndCommands.UnvoteDialogCommand}** - unvote on the issue\n\n" +
                       $"**{DialogMatchesAndCommands.CommentDialogCommand}** - comment the issue\n\n" +
                       (isGroup
                           ? string.Empty
                           : $"**{DialogMatchesAndCommands.DisconnectJiraDialogCommand}** - disconnect {jiraWording} you've connected from Microsoft Teams") + "\n\n" +
                       $"**{DialogMatchesAndCommands.CancelCommand}** - cancel current dialog\n\n"
                ;

            await dc.Context.SendActivityAsync(message, cancellationToken: cancellationToken);
            _analyticsService.SendBotDialogEvent(dc.Context, "help", "completed");
            return await dc.EndDialogAsync(cancellationToken: cancellationToken);
        }
    }
}
