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
using MicrosoftTeamsIntegration.Jira.Dialogs;
using MicrosoftTeamsIntegration.Jira.Extensions;
using MicrosoftTeamsIntegration.Jira.Models;
using MicrosoftTeamsIntegration.Jira.Models.Bot;
using MicrosoftTeamsIntegration.Jira.Models.FetchTask;
using MicrosoftTeamsIntegration.Jira.Models.Notifications;
using MicrosoftTeamsIntegration.Jira.Services.Interfaces;
using MicrosoftTeamsIntegration.Jira.Settings;

namespace MicrosoftTeamsIntegration.Jira.Services
{
    public class BotMessagesService : IBotMessagesService
    {
        private const string TagAPattern = "<a([^>]+)>";
        private const string AttributeHrefPattern = "(?i)\\s*https?\\s*(\"([^\"]*\")|'[^']*'|([^'\" >\\s]+))";

        private readonly AppSettings _appSettings;
        private readonly IMapper _mapper;
        private readonly IJiraService _jiraService;
        private readonly IAnalyticsService _analyticsService;

        public BotMessagesService(
            IOptions<AppSettings> appSettings,
            IMapper mapper,
            IJiraService jiraService,
            IAnalyticsService analyticsService)
        {
            _appSettings = appSettings.Value;
            _mapper = mapper;
            _jiraService = jiraService;
            _analyticsService = analyticsService;
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
                    _analyticsService.SendTrackEvent(
                        turnContext.Activity?.From?.AadObjectId,
                        "bot",
                        "installed",
                        "botApplication",
                        string.Empty,
                        new InstallationUpdatesTrackEventAttributes()
                        {
                            ConversationType = activity.Conversation.ConversationType
                        });
                }

