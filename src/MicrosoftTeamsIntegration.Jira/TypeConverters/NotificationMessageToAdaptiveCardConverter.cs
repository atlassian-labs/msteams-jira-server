using System;
using System.Collections.Generic;
using System.Linq;
using AdaptiveCards;
using AutoMapper;
using MicrosoftTeamsIntegration.Jira.Dialogs;
using MicrosoftTeamsIntegration.Jira.Models.Bot;
using MicrosoftTeamsIntegration.Jira.Models.FetchTask;
using MicrosoftTeamsIntegration.Jira.Models.Notifications;

namespace MicrosoftTeamsIntegration.Jira.TypeConverters;

public class NotificationMessageToAdaptiveCardConverter : ITypeConverter<NotificationMessage, AdaptiveCard>
{
    private const string IssueCreated = "ISSUE_CREATED";
    private const string CommentCreated = "COMMENT_CREATED";
    private const string CommentUpdated = "COMMENT_UPDATED";
    private const string UnknownUserIconUrl = "https://product-integrations-cdn.atl-paas.net/icons/unknown-user.png";

    public AdaptiveCard Convert(NotificationMessage source, AdaptiveCard destination, ResolutionContext context)
    {
        var adaptiveCard = new AdaptiveCard(new AdaptiveSchemaVersion(1, 4));

        adaptiveCard.Body = new List<AdaptiveElement>
        {
            new AdaptiveContainer
            {
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
                                        Url = new Uri(source.User.AvatarUrl?.ToString() ?? UnknownUserIconUrl),
                                        Size = AdaptiveImageSize.Small,
                                        Height = new AdaptiveHeight(32),
                                        PixelWidth = 32,
                                        HorizontalAlignment = AdaptiveHorizontalAlignment.Right,
                                        Style = AdaptiveImageStyle.Person
                                    }
                                },
                                VerticalContentAlignment = AdaptiveVerticalContentAlignment.Center
                            },
                            new AdaptiveColumn
                            {
                                Width = "stretch",
                                Items = new List<AdaptiveElement>
                                {
                                    new AdaptiveTextBlock
                                    {
                                        Text = BuildNotificationTitleMessage(source),
                                        Size = AdaptiveTextSize.Large,
                                        Wrap = true
                                    }
                                },
                                VerticalContentAlignment = AdaptiveVerticalContentAlignment.Center
                            }
                        }
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
                            new AdaptiveImage
                            {
                                Url =
                                    new Uri(
                                        $"https://product-integrations-cdn.atl-paas.net/jira-issuetype/medium/{source.Issue.Type.ToLower()}.png"),
                                Size = AdaptiveImageSize.Small,
                                PixelWidth = 24,
                                HorizontalAlignment = AdaptiveHorizontalAlignment.Right
                            }
                        },
                        VerticalContentAlignment = AdaptiveVerticalContentAlignment.Center
                    },
                    new AdaptiveColumn
                    {
                        Width = "80",
                        Items = new List<AdaptiveElement>
                        {
                            new AdaptiveTextBlock
                            {
                                Text = $"{source.Issue.Key} - {source.Issue.Summary}",
                                Size = AdaptiveTextSize.Medium,
                                Wrap = true
                            }
                        },
                        VerticalContentAlignment = AdaptiveVerticalContentAlignment.Center
                    },
                    new AdaptiveColumn
                    {
                        Width = "20",
                        Items = new List<AdaptiveElement>
                        {
                            new AdaptiveRichTextBlock
                            {
                                Inlines = new List<AdaptiveInline>
                                {
                                    new AdaptiveTextRun
                                    {
                                        Text = source.Issue.Status.ToUpper(),
                                        Weight = AdaptiveTextWeight.Bolder
                                    }
                                }
                            }
                        },
                        VerticalContentAlignment = AdaptiveVerticalContentAlignment.Center
                    },
                    new AdaptiveColumn
                    {
                        Width = "auto",
                        Items = new List<AdaptiveElement>
                        {
                            new AdaptiveImage
                            {
                                Url = new Uri(source.Issue.Assignee?.AvatarUrl?.ToString() ?? UnknownUserIconUrl),
                                Size = AdaptiveImageSize.Small,
                                Style = AdaptiveImageStyle.Person,
                                HorizontalAlignment = AdaptiveHorizontalAlignment.Right
                            }
                        },
                        VerticalContentAlignment = AdaptiveVerticalContentAlignment.Center
                    }
                },
                Separator = true,
                Spacing = AdaptiveSpacing.ExtraLarge
            }
        };

        List<AdaptiveAction> actions = new List<AdaptiveAction>();

        List<AdaptiveColumnSet> changeLogColumns = new List<AdaptiveColumnSet>();

        if (source.Changelog != null)
        {
            foreach (var changelog in source.Changelog)
            {
                changeLogColumns.Add(
                    CreateTextComponent(BuildNotificationTransitionMessage(source, changelog)));
            }
        }
        else
        {
            changeLogColumns.Add(
                CreateTextComponent(BuildNotificationTransitionMessage(source, null)));
        }

        actions.Add(new AdaptiveOpenUrlAction
        {
            Title = "Open in Jira",
            Url = new Uri(
                $"https://example.com") // Replace with actual URL
        });

        var commentIssueTaskModuleAction = new JiraBotTeamsDataWrapper
        {
            FetchTaskData = new FetchTaskBotCommand(
                DialogMatchesAndCommands.CommentIssueTaskModuleCommand,
                source.Issue.Id.ToString(),
                source.Issue.Key),
            TeamsData = new TeamsData
            {
                Type = "task/fetch"
            }
        };

        actions.Add(new AdaptiveSubmitAction
        {
            Title = DialogTitles.CommentTitle,
            Data = commentIssueTaskModuleAction
        });

        actions.Add(new AdaptiveSubmitAction
        {
            Title = DialogTitles.EditTitle,
            Data = new JiraBotTeamsDataWrapper
            {
                FetchTaskData = new FetchTaskBotCommand(
                    DialogMatchesAndCommands.EditIssueTaskModuleCommand,
                    source.Issue.Id.ToString(),
                    source.Issue.Key),
                TeamsData = new TeamsData
                {
                    Type = "task/fetch"
                }
            }
        });

        actions.Add(new AdaptiveSubmitAction
        {
            Title = DialogTitles.VoteTitle,
            Data = new JiraBotTeamsDataWrapper
            {
                TeamsData = new TeamsData
                {
                    Type = "imBack",
                    Value = $"{DialogMatchesAndCommands.VoteDialogCommand} {source.Issue.Key}"
                }
            }
        });

        actions.Add(new AdaptiveSubmitAction
        {
            Title = DialogTitles.LogTimeTitle,
            Data = new JiraBotTeamsDataWrapper
            {
                TeamsData = new TeamsData
                {
                    Type = "imBack",
                    Value = $"{DialogMatchesAndCommands.LogTimeDialogCommand} {source.Issue.Key}"
                }
            }
        });

        adaptiveCard.Body.AddRange(changeLogColumns);

        adaptiveCard.Body.Add(new AdaptiveActionSet
        {
            Actions = actions
        });

        adaptiveCard.AdditionalProperties = new SerializableDictionary<string, object>
        {
            { "msTeams", new { width = "full" } }
        };

        return adaptiveCard;
    }

    private static AdaptiveColumnSet CreateTextComponent(string text)
    {
        return new AdaptiveColumnSet
        {
            Columns = new List<AdaptiveColumn>
            {
                new AdaptiveColumn
                {
                    Width = "180",
                    Items = new List<AdaptiveElement>
                    {
                        new AdaptiveTextBlock
                        {
                            Text = text,
                            Size = AdaptiveTextSize.Small,
                            Wrap = true
                        }
                    },
                    VerticalContentAlignment = AdaptiveVerticalContentAlignment.Center
                },
            }
        };
    }

    private static string BuildNotificationTitleMessage(NotificationMessage notificationMessage)
    {
        switch (notificationMessage.EventType)
        {
            case IssueCreated:
                return $"{notificationMessage.User.Name} **created** this issue:";
            case CommentUpdated:
                return $"{notificationMessage.User.Name} **updated comment** on this issue:";
            case CommentCreated:
                return $"{notificationMessage.User.Name} **commented** on this issue:";
            default:
            {
                var updatedFields = string.Join(
                    ", ",
                    notificationMessage.Changelog?.Select(c => c.Field.ToLower()) ?? Array.Empty<string>());
                return $"{notificationMessage.User.Name} updated the **{updatedFields}** on this issue:";
            }
        }
    }

    private static string BuildNotificationTransitionMessage(
        NotificationMessage notificationMessage,
        NotificationChangelog changelog)
    {
        switch (notificationMessage.EventType)
        {
            case CommentCreated:
            case CommentUpdated:
                return notificationMessage.Comment?.Content;
            case IssueCreated:
                return string.Empty;
            default:
                return $"{changelog?.From}     \u2192     {changelog?.To}";
        }
    }
}
