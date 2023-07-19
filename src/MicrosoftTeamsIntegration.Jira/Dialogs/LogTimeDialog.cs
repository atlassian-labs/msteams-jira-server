using System;
using System.Collections.Generic;
using System.Linq;
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
using MicrosoftTeamsIntegration.Jira.Models.Bot.Prompts;
using MicrosoftTeamsIntegration.Jira.Models.Jira.Issue;
using MicrosoftTeamsIntegration.Jira.Services.Interfaces;
using MicrosoftTeamsIntegration.Jira.Settings;

namespace MicrosoftTeamsIntegration.Jira.Dialogs
{
    public class LogTimeDialog : JiraIssueDependentDialog
    {
        private const string CustomOption = "Custom";

        private const string LogTimeWaterfall = "logTimeWaterfall";
        private const string TimeSlotPrompt = "timeSlotPrompt";
        private const string CustomTimeSlotPrompt = "customTimeSlotPrompt";

        private readonly string[] _timeOptions = { "30m", "1h", "2h", "8h", CustomOption };

        private readonly JiraBotAccessors _accessors;
        private readonly IJiraService _jiraService;
        private readonly AppSettings _appSettings;
        private readonly TelemetryClient _telemetry;

        public LogTimeDialog(
            JiraBotAccessors accessors,
            IJiraService jiraService,
            AppSettings appSettings,
            TelemetryClient telemetry)
            : base(nameof(LogTimeDialog), accessors, jiraService, appSettings)
        {
            _jiraService = jiraService;
            _accessors = accessors;
            _appSettings = appSettings;
            _telemetry = telemetry;

            var waterfallSteps = new WaterfallStep[]
            {
                OnPromptTimeSlotAsync,
                OnHandleSelectedTimeSlotAsync,
                OnHandleCustomTimeSlotAsync
            };
            AddDialog(new WaterfallDialog(LogTimeWaterfall, waterfallSteps));
            AddDialog(new JiraCustomPrompt(TimeSlotPrompt, TimeSlotValidatorAsync));
            AddDialog(new TextPrompt(CustomTimeSlotPrompt, CustomTimeValidatorAsync));
        }

        public async Task<DialogTurnResult> OnPromptTimeSlotAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken = default)
        {
            _telemetry.TrackPageView("LogTimeDialog");

            var message = BuildSelectTimeSlotPromptCard(stepContext.Context, "Select a time slot you would like to log.");

            return await stepContext.PromptAsync(
                TimeSlotPrompt,
                new PromptOptions
                {
                    Prompt = message
                }, cancellationToken);
        }

