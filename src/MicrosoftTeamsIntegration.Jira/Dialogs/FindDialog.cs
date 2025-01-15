using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using MicrosoftTeamsIntegration.Artifacts.Extensions;
using MicrosoftTeamsIntegration.Artifacts.Models.Cards;
using MicrosoftTeamsIntegration.Jira.Extensions;
using MicrosoftTeamsIntegration.Jira.Helpers;
using MicrosoftTeamsIntegration.Jira.Models;
using MicrosoftTeamsIntegration.Jira.Models.Jira.Issue;
using MicrosoftTeamsIntegration.Jira.Services;
using MicrosoftTeamsIntegration.Jira.Services.Interfaces;
using MicrosoftTeamsIntegration.Jira.Settings;

namespace MicrosoftTeamsIntegration.Jira.Dialogs
{
    public class FindDialog : Dialog
    {
        private readonly JiraBotAccessors _accessors;
        private readonly IJiraService _jiraService;
        private readonly AppSettings _appSettings;
        private readonly TelemetryClient _telemetry;
        private readonly IAnalyticsService _analyticsService;

        public FindDialog(
            JiraBotAccessors accessors,
            IJiraService jiraBridgeService,
            AppSettings appSettings,
            TelemetryClient telemetry,
            IAnalyticsService analyticsService)
            : base(nameof(FindDialog))
        {
            _jiraService = jiraBridgeService;
            _accessors = accessors;
            _appSettings = appSettings;
            _telemetry = telemetry;
            _analyticsService = analyticsService;
        }

        public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default)
        {
            _telemetry.TrackPageView("FindDialog");

            var user = await JiraBotAccessorsHelper.GetUser(_accessors, dc.Context, _appSettings, cancellationToken);
            var query = dc.Context.Activity.GetTextWithoutCommand(DialogMatchesAndCommands.FindDialogCommand);

            if (string.IsNullOrEmpty(query))
            {
                await dc.Context.SendActivityAsync(
                    $"To search for an issue type '{DialogMatchesAndCommands.FindDialogCommand}' and provide a keyword. (e.g. find cookies)",
                    cancellationToken: cancellationToken);
                return await dc.EndDialogAsync(cancellationToken: cancellationToken);
            }

            JiraIssueSearch apiResponse;
            try
            {
                var searchRequest = SearchForIssuesRequestBase.CreateDefaultRequest();
                searchRequest.Jql = JiraIssueSearchHelper.GetSearchJql(query);
                apiResponse = await _jiraService.Search(user, searchRequest);
            }
            catch (MethodAccessException e)
            {
                await dc.Context.SendActivityAsync(e.Message, cancellationToken: cancellationToken);
                return await dc.EndDialogAsync(cancellationToken: cancellationToken);
            }

            if (apiResponse?.ErrorMessages != null && apiResponse.ErrorMessages.Any())
            {
                await dc.Context.SendActivityAsync(apiResponse.ErrorMessages.FirstOrDefault(), cancellationToken: cancellationToken);
                _analyticsService.SendBotDialogEvent(dc.Context, "find", "failed", apiResponse.ErrorMessages.FirstOrDefault());
            }
            else
            {
                if (apiResponse?.JiraIssues != null && apiResponse.JiraIssues.Any())
                {
                    var jiraIssues = apiResponse.JiraIssues;
                    var cardItems = new List<CardListItem>();
                    foreach (var jiraIssue in jiraIssues)
                    {
                        jiraIssue.SetJiraIssueIconUrl();
                        cardItems.Add(new CardListItem
                        {
                            Type = CardListItemTypes.ResultItem,
                            Title = jiraIssue.Key,
                            Subtitle = jiraIssue.Fields.Summary,
                            Icon = jiraIssue.Fields.Type.IconUrl
                                .AddOrUpdateGetParameter("format", "png")
                                .AddOrUpdateGetParameter("size", "large"),
                            Tap = new CardAction(ActionTypes.ImBack, value: jiraIssue.Key)
                        });
                    }

                    var attachment = new Attachment
                    {
                        ContentType = ListCard.ContentType,
                        Content = new ListCard
                        {
                            Title = $"Search results for '{query}'",
                            Items = cardItems
                        }
                    };

                    var message = dc.Context.Activity.CreateReply();
                    message.Attachments.Add(attachment);

                    await dc.Context.SendActivityAsync(message, cancellationToken);
                }
                else
                {
                    await dc.Context.SendActivityAsync("We didn't find any issues, try another keyword.", cancellationToken: cancellationToken);
                }

                _analyticsService.SendBotDialogEvent(dc.Context, "find", "completed");
            }

            return await dc.EndDialogAsync(cancellationToken: cancellationToken);
        }
    }
}
