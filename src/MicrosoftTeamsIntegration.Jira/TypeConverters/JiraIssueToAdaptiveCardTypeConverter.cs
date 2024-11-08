using System;
using System.Collections.Generic;
using System.Text;
using AdaptiveCards;
using AutoMapper;
using MicrosoftTeamsIntegration.Artifacts.Extensions;
using MicrosoftTeamsIntegration.Jira.Dialogs;
using MicrosoftTeamsIntegration.Jira.Extensions;
using MicrosoftTeamsIntegration.Jira.Models;
using MicrosoftTeamsIntegration.Jira.Models.Bot;
using MicrosoftTeamsIntegration.Jira.Models.FetchTask;
using MicrosoftTeamsIntegration.Jira.Models.Interfaces;
using MicrosoftTeamsIntegration.Jira.Settings;

using Newtonsoft.Json;

namespace MicrosoftTeamsIntegration.Jira.TypeConverters
{
    public class JiraIssueToAdaptiveCardTypeConverter : ITypeConverter<BotAndMessagingExtensionJiraIssue, AdaptiveCard>
    {
        private readonly AppSettings _appSettings;

        public JiraIssueToAdaptiveCardTypeConverter(
            AppSettings appSettings)
        {
            _appSettings = appSettings;
        }

        public AdaptiveCard Convert(BotAndMessagingExtensionJiraIssue model, AdaptiveCard card, IResolutionContext context)
        {
            if (model?.JiraIssue is null)
            {
                return null;
            }

            card ??= new AdaptiveCard(new AdaptiveSchemaVersion(1, 5));
            card.AdditionalProperties = new SerializableDictionary<string, object>
            {
                {
                    "msTeams", new
                    {
                        width = "full",
                    }
                },
                {
                    "metadata", new
                    {
                        webUrl = $"{model.JiraInstanceUrl}/browse/{model.JiraIssue?.Key}"
                    }
                }
            };

            model.JiraIssue?.SetJiraIssueIconUrl();
            model.JiraIssue?.SetJiraIssuePriorityIconUrl();

            var watchOrUnwatchActionColumn = GetWatchOrUnwatchAdaptiveColumn(model);
            var assignActionColumn = GetAssignAdaptiveColumn(model);
            var priorityColumn = GetPriorityColumn(model);

            card.Body = new List<AdaptiveElement>
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
                                    VerticalContentAlignment = AdaptiveVerticalContentAlignment.Center,
                                    Items = new List<AdaptiveElement>
                                    {
                                        new AdaptiveImage
                                        {
                                            UrlString = PrepareIconUrl(model.JiraIssue?.Fields?.Type?.IconUrl),
                                            PixelWidth = 30,
                                            PixelHeight = 30,
                                            Size = AdaptiveImageSize.Medium
                                        }
                                    }
                                },
                                new AdaptiveColumn
                                {
                                    Width = "stretch",
                                    Items = new List<AdaptiveElement>
                                    {
                                        new AdaptiveTextBlock
                                        {
                                            Text = model.JiraIssue?.Fields?.Summary,
                                            Wrap = true,
                                            Size = AdaptiveTextSize.Medium,
                                            Weight = AdaptiveTextWeight.Bolder
                                        },
                                        new AdaptiveTextBlock
                                        {
                                            Text = $"{model.JiraIssue?.Fields?.Project?.Name}/{model.JiraIssue?.Key}",
                                            IsSubtle = true,
                                            Size = AdaptiveTextSize.Small,
                                            Spacing = AdaptiveSpacing.None
                                        }
                                    }
                                },
                                watchOrUnwatchActionColumn
                            }
                        },
                        new AdaptiveColumnSet
                        {
                            Columns = new List<AdaptiveColumn>
                            {
                                new AdaptiveColumn
                                {
                                    Width = "stretch",
                                    Spacing = AdaptiveSpacing.Small,
                                    VerticalContentAlignment = AdaptiveVerticalContentAlignment.Top,
                                    Items = new List<AdaptiveElement>
                                    {
                                        new AdaptiveColumnSet
                                        {
                                            Columns = new List<AdaptiveColumn>
                                            {
                                                new AdaptiveColumn
                                                {
                                                    Width = "3",
                                                    VerticalContentAlignment = AdaptiveVerticalContentAlignment.Top,
                                                    Items = new List<AdaptiveElement>
                                                    {
                                                        new AdaptiveColumnSet
                                                        {
                                                            Spacing = AdaptiveSpacing.Small,
                                                            Columns = new List<AdaptiveColumn>
                                                            {
                                                                new AdaptiveColumn
                                                                {
                                                                    Width = "3",
                                                                    Spacing = AdaptiveSpacing.Small,
                                                                    VerticalContentAlignment = AdaptiveVerticalContentAlignment.Top,
                                                                    Items = new List<AdaptiveElement>
                                                                    {
                                                                        new AdaptiveTextBlock
                                                                        {
                                                                            Text = "Status",
                                                                            Wrap = true,
                                                                            Spacing = AdaptiveSpacing.None,
                                                                            Size = AdaptiveTextSize.Small
                                                                        },
                                                                        new AdaptiveColumnSet
                                                                        {
                                                                            Spacing = AdaptiveSpacing.Small,
                                                                            Columns = new List<AdaptiveColumn>
                                                                            {
                                                                                new AdaptiveColumn
                                                                                {
                                                                                    VerticalContentAlignment = AdaptiveVerticalContentAlignment.Top,
                                                                                    Width = "stretch",
                                                                                    Items = new List<AdaptiveElement>
                                                                                    {
                                                                                        new AdaptiveTextBlock
                                                                                        {
                                                                                            Text = model.JiraIssue?.Fields?.Status?.Name,
                                                                                            Spacing = AdaptiveSpacing.None,
                                                                                            Wrap = true,
                                                                                            HorizontalAlignment = AdaptiveHorizontalAlignment.Left,
                                                                                            Weight = AdaptiveTextWeight.Bolder
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
                                                },
                                                priorityColumn,
                                                new AdaptiveColumn
                                                {
                                                    Width = "3",
                                                    Spacing = AdaptiveSpacing.Small,
                                                    VerticalContentAlignment = AdaptiveVerticalContentAlignment.Top,
                                                    Items = new List<AdaptiveElement>
                                                    {
                                                        new AdaptiveColumnSet
                                                        {
                                                            Spacing = AdaptiveSpacing.Small,
                                                            Columns = new List<AdaptiveColumn>
                                                            {
                                                                new AdaptiveColumn
                                                                {
                                                                    Width = "3",
                                                                    Spacing = AdaptiveSpacing.Small,
                                                                    VerticalContentAlignment = AdaptiveVerticalContentAlignment.Top,
                                                                    Items = new List<AdaptiveElement>
                                                                    {
                                                                        new AdaptiveColumnSet
                                                                        {
                                                                            Spacing = AdaptiveSpacing.Small,
                                                                            Columns = new List<AdaptiveColumn>
                                                                            {
                                                                                new AdaptiveColumn
                                                                                {
                                                                                    VerticalContentAlignment = AdaptiveVerticalContentAlignment.Top,
                                                                                    Width = "auto",
                                                                                    Spacing = AdaptiveSpacing.Small,
                                                                                    Items = new List<AdaptiveElement>
                                                                                    {
                                                                                        new AdaptiveTextBlock
                                                                                        {
                                                                                            Text = "Assignee",
                                                                                            Wrap = true,
                                                                                            Spacing = AdaptiveSpacing.None,
                                                                                            Size = AdaptiveTextSize.Small
                                                                                        }
                                                                                    }
                                                                                },
                                                                                assignActionColumn
                                                                            }
                                                                        },
                                                                        new AdaptiveColumnSet
                                                                        {
                                                                            Spacing = AdaptiveSpacing.Small,
                                                                            Columns = new List<AdaptiveColumn>
                                                                            {
                                                                                new AdaptiveColumn
                                                                                {
                                                                                    VerticalContentAlignment = AdaptiveVerticalContentAlignment.Top,
                                                                                    Width = "stretch",
                                                                                    Spacing = AdaptiveSpacing.Small,
                                                                                    Items = new List<AdaptiveElement>
                                                                                    {
                                                                                        new AdaptiveTextBlock
                                                                                        {
                                                                                            Text = model.JiraIssue?
                                                                                            .Fields?.Assignee?.DisplayName ?? "Unassigned",
                                                                                            Spacing = AdaptiveSpacing.None,
                                                                                            Wrap = true,
                                                                                            HorizontalAlignment = AdaptiveHorizontalAlignment.Left,
                                                                                            Weight = AdaptiveTextWeight.Bolder
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
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };

            card.Actions = new List<AdaptiveAction>();

            if (!string.IsNullOrEmpty(model.JiraInstanceUrl))
            {
                card.Actions.Add(new AdaptiveOpenUrlAction
                {
                    Title = "Open in Jira",
                    UrlString = $"{model.JiraInstanceUrl}/browse/{model.JiraIssue?.Key}"
                });
            }

            var commentIssueTaskModuleAction = new JiraBotTeamsDataWrapper
            {
                FetchTaskData = new FetchTaskBotCommand(DialogMatchesAndCommands.CommentIssueTaskModuleCommand, model.JiraIssue?.Id, model.JiraIssue?.Key),
                TeamsData = new TeamsData
                {
                    Type = "task/fetch"
                }
            };

            card.Actions.Add(new AdaptiveSubmitAction
            {
                Title = DialogTitles.CommentTitle,
                Data = commentIssueTaskModuleAction
            });

            if (model.IsMessagingExtension)
            {
                return card;
            }

            // Edit button is common fro personal and team scope
            card.Actions.Add(new AdaptiveSubmitAction
            {
                Title = DialogTitles.EditTitle,
                Data = new JiraBotTeamsDataWrapper
                {
                    FetchTaskData = new FetchTaskBotCommand(DialogMatchesAndCommands.EditIssueTaskModuleCommand, model.JiraIssue?.Id, model.JiraIssue?.Key),
                    TeamsData = new TeamsData
                    {
                        Type = "task/fetch"
                    }
                }
            });

            // Action buttons for bot Jira issue details card
            if (!model.IsGroupConversation)
            {
                if (!model.JiraIssue.IsVotedByUser())
                {
                    card.Actions.Add(new AdaptiveSubmitAction
                    {
                        Title = DialogTitles.VoteTitle,
                        Data = new JiraBotTeamsDataWrapper
                        {
                            TeamsData = new TeamsData
                            {
                                Type = "imBack",
                                Value = $"{DialogMatchesAndCommands.VoteDialogCommand} {model.JiraIssue?.Key}"
                            }
                        }
                    });
                }
                else
                {
                    card.Actions.Add(new AdaptiveSubmitAction
                    {
                        Title = DialogTitles.UnvoteTitle,
                        Data = new JiraBotTeamsDataWrapper
                        {
                            TeamsData = new TeamsData
                            {
                                Type = "imBack",
                                Value = $"{DialogMatchesAndCommands.UnvoteDialogCommand} {model.JiraIssue?.Key}"
                            }
                        }
                    });
                }

                card.Actions.Add(new AdaptiveSubmitAction
                {
                    Title = DialogTitles.LogTimeTitle,
                    Data = new JiraBotTeamsDataWrapper
                    {
                        TeamsData = new TeamsData
                        {
                            Type = "imBack",
                            Value = $"{DialogMatchesAndCommands.LogTimeDialogCommand} {model.JiraIssue?.Key}"
                        }
                    }
                });
            }

            return card;
        }

        public AdaptiveCard Convert(BotAndMessagingExtensionJiraIssue model, AdaptiveCard card, ResolutionContext context)
        {
            return Convert(model, card, new ResolutionContextWrapper(context));
        }

        private static AdaptiveColumn GetPriorityColumn(BotAndMessagingExtensionJiraIssue model)
        {
            if (model.JiraIssue?.Fields?.Priority == null)
            {
                return new AdaptiveColumn()
                {
                    Width = "3",
                };
            }

            return new AdaptiveColumn
            {
                Width = "3",
                Spacing = AdaptiveSpacing.Small,
                VerticalContentAlignment = AdaptiveVerticalContentAlignment.Top,
                Items = new List<AdaptiveElement>
                {
                    new AdaptiveColumnSet
                    {
                        Spacing = AdaptiveSpacing.Small,
                        Columns = new List<AdaptiveColumn>
                        {
                            new AdaptiveColumn
                            {
                                Width = "4",
                                Spacing = AdaptiveSpacing.Small,
                                VerticalContentAlignment = AdaptiveVerticalContentAlignment.Top,
                                Items = new List<AdaptiveElement>
                                {
                                    new AdaptiveTextBlock
                                    {
                                        Text = "Priority",
                                        Wrap = true,
                                        Spacing = AdaptiveSpacing.None,
                                        Size = AdaptiveTextSize.Small
                                    },
                                    new AdaptiveColumnSet
                                    {
                                        Spacing = AdaptiveSpacing.Small,
                                        Columns = new List<AdaptiveColumn>
                                        {
                                            new AdaptiveColumn
                                            {
                                                Width = "16px",
                                                VerticalContentAlignment = AdaptiveVerticalContentAlignment.Top,
                                                Items = new List<AdaptiveElement>
                                                {
                                                    new AdaptiveImage
                                                    {
                                                        UrlString = PrepareIconUrl(model.JiraIssue?.Fields?.Priority
                                                            ?.IconUrl),
                                                        Size = AdaptiveImageSize.Small,
                                                        PixelWidth = 16,
                                                        PixelHeight = 16,
                                                        Spacing = AdaptiveSpacing.None
                                                    }
                                                }
                                            },
                                            new AdaptiveColumn
                                            {
                                                VerticalContentAlignment = AdaptiveVerticalContentAlignment.Top,
                                                Width = "6",
                                                Spacing = AdaptiveSpacing.Small,
                                                Items = new List<AdaptiveElement>
                                                {
                                                    new AdaptiveTextBlock
                                                    {
                                                        Text = model.JiraIssue?.Fields?.Priority?.Name,
                                                        Spacing = AdaptiveSpacing.None,
                                                        Wrap = true,
                                                        HorizontalAlignment = AdaptiveHorizontalAlignment.Left,
                                                        Weight = AdaptiveTextWeight.Bolder
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
            };
        }

        private static string PrepareIconUrl(string iconUrl)
        {
            return !string.IsNullOrEmpty(iconUrl) && Uri.IsWellFormedUriString(iconUrl, UriKind.Absolute) ? iconUrl
                    .AddOrUpdateGetParameter("format", "png")
                    .AddOrUpdateGetParameter("size", "large") : iconUrl;
        }

        private static AdaptiveColumn GetWatchOrUnwatchAdaptiveColumn(BotAndMessagingExtensionJiraIssue model)
        {
            if (model.IsMessagingExtension)
            {
                return new AdaptiveColumn();
            }

            var isWatching = model.JiraIssue.Fields.Watches?.IsWatching == true;
            var watchOrUnwatchTitle = isWatching ? DialogTitles.UnwatchTitle : DialogTitles.WatchTitle;
            var watchOrUnwatchCommand = isWatching ? DialogMatchesAndCommands.UnwatchDialogCommand : DialogMatchesAndCommands.WatchDialogCommand;

            return new AdaptiveColumn
            {
                Width = "auto",
                Spacing = AdaptiveSpacing.None,
                Items = new List<AdaptiveElement>
                {
                    new AdaptiveTextBlock
                    {
                        Text = $"**{watchOrUnwatchTitle} issue**",
                        Spacing = AdaptiveSpacing.None,
                        HorizontalAlignment = AdaptiveHorizontalAlignment.Right,
                        Color = AdaptiveTextColor.Accent
                    }
                },
                SelectAction = new AdaptiveSubmitAction
                {
                    Data = new AdaptiveCardBotCommand($"{watchOrUnwatchCommand} {model.JiraIssue.Key}"),
                    Title = $"{watchOrUnwatchTitle} issue"
                }
            };
        }

        private AdaptiveColumn GetAssignAdaptiveColumn(BotAndMessagingExtensionJiraIssue model)
        {
            if (model.IsMessagingExtension)
            {
                return new AdaptiveColumn();
            }

            var assignCommand = $"{DialogMatchesAndCommands.AssignDialogCommand} {model.JiraIssue.Key}";
            if (!model.IsGroupConversation)
            {
                return !model.JiraIssue.IsAssignedToUser(model.UserNameOrAccountId)
                    ? new AdaptiveColumn
                    {
                        Width = "auto",
                        Items = new List<AdaptiveElement>
                        {
                            new AdaptiveTextBlock
                            {
                                Spacing = AdaptiveSpacing.None,
                                Text = $"({DialogTitles.AssignTitle})",
                                Color = AdaptiveTextColor.Accent,
                                Wrap = true,
                                Size = AdaptiveTextSize.Small
                            }
                        },
                        SelectAction = new AdaptiveSubmitAction
                        {
                            Data = new AdaptiveCardBotCommand(assignCommand),
                            Title = $"{DialogTitles.AssignTitle}"
                        }
                    }
                    : new AdaptiveColumn();
            }

            assignCommand += " myself";

            return new AdaptiveColumn
            {
                Width = "auto",
                Items = new List<AdaptiveElement>
                {
                    new AdaptiveTextBlock
                    {
                        Spacing = AdaptiveSpacing.None,
                        Text = $"({DialogTitles.AssignTitle})",
                        Color = AdaptiveTextColor.Accent,
                        Wrap = true,
                        Size = AdaptiveTextSize.Small
                    }
                },
                SelectAction = new AdaptiveSubmitAction
                {
                    Data = new AdaptiveCardBotCommand(assignCommand),
                    Title = $"{DialogTitles.AssignTitle}"
                }
            };
        }

        internal class AdaptiveCardBotCommand
        {
            internal AdaptiveCardBotCommand(string command)
            {
                Command = command;
            }

            [JsonProperty("command")]
            public string Command { get; set; }
        }
    }
}
