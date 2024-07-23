using System;
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
                var welcomeActivity = activity.CreateReply();

                var botWasAdded = activity.MembersAdded.Any(x => x.Id == activity.Recipient.Id);
                if (botWasAdded)
                {
                    await SendWelcomeCard(turnContext, connectorClient, welcomeActivity, activity.IsGroupConversation(), cancellationToken);
                }
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

            jiraIssue.SetJiraIssueIconUrl();

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
                                                            "https://product-integrations-cdn.atl-paas.net/atlassian-logo-gradient-blue-large.png"),
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
                                                        Text = "Authorize in Jira",
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
                        Text = $"Click 'Authorize' to link your {jiraUrl ?? "Jira"} account with Microsoft Teams",
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
                                                Title = "Authorize",
                                                Data = new
                                                {
                                                    msteams = new
                                                    {
                                                        type = "signin",
                                                        value = url
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            },
                        }
                    }
                }
            };

            var message = MessageFactory.Attachment(adaptiveCard.ToAttachment());

            await turnContext.SendToDirectConversationAsync(message, cancellationToken: cancellationToken);
        }

        private static async Task SendWelcomeCard(ITurnContext turnContext, IConnectorClient connectorClient, Activity activity, bool isGroupConversation, CancellationToken cancellationToken)
        {
            string welcomeText =
                "- **View your work** in a tab via a Jira filter or see issues that are assigned to, reported by or watched by you.\n- **Search** for Jira issues right within message extension or bot command\n- **Update issues** or **add comments** right in your conversation with a bot so you can focus on your work and avoid context switching between your web browser and Teams";
            if (isGroupConversation)
            {
                welcomeText =
                    "- Add a Jira filter within a **tab** in a chat or team channel to easily reference issues within your projects\n- **Search** for Jira issues right within a Teams chat in the message extension and send it as card to the chat\n- **Update issues** or **add comments** right in your conversation so you can focus on your work and avoid context switching between your web browser and Teams\n- **Add messages as comments** to update issues right when collaboration is happening\n- **Create new issues** from messages within a chat";
            }

            var adaptiveCard = new AdaptiveCard(new AdaptiveSchemaVersion(1, 5));
            adaptiveCard.AdditionalProperties = new SerializableDictionary<string, object>
            {
                {
                    "msTeams", new
                    {
                        width = "full",
                    }
                }
            };
            adaptiveCard.Body = new List<AdaptiveElement>
            {
                new AdaptiveTextBlock
                {
                    Size = AdaptiveTextSize.Large,
                    Weight = AdaptiveTextWeight.Bolder,
                    Text = "Get started using Jira Data Center"
                },
                new AdaptiveTextBlock
                {
                    Text = "Here are some of the things you can do:",
                    Wrap = true
                },
                new AdaptiveTextBlock
                {
                    Text = welcomeText,
                    Wrap = true
                },
            };

            if (!isGroupConversation)
            {
                adaptiveCard.Body.Add(new AdaptiveContainer
                {
                    Items = new List<AdaptiveElement>
                    {
                        new AdaptiveRichTextBlock
                        {
                            Inlines = new List<AdaptiveInline>
                            {
                                new AdaptiveTextRun
                                {
                                    Text =
                                        "Ready to get started? Connect your Jira account!\nWant to learn more about this application? Click 'Help' button."
                                }
                            }
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
                                                new AdaptiveSubmitAction
                                                {
                                                    Title = "Connect",
                                                    Data = new
                                                    {
                                                        msteams = new
                                                        {
                                                            type = "messageBack",
                                                            text = "connect"
                                                        }
                                                    }
                                                },
                                                new AdaptiveSubmitAction
                                                {
                                                    Title = "Help",
                                                    Data = new
                                                    {
                                                        msteams = new
                                                        {
                                                            type = "messageBack",
                                                            text = "help"
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                });
            }

            if (isGroupConversation)
            {
                activity.Attachments = new List<Attachment> { adaptiveCard.ToAttachment() };
                await connectorClient.Conversations.ReplyToActivityAsync(activity, cancellationToken);
            }
            else
            {
                await turnContext.SendActivityAsync(MessageFactory.Attachment(adaptiveCard.ToAttachment()), cancellationToken);
            }
        }
    }
}