                if (activity.MembersRemoved?.Count > 0)
                {
                    var botWasRemoved = activity.MembersRemoved.Any(x => x.Id == activity.Recipient.Id);
                    if (botWasRemoved)
                    {
                        _analyticsService.SendTrackEvent(
                            turnContext.Activity?.From?.AadObjectId,
                            "bot",
                            "uninstalled",
                            "botApplication",
                            string.Empty,
                            new InstallationUpdatesTrackEventAttributes()
                            {
                                ConversationType = activity.Conversation.ConversationType
                            });
                    }
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

        public async Task SendConnectCard(ITurnContext turnContext, CancellationToken cancellationToken = default)
        {
            var message = MessageFactory.Attachment(new HeroCard("In order to use bot, please connect it first and rerun the command.")
            {
                Buttons = new List<CardAction>
                {
                    new CardAction(ActionTypes.ImBack, "Connect", value: "connect")
                }
            }.ToAttachment());

            await turnContext.SendToDirectConversationAsync(message, cancellationToken: cancellationToken);
        }

        public async Task SendConfigureNotificationsCard(ITurnContext turnContext, CancellationToken cancellationToken = default)
        {
            var adaptiveCard = BuildConfigureNotificationsCard(turnContext);

            var message = MessageFactory.Attachment(adaptiveCard.ToAttachment());

            if (turnContext.Activity.IsGroupConversation())
            {
                await turnContext.SendActivityAsync(message, cancellationToken: cancellationToken);
            }
            else
            {
                await turnContext.SendToDirectConversationAsync(message, cancellationToken: cancellationToken);
            }
        }

        public AdaptiveCard BuildConfigureNotificationsCard(ITurnContext turnContext)
        {
            bool isGroupConversation = turnContext.Activity.IsGroupConversation();

            string title = isGroupConversation ? "🔔 Channel notifications" : "🔔 Personal notifications";
            string turnOnCommandName = isGroupConversation
                ? DialogMatchesAndCommands.TurnOnChannelNotificationsCommand
                : DialogMatchesAndCommands.TurnOnNotificationsCommand;

            var adaptiveCard = new AdaptiveCard(new AdaptiveSchemaVersion(1, 3))
            {
                Body = new List<AdaptiveElement>()
                {
                    new AdaptiveTextBlock
                    {
                        Text = title,
                        Size = AdaptiveTextSize.Medium,
                        Weight = AdaptiveTextWeight.Bolder,
                        Wrap = true
                    },
                    new AdaptiveTextBlock
                    {
                        Text = isGroupConversation
                                ? "Manage your project notifications for this channel. I’ll send instant updates to keep your team in sync."
                                : "Turn on personal notifications to stay updated across your projects in Jira Data Center without the distraction of email notifications.",
                        Wrap = true
                    }
                },
                Actions = new List<AdaptiveAction>()
                {
                    new AdaptiveSubmitAction
                    {
                        Title = isGroupConversation ? "Manage notifications" : "Turn on notifications",
                        Style = "positive",
                        Data = new JiraBotTeamsDataWrapper
                        {
                            FetchTaskData = new FetchTaskBotCommand(turnOnCommandName),
                            TeamsData = new TeamsData
                            {
                                Type = "task/fetch"
                            }
                        }
                    }
                }
            };
            return adaptiveCard;
        }

        public async Task SendNotificationCard(
            ITurnContext turnContext,
            NotificationMessage notificationMessage,
            CancellationToken cancellationToken = default)
        {
            var adaptiveCard = _mapper.Map<AdaptiveCard>(notificationMessage);

            var message = MessageFactory.Attachment(adaptiveCard.ToAttachment());

            await turnContext.SendToDirectConversationAsync(message, cancellationToken: cancellationToken);
        }

        public AdaptiveCard BuildNotificationConfigurationSummaryCard(NotificationSubscription subscription, bool showSuccessMessage = false)
        {
            List<PersonalEventType> eventTypes
                = subscription.EventTypes.AsEnumerable().Select(x
                    => Enum.Parse<PersonalEventType>(x)).ToList();

            var adaptiveCard = new AdaptiveCard(new AdaptiveSchemaVersion(1, 4));

            adaptiveCard.Body = new List<AdaptiveElement>()
            {
                new AdaptiveContainer()
                {
                    Items = new List<AdaptiveElement>()
                    {
                        new AdaptiveTextBlock()
                        {
                            Text = "You successfully subscribed to notifications from Jira \ud83e\udd73",
                            IsVisible = showSuccessMessage
                        },
                        new AdaptiveTextBlock()
                        {
                            Text = $"{(showSuccessMessage ? string.Empty : "\ud83d\udd14 ")}" +
                                   $"You will receive notifications when there are:"
                        },
                        new AdaptiveTextBlock()
                        {
                            Text = "* Updates on issues you **assigned** to",
                            IsVisible = eventTypes.Contains(PersonalEventType.ActivityIssueAssignee)
                        },
                        new AdaptiveTextBlock()
                        {
                            Text = "* Comments on issues you **assigned** to",
                            IsVisible = eventTypes.Contains(PersonalEventType.CommentIssueAssignee)
                        },
                        new AdaptiveTextBlock()
                        {
                            Text = "* Updates on issues you've **reported**",
                            IsVisible = eventTypes.Contains(PersonalEventType.ActivityIssueCreator)
                        },
                        new AdaptiveTextBlock()
                        {
                            Text = "* Comments on issues you've **reported**",
                            IsVisible = eventTypes.Contains(PersonalEventType.CommentIssueCreator)
                        },
                        new AdaptiveTextBlock()
                        {
                            Text = "* Updates on issues that you are **watching**",
                            IsVisible = eventTypes.Contains(PersonalEventType.IssueViewer)
                        },
                        new AdaptiveTextBlock()
                        {
                            Text = "* Comments on issues that you are **watching**",
                            IsVisible = eventTypes.Contains(PersonalEventType.CommentViewer)
                        },
                        new AdaptiveTextBlock()
                        {
                            Text = "* Someone **mentioned** you in a comment or issue",
                            IsVisible = eventTypes.Contains(PersonalEventType.MentionedOnIssue)
                        }
                    }
                }
            };

            adaptiveCard.AdditionalProperties = new SerializableDictionary<string, object>
            {
                { "msTeams", new { width = "full" } }
            };
            adaptiveCard.Actions = new List<AdaptiveAction>()
            {
                new AdaptiveSubmitAction
                {
                    Title = "Change notifications",
                    Style = "positive",
                    Data = new JiraBotTeamsDataWrapper
                    {
                        FetchTaskData = new FetchTaskBotCommand(DialogMatchesAndCommands.TurnOnNotificationsCommand),
                        TeamsData = new TeamsData
                        {
                            Type = "task/fetch"
                        }
                    }
                }
            };

            return adaptiveCard;
        }

        public AdaptiveCard BuildChannelNotificationConfigurationSummaryCard(
            NotificationSubscriptionEvent subscriptionEvent,
            string callerName)
        {
            string title = string.Empty;

            List<ChannelEventType> eventTypes
                = subscriptionEvent.Subscription.EventTypes.AsEnumerable().Select(x
                    => Enum.Parse<ChannelEventType>(x)).ToList();

            bool showEventListMessage = subscriptionEvent.Action != SubscriptionAction.Deleted
                                        && subscriptionEvent.Action != SubscriptionAction.Disabled
                                        && eventTypes.Count > 0;

            switch (subscriptionEvent.Action)
            {
                case SubscriptionAction.Created:
                    title = $"**{callerName}** has set up channel notifications for **{subscriptionEvent.Subscription.ProjectName}** project";
                    break;
                case SubscriptionAction.Updated:
                    title = $"**{callerName}** has updated channel notifications for **{subscriptionEvent.Subscription.ProjectName}** project";
                    break;
                case SubscriptionAction.Deleted:
                    title = $"**{callerName}** has removed channel notifications for **{subscriptionEvent.Subscription.ProjectName}** project";
                    break;
                case SubscriptionAction.Enabled:
                    title = $"**{callerName}** has enabled channel notifications for **{subscriptionEvent.Subscription.ProjectName}** project";
                    break;
                case SubscriptionAction.Disabled:
                    title = $"**{callerName}** has disabled channel notifications for **{subscriptionEvent.Subscription.ProjectName}** project";
                    break;
            }

            var adaptiveCard = new AdaptiveCard(new AdaptiveSchemaVersion(1, 4));

            adaptiveCard.Body = new List<AdaptiveElement>()
            {
                new AdaptiveContainer()
                {
                    Items = new List<AdaptiveElement>()
                    {
                        new AdaptiveTextBlock()
                        {
                            Text = title,
                            IsVisible = true
                        },
                        new AdaptiveTextBlock()
                        {
                            Text = $"You will now get a message when someone:",
                            IsVisible = showEventListMessage
                        },
                        new AdaptiveTextBlock()
                        {
                            Text = "* **Created comment** on issue",
                            IsVisible = eventTypes.Contains(ChannelEventType.CommentCreated)
                                      && showEventListMessage
                        },
                        new AdaptiveTextBlock()
                        {
                            Text = "* **Updated comment** on issue",
                            IsVisible = eventTypes.Contains(ChannelEventType.CommentUpdated)
                                        && showEventListMessage
                        },
                        new AdaptiveTextBlock()
                        {
                            Text = "* **Created issue**",
                            IsVisible = eventTypes.Contains(ChannelEventType.IssueCreated)
                                        && showEventListMessage
                        },
                        new AdaptiveTextBlock()
                        {
                            Text = "* **Updated issue**",
                            IsVisible = eventTypes.Contains(ChannelEventType.IssueUpdated)
                                        && showEventListMessage
                        },
                    }
                }
            };

            adaptiveCard.AdditionalProperties = new SerializableDictionary<string, object>
            {
                { "msTeams", new { width = "full" } }
            };
            adaptiveCard.Actions = new List<AdaptiveAction>()
            {
                new AdaptiveSubmitAction
                {
                    Title = "Manage notifications",
                    Style = "positive",
                    Data = new JiraBotTeamsDataWrapper
                    {
                        FetchTaskData = new FetchTaskBotCommand(DialogMatchesAndCommands.TurnOnChannelNotificationsCommand),
                        TeamsData = new TeamsData
                        {
                            Type = "task/fetch"
                        }
                    }
                }
            };

            return adaptiveCard;
        }

        public AdaptiveCard BuildHelpCard(ITurnContext turnContext)
        {
            var isGroup = turnContext.Activity.IsGroupConversation();
            var adaptiveCard = new AdaptiveCard(new AdaptiveSchemaVersion(1, 4));

            adaptiveCard.Body = new List<AdaptiveElement>()
            {
                new AdaptiveContainer()
                {
                    Items = new List<AdaptiveElement>()
                    {
                        new AdaptiveTextBlock()
                        {
                            Text = "How I can help you?",
                            Size = AdaptiveTextSize.Large,
                            Weight = AdaptiveTextWeight.Bolder
                        },
                        new AdaptiveTextBlock()
                        {
                            Text = "Here’s a list of the commands I can process:",
                            Wrap = true
                        },
                        CreateActionColumnSet(
                            "Connect",
                            "Connect a Jira Data Center instance to your Microsoft Teams account",
                            DialogMatchesAndCommands.ConnectToJiraDialogCommand,
                            !isGroup,
                            false),
                        CreateActionColumnSet(
                            "Create",
                            "Create a new issue",
                            DialogMatchesAndCommands.CreateNewIssueDialogCommand,
                            !isGroup,
                            true),
                        CreateActionColumnSet(
                            "Notifications",
                            $"Set up {(isGroup ? "channel" : "personal")} notifications here in Teams",
                            DialogMatchesAndCommands.TurnOnNotificationsCommand,
                            true,
                            true),
                        CreateActionColumnSet(
                            "Find",
                            $"Obtain issue(s) by a summary search phrase or an issue key (e.g. **{DialogMatchesAndCommands.FindDialogCommand} MP-47** or **{DialogMatchesAndCommands.FindDialogCommand} search_phrase**)",
                            DialogMatchesAndCommands.FindDialogCommand,
                            !isGroup,
                            false),
                        CreateActionColumnSet(
                            "Assign",
                            $"Assign the issue to yourself (e.g. **{DialogMatchesAndCommands.AssignDialogCommand} MP-47**)",
                            DialogMatchesAndCommands.AssignDialogCommand,
                            !isGroup,
                            false),
                        CreateActionColumnSet(
                            "Edit",
                            "Open the issue card to change priority, summary, and description of the issue",
                            DialogMatchesAndCommands.IssueEditDialogCommand,
                            !isGroup,
                            false),
                        CreateActionColumnSet(
                            "Log",
                            "Log time spent on the issue",
                            DialogMatchesAndCommands.LogTimeDialogCommand,
                            !isGroup,
                            false),
                        CreateActionColumnSet(
                            "Watch",
                            $"Start watching the issue (e.g. **{DialogMatchesAndCommands.WatchDialogCommand} MP-47**)",
                            DialogMatchesAndCommands.WatchDialogCommand,
                            true,
                            false),
                        CreateActionColumnSet(
                            "Unwatch",
                            $"Stop watching the issue (e.g. **{DialogMatchesAndCommands.UnwatchDialogCommand} MP-47**)",
                            DialogMatchesAndCommands.UnwatchDialogCommand,
                            true,
                            false),
                        CreateActionColumnSet(
                            "Vote",
                            "Vote on the issue",
                            DialogMatchesAndCommands.VoteDialogCommand,
                            true,
                            false),
                        CreateActionColumnSet(
                            "Unvote",
                            "Unvote on the issue",
                            DialogMatchesAndCommands.UnvoteDialogCommand,
                            true,
                            false),
                        CreateActionColumnSet(
                            "Comment",
                            "Comment the issue",
                            DialogMatchesAndCommands.CommentDialogCommand,
                            true,
                            false),
                        CreateActionColumnSet(
                            "Disconnect",
                            $"Disconnect Jira Data Center instance you've connected from Microsoft Teams",
                            DialogMatchesAndCommands.DisconnectJiraDialogCommand,
                            !isGroup,
                            false),
                        CreateActionColumnSet(
                            "Cancel",
                            "cancel current dialog",
                            DialogMatchesAndCommands.CancelCommand,
                            true,
                            false),
                        new AdaptiveTextBlock()
                        {
                            IsVisible = !isGroup,
                            Wrap = true,
                            Text = "Type an issue key (e.g. **MP-47**) to view issue card with actions"
                        },
                        new AdaptiveTextBlock()
                        {
                            Separator = true,
                            Wrap = true,
                            Text =
                                "\u24d8 For detailed instructions on configuring Jira Data Center application, please visit our [help page](https://confluence.atlassian.com/msteamsjiraserver/microsoft-teams-for-jira-server-documentation-1027116656.html)."
                        }
                    }
                }
            };
            adaptiveCard.AdditionalProperties = new SerializableDictionary<string, object>
            {
                { "msTeams", new { width = "full" } }
            };

            return adaptiveCard;
        }

        private static async Task SendWelcomeCard(ITurnContext turnContext, IConnectorClient connectorClient, Activity activity, bool isGroupConversation, CancellationToken cancellationToken)
        {
            string welcomeText =
                "- **View your work** in a tab via a Jira filter or see issues that are assigned to, reported by " +
                "or watched by you.\n- **Search** for Jira issues right within message extension or bot command\n- " +
                "**Update issues** or **add comments** right in your conversation with a bot so you can focus on your " +
                "work and avoid context switching between your web browser and Teams";
            if (isGroupConversation)
            {
                welcomeText =
                    "- Add a Jira filter within a **tab** in a chat or team channel to easily reference issues" +
                    " within your projects\n- **Search** for Jira issues right within a Teams chat in the message" +
                    " extension and send it as card to the chat\n- **Update issues** or **add comments** right in" +
                    " your conversation so you can focus on your work and avoid context switching between your web " +
                    "browser and Teams\n- **Add messages as comments** to update issues right when collaboration is " +
                    "happening\n- **Create new issues** from messages within a chat";
            }

            string installAddonMessage =
                "To use messaging extension, bot, or tabs, you'll need to install the " +
                "**[Microsoft Teams for Jira Data Center](https://marketplace.atlassian.com/apps/1217836?tab=overview&hosting=datacenter)** app to your Jira Data Center. " +
                "Ensure you have admin permissions within your Jira instance to install and configure the app. " +
                "If you don't have admin permissions, contact your Jira admin.\n\n" +
                "When the app is installed, it'll generate and assign a unique " +
                "Jira ID to your Jira Data Center instance. Share the generated Jira ID" +
                " with the team so your teammates can connect Microsoft Teams to Jira.\n\n" +
                "For detailed instructions on configuring Jira Data Center for Teams app, please visit our [help page](https://confluence.atlassian.com/msteamsjiraserver/microsoft-teams-for-jira-server-documentation-1027116656.html).";

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
                        new AdaptiveTextBlock
                        {
                            Size = AdaptiveTextSize.Medium,
                            Weight = AdaptiveTextWeight.Bolder,
                            Text = "Important:"
                        },
                        new AdaptiveTextBlock
                        {
                            Text = installAddonMessage,
                            Wrap = true
                        },
                        new AdaptiveRichTextBlock
                        {
                            Inlines = new List<AdaptiveInline>
                            {
                                new AdaptiveTextRun
                                {
                                    Text =
                                        "Ready to get started? Connect your Jira account!\nWant to learn more about " +
                                        "this application? Click 'Help' button."
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

        private static AdaptiveColumnSet CreateActionColumnSet(string title, string description, string command, bool isVisible, bool isFetchTask)
        {
            AdaptiveActionSet adaptiveActionSet = isFetchTask
                ? new AdaptiveActionSet()
                {
                    Actions = new List<AdaptiveAction>()
                    {
                        new AdaptiveSubmitAction()
                        {
                            Title = title,
                            Data = new JiraBotTeamsDataWrapper
                            {
                                FetchTaskData = new FetchTaskBotCommand(command),
                                TeamsData = new TeamsData
                                {
                                    Type = "task/fetch"
                                }
                            }
                        }
                    }
                }
                : new AdaptiveActionSet()
                {
                    Actions = new List<AdaptiveAction>()
                    {
                        new AdaptiveSubmitAction()
                        {
                            Title = title,
                            Data = new
                            {
                                msteams = new
                                {
                                    type = "messageBack",
                                    text = command
                                }
                            }
                        }
                    }
                };

            return new AdaptiveColumnSet()
            {
                IsVisible = isVisible,
                Columns = new List<AdaptiveColumn>()
                {
                    new AdaptiveColumn()
                    {
                        Width = "120px",
                        Items = new List<AdaptiveElement>()
                        {
                            adaptiveActionSet
                        }
                    },
                    new AdaptiveColumn()
                    {
                        Width = "stretch",
                        VerticalContentAlignment = AdaptiveVerticalContentAlignment.Center,
                        Items = new List<AdaptiveElement>()
                        {
                            new AdaptiveTextBlock()
                            {
                                Text = description,
                                Wrap = true
                            }
                        }
                    }
                }
            };
        }
    }
}