        public async Task<DialogTurnResult> OnHandleSelectedTimeSlotAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken = default)
        {
            var timeSpent = stepContext.Context.Activity.RemoveRecipientMention();
            if (timeSpent.HasValue())
            {
                if (string.Equals(timeSpent, CustomOption, StringComparison.OrdinalIgnoreCase))
                {
                    return await stepContext.PromptAsync(
                        CustomTimeSlotPrompt,
                        new PromptOptions
                        {
                            Prompt = MessageFactory.Text("Please enter a time duration in the following format: 2w 4d 6h 45m (w = weeks, d = days, h = hours, m = minutes).")
                        }, cancellationToken);
                }

                await AddIssueWorklog(stepContext, timeSpent);
            }
            else
            {
                await stepContext.Context.SendActivityAsync(BotMessages.OperationCancelled, cancellationToken: cancellationToken);
                return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
            }

            return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
        }

        public async Task<DialogTurnResult> OnHandleCustomTimeSlotAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken = default)
        {
            var timeSpent = stepContext.Context.Activity.RemoveRecipientMention();
            await AddIssueWorklog(stepContext, timeSpent);
            return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
        }

        protected override async Task<DialogTurnResult> ProcessJiraIssueAsync(DialogContext stepContext, IntegratedUser user, JiraIssue jiraIssue)
        {
            var textWithoutCommand =
                stepContext.Context.Activity.GetTextWithoutCommand(DialogMatchesAndCommands.LogTimeDialogCommand);

            var additionalParameter =
                Regex.Replace(textWithoutCommand, jiraIssue.Key, string.Empty, RegexOptions.IgnoreCase)
                    .NormalizeUtterance();

            if (additionalParameter.HasValue())
            {
                // user specified a work log value
                await AddIssueWorklog(stepContext, additionalParameter);
            }
            else
            {
                return await stepContext.ReplaceDialogAsync(LogTimeWaterfall);
            }

            return await stepContext.EndDialogAsync();
        }

        private async Task AddIssueWorklog(DialogContext dc, string timeSpent)
        {
            var user = await JiraBotAccessorsHelper.GetUser(_accessors, dc.Context, _appSettings);
            var jiraIssueState = await _accessors.JiraIssueState.GetAsync(dc.Context, () => new JiraIssueState());

            if (!string.IsNullOrEmpty(jiraIssueState.JiraIssue?.Key))
            {
                var response = await _jiraService.AddIssueWorklog(user, jiraIssueState.JiraIssue?.Key, timeSpent);
                if (response.IsSuccess)
                {
                    await dc.Context.SendActivityAsync("Your time has been reported.");
                }
                else
                {
                    await dc.Context.SendActivityAsync(AdjustErrorMessage(response.ErrorMessage));
                }
            }
        }

        private Activity BuildSelectTimeSlotPromptCard(ITurnContext context, string title)
        {
            var card = new HeroCard
            {
                Subtitle = title,
                Buttons = new List<CardAction>()
            };

            foreach (var option in _timeOptions)
            {
                card.Buttons.Add(new CardAction(ActionTypes.ImBack, option, value: option));
            }

            var attachments = new List<Attachment> { card.ToAttachment() };

            var message = context.Activity.CreateReply();
            message.Attachments = attachments;

            return message;
        }

        private static string AdjustErrorMessage(string errorMessage)
        {
            switch (errorMessage)
            {
                case "Worklog must not be null.":
                    return "Invalid time duration entered.";
            }

            return errorMessage;
        }

        private async Task<bool> TimeSlotValidatorAsync(JiraPromptValidatorContext<string> promptContext, CancellationToken cancellationToken)
        {
            // Check whether the input could be recognized
            if (!promptContext.Recognized.Succeeded)
            {
                await promptContext.Context.SendActivityAsync(BotMessages.PleaseRepeat, cancellationToken: cancellationToken);
                return false;
            }

            var issueName = promptContext.Context.Activity.RemoveRecipientMention();
            var timeOptions = new List<string>(_timeOptions);
            if (timeOptions.All(x => !string.Equals(x, issueName, StringComparison.OrdinalIgnoreCase)))
            {
                var message = BuildSelectTimeSlotPromptCard(promptContext.Context, BotMessages.NotValidOption);

                await promptContext.Context.SendActivityAsync(
                    message, cancellationToken);
                return false;
            }

            return true;
        }

        private static async Task<bool> CustomTimeValidatorAsync(PromptValidatorContext<string> promptContext, CancellationToken cancellationToken)
        {
            // Check whether the input could be recognized
            if (!promptContext.Recognized.Succeeded)
            {
                await promptContext.Context.SendActivityAsync(
                    BotMessages.PleaseRepeat,
                    cancellationToken: cancellationToken);
                return false;
            }

            var timeSpent = promptContext.Context.Activity.RemoveRecipientMention();
            const string pattern = @"^(\d+[dhmw]\s*){0,4}$";
            var regex = new Regex($"{pattern}", RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);
            var match = regex.Match(timeSpent);

            if (!match.Success)
            {
                await promptContext.Context.SendActivityAsync(
                    "Invalid time duration entered. Please use the format: 2w 4d 6h 45m (w = weeks, d = days, h = hours, m = minutes) or type cancel to interrupt the dialog.",
                    cancellationToken: cancellationToken);
                return false;
            }

            return true;
        }
    }
}
