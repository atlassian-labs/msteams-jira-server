using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
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
    public class AssignDialog : JiraIssueDependentDialog
    {
        private const string AssignJiraIssueWaterfall = "assignJiraIssueWaterfall";
        private const string MentionUserToAssignIssuePrompt = "mentionUserToAssignIssuePrompt";

        private readonly JiraBotAccessors _accessors;
        private readonly IJiraService _jiraService;
        private readonly IDatabaseService _databaseService;
        private readonly AppSettings _appSettings;
        private readonly IBotMessagesService _botMessagesService;
        private readonly TelemetryClient _telemetry;

        private IntegratedUser MentionedUser { get; set; }

        public AssignDialog(
            JiraBotAccessors accessors,
            IJiraService jiraService,
            AppSettings appSettings,
            IDatabaseService databaseService,
            IBotMessagesService botMessagesService,
            TelemetryClient telemetry)
            : base(nameof(AssignDialog), accessors, jiraService, appSettings)
        {
            _accessors = accessors;
            _jiraService = jiraService;
            _appSettings = appSettings;
            _databaseService = databaseService;
            _botMessagesService = botMessagesService;
            _telemetry = telemetry;

            var waterfallSteps = new WaterfallStep[]
            {
                OnStartAssigningProcessingAsync,
                OnHandleMentionedUserAssigningAsync
            };
            AddDialog(new WaterfallDialog(AssignJiraIssueWaterfall, waterfallSteps));
            AddDialog(new TextPrompt(MentionUserToAssignIssuePrompt, JiraIssueKeyValidatorAsync));
        }

        protected override async Task<DialogTurnResult> ProcessJiraIssueAsync(DialogContext dc, IntegratedUser user, JiraIssue jiraIssue)
        {
            return await dc.ReplaceDialogAsync(AssignJiraIssueWaterfall);
        }

        private async Task<DialogTurnResult> OnStartAssigningProcessingAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken = default)
        {
            _telemetry.TrackPageView("AssignDialog");

            var jiraIssueState =
                await _accessors.JiraIssueState.GetAsync(stepContext.Context, () => new JiraIssueState(), cancellationToken);

            var jiraIssue = jiraIssueState.JiraIssue;

            return await AssignJiraIssueToUser(stepContext, CurrentUser, jiraIssue);
        }

        private async Task<DialogTurnResult> OnHandleMentionedUserAssigningAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken = default)
        {
            var user = MentionedUser;
            if (user == null)
            {
                await stepContext.Context.SendActivityAsync(BotMessages.OperationCancelled, cancellationToken: cancellationToken);
            }
            else
            {
                var jiraIssueState = await _accessors.JiraIssueState.GetAsync(stepContext.Context, () => new JiraIssueState(), cancellationToken);

                var jiraIssue = jiraIssueState.JiraIssue;

                if (!string.IsNullOrEmpty(jiraIssue?.Key))
                {
                    await AssignJiraIssueToUser(stepContext, user, jiraIssue);
                }
            }

            return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
        }

        private async Task<DialogTurnResult> AssignJiraIssueToUser(DialogContext dc, IntegratedUser user, JiraIssue jiraIssue)
        {
            CurrentUser = await JiraBotAccessorsHelper.GetUser(_accessors, dc.Context, _appSettings);

            var issueKey = jiraIssue.Key;
            var invokedFromCard = dc.Context.Activity?.Value != null;

            var userNameOrAccountId = await _jiraService.GetUserNameOrAccountId(user);

            if (!jiraIssue.IsAssignedToUser(userNameOrAccountId))
            {
                var response = await _jiraService.Assign(CurrentUser, issueKey, userNameOrAccountId);
                if (response.IsSuccess)
                {
                    if (CurrentUser.MsTeamsUserId == user.MsTeamsUserId)
                    {
                        // if command is sent by clicking the button - re-render adaptive card
                        if (invokedFromCard)
                        {
                            await _botMessagesService.BuildAndUpdateJiraIssueCard(dc.Context, user, issueKey);
                        }
                        else
                        {
                            await dc.Context.SendActivityAsync($"You have been assigned {issueKey}.");
                        }
                    }
                    else
                    {
                        await dc.Context.SendActivityAsync($"{issueKey} has been assigned.");
                    }
                }
                else
                {
                    if (invokedFromCard)
                    {
                        await dc.Context.SendToDirectConversationAsync(response.ErrorMessage);
                    }
                    else
                    {
                        await dc.Context.SendActivityAsync(response.ErrorMessage);
                    }
                }
            }
            else
            {
                if (CurrentUser.MsTeamsUserId == user.MsTeamsUserId)
                {
                    var replyText = $"You're already assigned to {issueKey}.";
                    if (invokedFromCard)
                    {
                        await dc.Context.SendToDirectConversationAsync(replyText);
                    }
                    else
                    {
                        await dc.Context.SendActivityAsync(replyText);
                    }
                }
                else
                {
                    await dc.Context.SendActivityAsync($"{issueKey} is already assigned.");
                }
            }

            return await dc.EndDialogAsync();
        }

        private async Task<bool> JiraIssueKeyValidatorAsync(PromptValidatorContext<string> promptContext, CancellationToken cancellationToken)
        {
            // Check whether the input could be recognized
            if (!promptContext.Recognized.Succeeded)
            {
                await promptContext.Context.SendActivityAsync(
                    BotMessages.PleaseRepeat,
                    cancellationToken: cancellationToken);
                return false;
            }

            var currentUser = await JiraBotAccessorsHelper.GetUser(_accessors, promptContext.Context, _appSettings, cancellationToken);

            // trim bot mention to compare for myself assigning. Users mentioning will be taking in GetMsTeamsUserIdFromMentions method
            var mention = promptContext.Context.Activity.RemoveRecipientMention();
            if (string.Equals(mention, "myself", StringComparison.OrdinalIgnoreCase))
            {
                MentionedUser = currentUser;
                return true;
            }

            var msTeamsUserId = await promptContext.Context.GetMsTeamsUserIdFromMentions();

            var jiraId = currentUser.JiraServerId;
            var mentionedUser = await _databaseService.GetUserByTeamsUserIdAndJiraUrl(msTeamsUserId, jiraId);

            if (mentionedUser is null)
            {
                var couldNotFindMessage = "I couldn't find this person. Make sure they have the right permissions or try someone new.";
                await promptContext.Context.SendActivityAsync(
                    couldNotFindMessage,
                    cancellationToken: cancellationToken);
                return false;
            }

            MentionedUser = mentionedUser;

            return true;
        }
    }
}
