﻿using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.Bot.Builder.Dialogs;
using MicrosoftTeamsIntegration.Jira.Extensions;
using MicrosoftTeamsIntegration.Jira.Models;
using MicrosoftTeamsIntegration.Jira.Models.Jira.Issue;
using MicrosoftTeamsIntegration.Jira.Services.Interfaces;
using MicrosoftTeamsIntegration.Jira.Settings;

namespace MicrosoftTeamsIntegration.Jira.Dialogs
{
    public class VoteDialog : JiraIssueDependentDialog
    {
        private readonly IJiraService _jiraService;
        private readonly AppSettings _appSettings;
        private readonly TelemetryClient _telemetry;
        private readonly IAnalyticsService _analyticsService;

        public VoteDialog(
            JiraBotAccessors accessors,
            IJiraService jiraService,
            AppSettings appSettings,
            TelemetryClient telemetry,
            IAnalyticsService analyticsService)
            : base(nameof(VoteDialog), accessors, jiraService, appSettings)
        {
            _jiraService = jiraService;
            _appSettings = appSettings;
            _telemetry = telemetry;
            _analyticsService = analyticsService;
        }

        protected override async Task<DialogTurnResult> ProcessJiraIssueAsync(DialogContext dc, IntegratedUser user, JiraIssue jiraIssue)
        {
            _telemetry.TrackPageView("VoteDialog");

            var issueKey = jiraIssue.Key;
            var userNameOrAccountId = await _jiraService.GetUserNameOrAccountId(CurrentUser);

            if (jiraIssue.AllowsToVote(userNameOrAccountId))
            {
                if (!jiraIssue.IsVotedByUser())
                {
                    var response = await _jiraService.Vote(CurrentUser, issueKey);
                    if (response.IsSuccess)
                    {
                        await dc.Context.SendActivityAsync($"Your vote has been added to {issueKey}.");
                        _analyticsService.SendBotDialogEvent(dc.Context, "vote", "completed");
                    }
                    else
                    {
                        await dc.Context.SendActivityAsync(response.ErrorMessage);
                        _analyticsService.SendBotDialogEvent(dc.Context, "vote", "failed", response.ErrorMessage);
                    }
                }
                else
                {
                    await dc.Context.SendActivityAsync($"You have already voted for {issueKey}.");
                    _analyticsService.SendBotDialogEvent(dc.Context, "vote", "completed");
                }
            }
            else
            {
                if (jiraIssue.IsResolved())
                {
                    await dc.Context.SendActivityAsync("You cannot vote for a resolved issue.");
                }

                if (jiraIssue.IsUserReporter(userNameOrAccountId))
                {
                    await dc.Context.SendActivityAsync("You cannot vote for an issue you have reported.");
                }

                _analyticsService.SendBotDialogEvent(dc.Context, "vote", "completed");
            }

            return await dc.EndDialogAsync();
        }
    }
}
