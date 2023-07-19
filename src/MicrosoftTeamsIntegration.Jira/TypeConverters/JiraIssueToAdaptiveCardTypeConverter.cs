using System.Collections.Generic;
using System.Globalization;
using System.Text;
using AdaptiveCards;
using AutoMapper;
using MicrosoftTeamsIntegration.Artifacts.Extensions;
using MicrosoftTeamsIntegration.Jira.Dialogs;
using MicrosoftTeamsIntegration.Jira.Extensions;
using MicrosoftTeamsIntegration.Jira.Models;
using MicrosoftTeamsIntegration.Jira.Models.Bot;
using MicrosoftTeamsIntegration.Jira.Models.FetchTask;
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

        public AdaptiveCard Convert(BotAndMessagingExtensionJiraIssue model, AdaptiveCard card, ResolutionContext context)
        {
            if (model?.JiraIssue is null)
            {
                return null;
            }

            if (card is null)
            {
                card = new AdaptiveCard(new AdaptiveSchemaVersion(1, 2));
            }

            model.JiraIssue.SetJiraIssueIconUrl(_appSettings.BaseUrl);

            var watchOrUnwatchActionColumn = GetWatchOrUnwatchAdaptiveColumn(model);
            var assignActionColumn = GetAssignAdaptiveColumn(model);
            var issueDescriptionTextBlock = GetIssueDescriptionTextBlock(model);
            var issueSummaryText = new StringBuilder();
            if (!string.IsNullOrEmpty(model.JiraInstanceUrl))
            {
                issueSummaryText.Append($"[{model.JiraIssue.Fields.Summary}]({model.JiraInstanceUrl}/browse/{model.JiraIssue.Key})");
            }
            else
            {
                issueSummaryText.Append($"{model.JiraIssue.Fields.Summary}");
            }

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
                                            UrlString = PrepareIconUrl(model.JiraIssue.Fields.Type.IconUrl),
                                            Size = AdaptiveImageSize.Small,
                                            HorizontalAlignment = AdaptiveHorizontalAlignment.Left,
                                            Spacing = AdaptiveSpacing.None
                                        }
                                    }
                                },
                                new AdaptiveColumn
                                {
                                    Width = "stretch",
                                    VerticalContentAlignment = AdaptiveVerticalContentAlignment.Center,
                                    Items = new List<AdaptiveElement>
                                    {
                                        new AdaptiveTextBlock
                                        {
                                            Text = model.JiraIssue.Fields.Project?.Name,
                                            Size = AdaptiveTextSize.Small,
                                            Spacing = AdaptiveSpacing.None,
                                            MaxLines = 1
                                        },
                                        new AdaptiveTextBlock
                                        {
                                            Text = model.JiraIssue.Key,
                                            Spacing = AdaptiveSpacing.None
                                        }
                                    }
                                },
                                watchOrUnwatchActionColumn
                            }
                        },
                        new AdaptiveColumnSet
                        {
                            Spacing = AdaptiveSpacing.Small,
                            Columns = new List<AdaptiveColumn>
                            {
                                new AdaptiveColumn
                                {
                                    Width = "stretch",
                                    Items = new List<AdaptiveElement>
                                    {
                                        new AdaptiveTextBlock
                                        {
                                            Text = issueSummaryText.ToString(),
                                            Spacing = AdaptiveSpacing.None,
                                            Color = AdaptiveTextColor.Dark,
                                            Wrap = true,
                                            MaxLines = 3
                                        },
                                        new AdaptiveTextBlock
                                        {
                                            Text = $"{model.JiraIssue.Fields.Status?.Name} " +
                                                   $"| {model.JiraIssue.Fields.Priority?.Name} " +
                                                   $"| {model.JiraIssue.Fields.Updated.ToString("MM/dd/yyyy", CultureInfo.InvariantCulture)}",
                                            Spacing = AdaptiveSpacing.Small,
                                            IsSubtle = true,
                                            Weight = AdaptiveTextWeight.Lighter
                                        },
                                        new AdaptiveFactSet
                                        {
                                            Spacing = AdaptiveSpacing.Small,
                                            Facts = new List<AdaptiveFact>
                                            {
                                                new AdaptiveFact
                                                {
                                                    Title = "Reporter:",
                                                    Value = model.JiraIssue.Fields.Reporter?.DisplayName
                                                },
                                                new AdaptiveFact
                                                {
                                                    Title = "Assignee:",
                                                    Value = !string.IsNullOrEmpty(model.JiraIssue.Fields.Assignee?.DisplayName) ?
                                                        model.JiraIssue.Fields.Assignee.DisplayName :
                                                        "Unassigned"
                                                }
                                            }
                                        },
                                        new AdaptiveColumnSet
                                        {
                                            Spacing = AdaptiveSpacing.None,
                                            Columns = new List<AdaptiveColumn>
                                            {
                                                assignActionColumn
                                            }
                                        }
                                    }
                                }
                            }
                        },
                        issueDescriptionTextBlock
                    }
                }
            };

            card.Actions = new List<AdaptiveAction>
            {
                // Comment button is common for ME card and Bot issue details card
                new AdaptiveShowCardAction
                {
                    Title = DialogTitles.CommentTitle,
                    Card = new AdaptiveCard(new AdaptiveSchemaVersion(1, 2))
                    {
                        Body = new List<AdaptiveElement>
                        {
                            new AdaptiveTextBlock { Text = $"Add your comment to {model.JiraIssue.Key}" },
                            new AdaptiveTextInput { IsMultiline = true, Id = "commentText" }
                        },
                        Actions = new List<AdaptiveAction>
                        {
                            new AdaptiveSubmitAction
                            {
                                Title = "Cancel",
                                Data = new AdaptiveCardBotCommand(DialogMatchesAndCommands.CancelCommand)
                            },
                            new AdaptiveSubmitAction
                            {
                                Title = "Save",
                                Data = new AdaptiveCardBotCommand($"{DialogMatchesAndCommands.CommentDialogCommand} {model.JiraIssue.Key}")
                            }
                        }
                    }
                }
            };

            if (model.IsMessagingExtension && !string.IsNullOrEmpty(model.JiraInstanceUrl))
            {
                card.Actions.Add(new AdaptiveOpenUrlAction
                {
                    Title = "View in Jira",
                    UrlString = $"{model.JiraInstanceUrl}/browse/{model.JiraIssue.Key}"
                });
            }

            if (model.IsMessagingExtension)
            {
                return card;
            }

            // Edit button is common fro personal and team scope
            var taskModuleAction = new JiraBotTeamsDataWrapper
            {
                FetchTaskData = new FetchTaskBotCommand(DialogMatchesAndCommands.EditIssueTaskModuleCommand, model.JiraIssue.Id, model.JiraIssue.Key),
                TeamsData = new TeamsData
                {
                    Type = "task/fetch"
                }
            };

            card.Actions.Add(new AdaptiveSubmitAction
            {
                Title = DialogTitles.EditTitle,
                Data = taskModuleAction
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
                                Value = $"{DialogMatchesAndCommands.VoteDialogCommand} {model.JiraIssue.Key}"
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
                                Value = $"{DialogMatchesAndCommands.UnvoteDialogCommand} {model.JiraIssue.Key}"
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
                            Value = $"{DialogMatchesAndCommands.LogTimeDialogCommand} {model.JiraIssue.Key}"
                        }
                    }
                });
            }

            return card;
        }

        private static string PrepareIconUrl(string iconUrl)
        {
            return iconUrl
                    .AddOrUpdateGetParameter("format", "png")
                    .AddOrUpdateGetParameter("size", "large");
        }

        private static string AdjustTicketDescription(string ticketDescription)
        {
            var text = new StringBuilder();
            text.Append(ticketDescription);
            return text.Replace("\\\\", "\n\n").ToString();
        }

        private static AdaptiveColumn GetWatchOrUnwatchAdaptiveColumn(BotAndMessagingExtensionJiraIssue model)
        {
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
                                Text = $"_({DialogTitles.AssignTitle})_",
                                Color = AdaptiveTextColor.Accent
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
                        Text = $"_({DialogTitles.AssignTitle})_",
                        Color = AdaptiveTextColor.Accent
                    }
                },
                SelectAction = new AdaptiveSubmitAction
                {
                    Data = new AdaptiveCardBotCommand(assignCommand),
                    Title = $"{DialogTitles.AssignTitle}"
                }
            };
        }

        private static AdaptiveTextBlock GetIssueDescriptionTextBlock(BotAndMessagingExtensionJiraIssue model)
        {
            return model.IsMessagingExtension
                ? new AdaptiveTextBlock()
                : new AdaptiveTextBlock
                {
                    Text = AdjustTicketDescription(model.JiraIssue.Fields.Description),
                    Wrap = true,
                    MaxLines = 2
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
