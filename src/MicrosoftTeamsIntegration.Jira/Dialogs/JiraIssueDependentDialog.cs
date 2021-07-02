using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using MicrosoftTeamsIntegration.Jira.Helpers;
using MicrosoftTeamsIntegration.Jira.Models;
using MicrosoftTeamsIntegration.Jira.Models.Bot;
using MicrosoftTeamsIntegration.Jira.Models.Jira.Issue;
using MicrosoftTeamsIntegration.Jira.Services.Interfaces;
using MicrosoftTeamsIntegration.Jira.Settings;

namespace MicrosoftTeamsIntegration.Jira.Dialogs
{
    public abstract class JiraIssueDependentDialog : ComponentDialog
    {
        private const string JiraIssueDependingWaterfall = "jiraIssueDependingWaterfall";
        private const string JiraIssueKeyPrompt = "jiraIssueKeyPrompt";

        private readonly JiraBotAccessors _accessors;
        private readonly IJiraService _jiraService;
        private readonly AppSettings _appSettings;

        protected JiraIssueDependentDialog(
            string dialogId,
            JiraBotAccessors accessors,
            IJiraService jiraService,
            AppSettings appSettings)
            : base(dialogId)
        {
            _accessors = accessors;
            _jiraService = jiraService;
            _appSettings = appSettings;

            var waterfallSteps = new WaterfallStep[]
            {
                OnAskForIssueKeyAsync,
                OnStartProcessingAsync
            };
            AddDialog(new WaterfallDialog(JiraIssueDependingWaterfall, waterfallSteps));
            AddDialog(new TextPrompt(JiraIssueKeyPrompt, JiraIssueKeyValidatorAsync));
        }

        protected IntegratedUser CurrentUser { get; set; }
        protected JiraIssue JiraIssue { get; set; }

        protected abstract Task<DialogTurnResult> ProcessJiraIssueAsync(DialogContext dc, IntegratedUser user, JiraIssue jiraIssue);

        private async Task<DialogTurnResult> OnAskForIssueKeyAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // clear jira issue state
            await _accessors.JiraIssueState.SetAsync(stepContext.Context, new JiraIssueState(), cancellationToken);

            var textWithoutCommand = stepContext.Context.Activity.RemoveRecipientMention();
            if (textWithoutCommand.ContainsJiraKeyIssue())
            {
                return await stepContext.NextAsync(cancellationToken: cancellationToken);
            }

            return await stepContext.PromptAsync(
                JiraIssueKeyPrompt,
                new PromptOptions
                {
                    Prompt = MessageFactory.Text("Specify the Jira issue key"),
                    RetryPrompt = MessageFactory.Text("Please enter a valid issue key.")
                },
                cancellationToken);
        }

        private async Task<DialogTurnResult> OnStartProcessingAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var textWithoutMentions = stepContext.Context.Activity.RemoveRecipientMention();
            if (textWithoutMentions.TryGetJiraKeyIssue(out var jiraIssueKey))
            {
                return await ActionOnJiraIssue(stepContext, jiraIssueKey, cancellationToken);
            }

            return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
        }

        private async Task<DialogTurnResult> ActionOnJiraIssue(DialogContext dc, string issueKey, CancellationToken cancellationToken)
        {
            CurrentUser = await JiraBotAccessorsHelper.GetUser(_accessors, dc.Context, _appSettings, cancellationToken);

            try
            {
                var jiraIssueState = await _accessors.JiraIssueState.GetAsync(dc.Context, () => new JiraIssueState(), cancellationToken);
                var jiraIssue = await GetJiraIssueByKey(CurrentUser, issueKey);
                jiraIssueState.JiraIssue = jiraIssue;
                await _accessors.JiraIssueState.SetAsync(dc.Context, jiraIssueState, cancellationToken);

                JiraIssue = jiraIssue;

                if (jiraIssue == null)
                {
                    await dc.Context.SendActivityAsync("I couldn't find an issue", cancellationToken: cancellationToken);
                    return await dc.EndDialogAsync(cancellationToken: cancellationToken);
                }

                return await ProcessJiraIssueAsync(dc, CurrentUser, jiraIssue);
            }
            catch (MethodAccessException e)
            {
                await dc.Context.SendActivityAsync(e.Message, cancellationToken: cancellationToken);
                return await dc.EndDialogAsync(cancellationToken: cancellationToken);
            }
        }

        private async Task<JiraIssue> GetJiraIssueByKey(IntegratedUser user, string issueKey)
        {
            JiraIssue jiraIssue = null;
            var searchRequest = SearchForIssuesRequestBase.CreateFindIssueByIdRequest(issueKey);

            var apiResponse = await _jiraService.Search(user, searchRequest);
            if (apiResponse?.JiraIssues != null && apiResponse.JiraIssues.Any())
            {
                jiraIssue = apiResponse.JiraIssues.First();
                jiraIssue.Schema = apiResponse.Schema;
            }

            return jiraIssue;
        }

        private static async Task<bool> JiraIssueKeyValidatorAsync(PromptValidatorContext<string> promptContext, CancellationToken cancellationToken)
        {
            // Check whether the input could be recognized
            if (!promptContext.Recognized.Succeeded)
            {
                await promptContext.Context.SendActivityAsync(
                    BotMessages.PleaseRepeat,
                    cancellationToken: cancellationToken);
                return false;
            }

            var jiraIssueKey = promptContext.Context.Activity.RemoveRecipientMention();
            if (!jiraIssueKey.IsJiraKeyIssue())
            {
                await promptContext.Context.SendActivityAsync(
                    "Please enter a valid issue key.",
                    cancellationToken: cancellationToken);
                return false;
            }

            return true;
        }
    }
}
