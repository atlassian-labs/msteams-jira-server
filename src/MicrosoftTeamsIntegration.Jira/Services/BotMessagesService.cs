﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AdaptiveCards;
using AutoMapper;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Options;
using MicrosoftTeamsIntegration.Artifacts.Extensions;
using MicrosoftTeamsIntegration.Jira.Extensions;
using MicrosoftTeamsIntegration.Jira.Models;
using MicrosoftTeamsIntegration.Jira.Services.Interfaces;
using MicrosoftTeamsIntegration.Jira.Settings;

namespace MicrosoftTeamsIntegration.Jira.Services
{
    public class BotMessagesService : IBotMessagesService
    {
        public const string TagAPattern = "<a([^>]+)>";
        public const string AttributeHrefPattern = "(?i)\\s*https?\\s*(\"([^\"]*\")|'[^']*'|([^'\" >\\s]+))";

        private readonly AppSettings _appSettings;
        private readonly IMapper _mapper;
        private readonly IJiraService _jiraService;

        public BotMessagesService(
            IOptions<AppSettings> appSettings,
            IMapper mapper,
            IJiraService jiraService)
        {
            _appSettings = appSettings.Value;
            _mapper = mapper;
            _jiraService = jiraService;
        }

        public string HandleHtmlMessageFromUser(Activity activity)
        {
            var textData = string.IsNullOrEmpty(activity.Text) ? string.Empty : activity.Text;
            var contentData = activity.Attachments.Where(x => x.ContentType.Equals("text/html", StringComparison.OrdinalIgnoreCase)).ToList();

            if (!contentData.Any() || contentData.Count != 1)
            {
                return textData;
            }

            var tagData = Regex.Match(contentData.First().Content.ToString(), TagAPattern, RegexOptions.Compiled);
            var hrefData = Regex.Match(tagData.ToString(), AttributeHrefPattern, RegexOptions.Compiled);

            textData = hrefData.Value;
            return textData;
        }

        public async Task HandleConversationUpdates(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            var activity = turnContext.Activity;
            if (activity.MembersAdded?.Count > 0)
            {
                var connectorClient = turnContext.TurnState.Get<IConnectorClient>();
                var welcomeMessage = activity.CreateReply();
                welcomeMessage.Text = BotMessages.WelcomeMessage;

                var botWasAdded = activity.MembersAdded.Any(x => x.Id == activity.Recipient.Id);
                if (botWasAdded)
                {
                    if (!activity.IsGroupConversation())
                    {
                        await turnContext.SendActivityAsync(welcomeMessage, cancellationToken);
                    }
                    else
                    {
                        await connectorClient.Conversations.ReplyToActivityAsync(welcomeMessage, cancellationToken);
                    }
                }
            }

            if (activity.MembersRemoved?.Count > 0)
            {
                activity.MembersRemoved.Any(x => x.Id == activity.Recipient.Id);
            }
        }

        public async Task BuildAndUpdateJiraIssueCard(ITurnContext turnContext, IntegratedUser user, string issueKey)
        {
            var card = await SearchIssueAndBuildIssueCard(turnContext, user, issueKey);

            if (card != null)
            {
                var message = turnContext.Activity.CreateReply();
                message.Id = turnContext.Activity.ReplyToId;
                message.Attachments.Add(card.ToAttachment());

                await turnContext.UpdateActivityAsync(message);
            }
        }

        public async Task<AdaptiveCard> SearchIssueAndBuildIssueCard(ITurnContext turnContext, IntegratedUser user, string jiraIssueKey)
        {
            user.AccessToken = await turnContext.GetBotUserAccessToken(_appSettings.OAuthConnectionName);

            var searchRequest = SearchForIssuesRequestBase.CreateFindIssueByIdRequest(jiraIssueKey);
            var apiResponse = await _jiraService.Search(user, searchRequest);

            var isGroupConversation = turnContext.Activity.Conversation.IsGroup.GetValueOrDefault();
            var jiraIssue = apiResponse.JiraIssues.FirstOrDefault();

            if (jiraIssue is null)
            {
                return null;
            }

            jiraIssue.SetJiraIssueIconUrl(_appSettings.BaseUrl);

            var userNameOrAccountId = await _jiraService.GetUserNameOrAccountId(user);

            var card = _mapper.Map<AdaptiveCard>(
                new BotAndMessagingExtensionJiraIssue
                {
                    User = user,
                    JiraIssue = jiraIssue,
                    JiraInstanceUrl = user.JiraInstanceUrl,
                    IsGroupConversation = isGroupConversation,
                    UserNameOrAccountId = userNameOrAccountId
                },  _ => { });

            return card;
        }

        public async Task SendAuthorizationCard(ITurnContext turnContext, string jiraUrl, CancellationToken cancellationToken = default)
        {
            var tenantId = turnContext.Activity.Conversation.TenantId;

            var application = "jiraServerBot";

            var url = $"{_appSettings.BaseUrl}/#/config;application={application};tenantId={tenantId}";

            var card = new ThumbnailCard
            {
                Title = "Authorize in Jira",
                Subtitle = $"Connecting you to {jiraUrl ?? "Jira"}",
                Images = new List<CardImage>
                {
                    new CardImage("https://wac-cdn.atlassian.com/assets/img/favicons/atlassian/favicon.png")
                },
                Buttons = new List<CardAction>
                {
                    new CardAction(ActionTypes.Signin, "Authorize", value: url)
                }
            };

            var message = MessageFactory.Attachment(card.ToAttachment());

            await turnContext.SendToDirectConversationAsync(message, cancellationToken: cancellationToken);
        }
    }
}
