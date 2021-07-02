using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using MicrosoftTeamsIntegration.Artifacts.Extensions;
using MicrosoftTeamsIntegration.Jira.Extensions;
using MicrosoftTeamsIntegration.Jira.Helpers;
using MicrosoftTeamsIntegration.Jira.Models;
using MicrosoftTeamsIntegration.Jira.Models.Bot;
using MicrosoftTeamsIntegration.Jira.Models.Jira.Issue;
using MicrosoftTeamsIntegration.Jira.Services.Interfaces;
using MicrosoftTeamsIntegration.Jira.Settings;

namespace MicrosoftTeamsIntegration.Jira.Dialogs
{
    public class CommentDialog : JiraIssueDependentDialog
    {
        private const string CommentJiraIssueWaterfall = "commentJiraIssueWaterfall";
        private const string CommentJiraIssuePrompt = "commentJiraIssuePrompt";

        private readonly JiraBotAccessors _accessors;
        private readonly IJiraService _jiraService;
        private readonly AppSettings _appSettings;
        private readonly TelemetryClient _telemetry;

        public CommentDialog(
            JiraBotAccessors accessors,
            IJiraService jiraService,
            AppSettings appSettings,
            TelemetryClient telemetry)
            : base(nameof(CommentDialog), accessors, jiraService, appSettings)
        {
            _accessors = accessors;
            _jiraService = jiraService;
            _appSettings = appSettings;
            _telemetry = telemetry;

            var waterfallSteps = new WaterfallStep[]
            {
                OnStartAsync,
                OnSaveCommentAsync
            };
            AddDialog(new WaterfallDialog(CommentJiraIssueWaterfall, waterfallSteps));
            AddDialog(new TextPrompt(CommentJiraIssuePrompt, CommentValidatorAsync));
        }

        protected override Task<DialogTurnResult> ProcessJiraIssueAsync(DialogContext dc, IntegratedUser user, JiraIssue jiraIssue)
        {
            return dc.ReplaceDialogAsync(CommentJiraIssueWaterfall, jiraIssue);
        }

        private async Task<DialogTurnResult> OnStartAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken = default)
        {
            _telemetry.TrackPageView("CommentDialog");

            var user =
                await JiraBotAccessorsHelper.GetUser(_accessors, stepContext.Context, _appSettings, cancellationToken);

            var jiraIssueState =
                await _accessors.JiraIssueState.GetAsync(stepContext.Context, () => new JiraIssueState(), cancellationToken);

            var jiraIssueKey = jiraIssueState.JiraIssue.Key;

            var textWithoutCommand = stepContext.Context.Activity.GetTextWithoutCommand(DialogMatchesAndCommands.CommentDialogCommand);
            var additionalParameter = Regex.Replace(textWithoutCommand, jiraIssueKey, string.Empty, RegexOptions.IgnoreCase).NormalizeUtterance();
            if (!string.IsNullOrEmpty(additionalParameter))
            {
                return await AddComment(stepContext, user, jiraIssueKey, additionalParameter, cancellationToken);
            }

            return await stepContext.PromptAsync(
                CommentJiraIssuePrompt,
                new PromptOptions
                {
                    Prompt = MessageFactory.Text("Please type comment below")
                },
                cancellationToken);
        }

        private async Task<DialogTurnResult> OnSaveCommentAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken = default)
        {
            var user =
                await JiraBotAccessorsHelper.GetUser(_accessors, stepContext.Context, _appSettings, cancellationToken);

            var jiraIssueState =
                await _accessors.JiraIssueState.GetAsync(stepContext.Context, () => new JiraIssueState(), cancellationToken);

            var jiraIssueKey = jiraIssueState.JiraIssue.Key;

            var comment = stepContext.Context.Activity.RemoveRecipientMention();
            if (!string.IsNullOrEmpty(comment))
            {
                return await AddComment(stepContext, user, jiraIssueKey, comment, cancellationToken);
            }

            return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
        }

        private async Task<DialogTurnResult> AddComment(DialogContext dc, IntegratedUser user, string jiraIssueKey, string commentText, CancellationToken cancellationToken)
        {
            var invokedFromCard = dc.Context.Activity?.Value != null;
            var isMessagingExtension = dc.Context.Activity != null && dc.Context.Activity.Type == ActivityTypes.Invoke;

            var response = await _jiraService.AddComment(user, jiraIssueKey, commentText);

            var replyText = response.IsSuccess ? $"You've commented on {jiraIssueKey}." : response.ErrorMessage;

            // if action was invoked from card - send a message to the personal chat
            if (invokedFromCard)
            {
                await dc.Context.SendToDirectConversationAsync(replyText, cancellationToken);
            }
            else
            {
               await dc.Context.SendActivityAsync(replyText, cancellationToken: cancellationToken);
            }

            // if command was revoked from ME card - send InvokeResponse
            if (isMessagingExtension)
            {
                await dc.Context.SendActivityAsync(new Activity { Value = null, Type = ActivityTypesEx.InvokeResponse }, cancellationToken);
            }

            return await dc.EndDialogAsync(cancellationToken: cancellationToken);
        }

        private static async Task<bool> CommentValidatorAsync(PromptValidatorContext<string> promptContext, CancellationToken cancellationToken)
        {
            var comment = promptContext.Context.Activity.RemoveRecipientMention();

            // Check whether the input could be recognized
            if (!promptContext.Recognized.Succeeded || string.IsNullOrEmpty(comment))
            {
                await promptContext.Context.SendActivityAsync(
                    BotMessages.PleaseRepeat,
                    cancellationToken: cancellationToken);
                return false;
            }

            return true;
        }
    }
}
