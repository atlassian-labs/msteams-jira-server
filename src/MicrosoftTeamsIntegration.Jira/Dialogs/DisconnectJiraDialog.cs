using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Schema.Teams;
using MicrosoftTeamsIntegration.Artifacts.Extensions;
using MicrosoftTeamsIntegration.Jira.Helpers;
using MicrosoftTeamsIntegration.Jira.Models;
using MicrosoftTeamsIntegration.Jira.Services.Interfaces;
using MicrosoftTeamsIntegration.Jira.Settings;

namespace MicrosoftTeamsIntegration.Jira.Dialogs
{
    public class DisconnectJiraDialog : ComponentDialog
    {
        private const string MainWaterfall = "disconnectJiraWaterfall";
        private const string ConfirmationPrompt = "confirmDisconnectPrompt";

        private readonly JiraBotAccessors _accessors;
        private readonly IJiraAuthService _jiraAuthService;
        private readonly AppSettings _appSettings;
        private readonly TelemetryClient _telemetry;

        public DisconnectJiraDialog(
            JiraBotAccessors accessors,
            IJiraAuthService jiraAuthService,
            AppSettings appSettings,
            TelemetryClient telemetry)
            : base(nameof(DisconnectJiraDialog))
        {
            _telemetry = telemetry;
            _accessors = accessors;
            _jiraAuthService = jiraAuthService;
            _appSettings = appSettings;

            var waterfallSteps = new WaterfallStep[]
            {
                OnDisconnectJiraRequestAsync,
                OnDisconnectJiraConfirmAsync
            };
            AddDialog(new WaterfallDialog(MainWaterfall, waterfallSteps));
            AddDialog(new ConfirmPrompt(ConfirmationPrompt));
        }

        public async Task<DialogTurnResult> OnDisconnectJiraRequestAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            _telemetry.TrackPageView("DisconnectJiraDialog");

            if (stepContext.Context.Activity.IsGroupConversation())
            {
                var conversation = await GetDirectConversation(stepContext.Context, cancellationToken);
                stepContext.Context.Activity.Conversation = new ConversationAccount(null, null, conversation.Id);
            }

            var user = await JiraBotAccessorsHelper.GetUser(_accessors, stepContext.Context, _appSettings, cancellationToken);
            var connected = await _jiraAuthService.IsJiraConnected(user);
            if (!connected)
            {
                await stepContext.Context.SendActivityAsync(
                    BotMessages.JiraDisconnectDialogNotConnected,
                    cancellationToken: cancellationToken);
                return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
            }

            return await stepContext.PromptAsync(
                ConfirmationPrompt,
                new PromptOptions
                {
                    Prompt = MessageFactory.Text(BotMessages.JiraDisconnectDialogConfirmPrompt)
                }, cancellationToken);
        }

        public async Task<DialogTurnResult> OnDisconnectJiraConfirmAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            IntegratedUser user;
            user = await JiraBotAccessorsHelper.GetUser(_accessors, stepContext.Context, _appSettings, cancellationToken);
            if (!(bool)stepContext.Result)
            {
                return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
            }

            var jiraId = user.JiraServerId;

            var result = await _jiraAuthService.Logout(user);

            if (result.IsSuccess)
            {
                await stepContext.Context.SendActivityAsync(
                    $"**You've been successfully disconnected from {jiraId}**",
                    cancellationToken: cancellationToken);
                await SendLogOutCard(stepContext, cancellationToken);
            }

            return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
        }

        private async Task SendLogOutCard(WaterfallStepContext stepContext, CancellationToken cancellationToken = default)
        {
            var url = _appSettings.BaseUrl + "/logout.html";

            var card = new ThumbnailCard
            {
                Subtitle = BotMessages.JiraDisconnectAtlassianLogOut,
                Images = new List<CardImage>
                {
                    new CardImage("https://wac-cdn.atlassian.com/assets/img/favicons/atlassian/favicon.png")
                },
                Buttons = new List<CardAction>
                {
                    new CardAction(ActionTypes.Signin, "Log out", value: url)
                }
            };

            var attachments = new List<Attachment> { card.ToAttachment() };
            var message = stepContext.Context.Activity.CreateReply();
            message.Attachments = attachments;
            await stepContext.Context.SendActivityAsync(message, cancellationToken);
        }

        private async Task<ConversationAccount> GetDirectConversation(ITurnContext context, CancellationToken cancellationToken = default)
        {
            var contextActivity = context.Activity;

            var client = context.TurnState.Get<IConnectorClient>();

            var parameters = new ConversationParameters
            {
                Bot = contextActivity.Recipient,
                Members = new[] { contextActivity.From },
                ChannelData = new TeamsChannelData
                {
                    Tenant = new TenantInfo
                    {
                        Id = contextActivity.Conversation.TenantId
                    }
                }
            };
            var conversation = await client.Conversations.CreateConversationAsync(parameters, cancellationToken);

            return new ConversationAccount(null, null, conversation.Id);
        }
    }
}
