using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AdaptiveCards;
using JetBrains.Annotations;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Options;
using MicrosoftTeamsIntegration.Artifacts.Extensions;
using MicrosoftTeamsIntegration.Jira.Dialogs;
using MicrosoftTeamsIntegration.Jira.Extensions;
using MicrosoftTeamsIntegration.Jira.Helpers;
using MicrosoftTeamsIntegration.Jira.Models;
using MicrosoftTeamsIntegration.Jira.Services.Interfaces;
using MicrosoftTeamsIntegration.Jira.Settings;
using Newtonsoft.Json;

namespace MicrosoftTeamsIntegration.Jira.Services
{
    [UsedImplicitly]
    public sealed class ActionableMessageService : IActionableMessageService
    {
        private readonly IJiraService _jiraService;
        private readonly AppSettings _appSettings;

        public ActionableMessageService(
            IJiraService jiraService,
            IOptions<AppSettings> appSettings)
        {
            _jiraService = jiraService;
            _appSettings = appSettings.Value;
        }

        public async Task<bool> HandleConnectorCardActionQuery(ITurnContext context, IntegratedUser user)
        {
            var activity = context.Activity;
            if (user == null)
            {
                var attachments = new List<Attachment>
                {
                    new HeroCard("In order to use bot, please connect it first and rerun the command.")
                    {
                        Buttons = new List<CardAction>
                        {
                            new CardAction(ActionTypes.ImBack, "Connect", value: "connect")
                        }
                    }.ToAttachment()
                };

                var message = activity.CreateReply();
                message.Attachments = attachments;

                await context.SendToDirectConversationAsync(message);

                return false;
            }

            return await ProcessCardActionQuery(context, user);
        }

        public async Task HandleSuccessfulConnection(ITurnContext context)
        {
            var adaptiveCard = new AdaptiveCard(new AdaptiveSchemaVersion(1, 3))
            {
                Body = new List<AdaptiveElement>
                {
                    new AdaptiveColumnSet
                    {
                        Columns = new List<AdaptiveColumn>
                        {
                            new AdaptiveColumn
                            {
                                Width = "stretch",
                                Items = new List<AdaptiveElement>
                                {
                                    new AdaptiveColumnSet
                                    {
                                        Columns = new List<AdaptiveColumn>
                                        {
                                            new AdaptiveColumn
                                            {
                                                Width = "auto",
                                                Items = new List<AdaptiveElement>
                                                {
                                                    new AdaptiveImage
                                                    {
                                                        Url = new Uri(
                                                            "https://product-integrations-cdn.atl-paas.net/celebration.png"),
                                                        Size = AdaptiveImageSize.Small
                                                    }
                                                },
                                                Spacing = AdaptiveSpacing.Small,
                                                VerticalContentAlignment = AdaptiveVerticalContentAlignment.Center
                                            },
                                            new AdaptiveColumn
                                            {
                                                Width = "auto",
                                                VerticalContentAlignment = AdaptiveVerticalContentAlignment.Center,
                                                Items = new List<AdaptiveElement>
                                                {
                                                    new AdaptiveTextBlock
                                                    {
                                                        Size = AdaptiveTextSize.Medium,
                                                        Weight = AdaptiveTextWeight.Bolder,
                                                        Text = "You have been successfully connected to Jira.",
                                                        Wrap = true
                                                    }
                                                },
                                                Spacing = AdaptiveSpacing.Small
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    },
                    new AdaptiveTextBlock
                    {
                        Text = "Click 'Help' to explore commands.",
                        Wrap = true
                    },
                    new AdaptiveColumnSet
                    {
                        Columns = new List<AdaptiveColumn>
                        {
                            new AdaptiveColumn
                            {
                                Width = "auto",
                                Items = new List<AdaptiveElement>
                                {
                                    new AdaptiveActionSet
                                    {
                                        Actions = new List<AdaptiveAction>
                                        {
                                            new AdaptiveSubmitAction()
                                            {
                                                Title = "Help",
                                                Data = new
                                                {
                                                    msteams = new
                                                    {
                                                        type = "messageBack",
                                                        text = $"{DialogMatchesAndCommands.HelpDialogCommand}"
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            },
                            new AdaptiveColumn
                            {
                                Width = "auto",
                                Items = new List<AdaptiveElement>
                                {
                                    new AdaptiveActionSet
                                    {
                                        Actions = new List<AdaptiveAction>
                                        {
                                            new AdaptiveSubmitAction()
                                            {
                                                Title = "Disconnect",
                                                Data = new
                                                {
                                                    msteams = new
                                                    {
                                                        type = "messageBack",
                                                        text = $"{DialogMatchesAndCommands.DisconnectJiraDialogCommand}"
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                },
            };

            await context.SendActivityAsync(MessageFactory.Attachment(adaptiveCard.ToAttachment()));
        }

        private async Task<bool> ProcessCardActionQuery(ITurnContext context, IntegratedUser user)
        {
            var activity = context.Activity;
            var cardActionQuery = activity.GetO365ConnectorCardActionQueryData();
            if (cardActionQuery == null)
            {
                return false;
            }

            var actionId = cardActionQuery.ActionId
                .Substring(DialogMatchesAndCommands.O365ConnectorCardPostActionPrefix.Length).ToLowerInvariant();
            var actionData = JsonConvert.DeserializeObject<O365ConnectorCardHttpPostBody>(cardActionQuery.Body);

            user.AccessToken = await context.GetBotUserAccessToken(_appSettings.OAuthConnectionName);

            var result = new JiraApiActionCallResponse();
            switch (actionId)
            {
                case DialogMatchesAndCommands.CommentDialogCommand:
                    result = await _jiraService.AddComment(user, actionData.JiraIssueKey, actionData.Value);
                    break;
                case DialogMatchesAndCommands.PriorityDialogCommand:
                    result = await _jiraService.UpdatePriority(user, actionData.JiraIssueKey, actionData.Value);
                    break;
                case DialogMatchesAndCommands.SummaryDialogCommand:
                    result = await _jiraService.UpdateSummary(user, actionData.JiraIssueKey, actionData.Value);
                    break;
                case DialogMatchesAndCommands.DescriptionDialogCommand:
                    result = await _jiraService.UpdateDescription(user, actionData.JiraIssueKey, actionData.Value);
                    break;
            }

            var message = Activity.CreateMessageActivity();
            var client = context.TurnState.Get<IConnectorClient>();
            if (result.IsSuccess)
            {
                var searchRequest = SearchForIssuesRequestBase.CreateFindIssueByIdRequest(actionData.JiraIssueKey);
                var apiResponse = await _jiraService.Search(user, searchRequest);

                var priorities = await _jiraService.GetPriorities(user);
                var jiraIssue = apiResponse.JiraIssues.FirstOrDefault();

                jiraIssue?.SetJiraIssueIconUrl(_appSettings.BaseUrl);
                jiraIssue?.SetJiraIssuePriorityIconUrl(_appSettings.BaseUrl);

                var updatedCard =
                    JiraIssueSearchHelper.CreateO365ConnectorCardFromApiResponse(apiResponse, user, priorities);
                message.Attachments.Add(updatedCard.ToAttachment());

                await client.Conversations.UpdateActivityAsync(activity.Conversation.Id, activity.ReplyToId, (Activity)message);
            }
            else
            {
                message.Text = result.ErrorMessage;
                await context.SendToDirectConversationAsync(message);
            }

            return result.IsSuccess;
        }
    }
}
