using Bogus;
using MicrosoftTeamsIntegration.Jira.Models;
using MicrosoftTeamsIntegration.Jira.Models.Jira;
using MicrosoftTeamsIntegration.Jira.Models.Jira.Issue;

namespace MicrosoftTeamsIntegration.Jira.Tests
{
    public static class JiraDataGenerator
    {
        public static JiraIssue GenerateJiraIssue()
        {
            var watchesFaker = new Faker<JiraIssueWatches>().RuleFor(o => o.IsWatching, f => f.Random.Bool());

            var reporterFaker = new Faker<JiraUser>()
                .RuleFor(o => o.AccountId, f => f.Random.Uuid().ToString());

            var assigneeFaker = new Faker<JiraUser>()
                .RuleFor(o => o.AccountId, f => f.Random.Uuid().ToString());

            var voters = new Faker<JiraUser>()
                .RuleFor(o => o.DisplayName, f => f.Name.FullName())
                .RuleFor(o => o.AccountId, f => f.Random.Uuid().ToString());

            var votesFaker = new Faker<JiraIssueVotes>()
                .RuleFor(o => o.HasVoted, f => f.Random.Bool())
                .RuleFor(o => o.Voters, f => voters.Generate(f.Random.Number(1, 5)).ToArray());

            var fieldsFaker = new Faker<JiraIssueFields>()
                .RuleFor(o => o.Watches, () => watchesFaker)
                .RuleFor(o => o.Assignee, () => assigneeFaker)
                .RuleFor(o => o.Reporter, () => reporterFaker)
                .RuleFor(o => o.Votes, () => votesFaker);

            var jiraIssuesFaker = new Faker<JiraIssue>()
                .RuleFor(o => o.Id, f => f.Random.Uuid().ToString())
                .RuleFor(o => o.Key, f => $"{f.Random.Word().ToUpperInvariant()}-{f.Random.Number(0, 100)}")
                .RuleFor(o => o.Fields, () => fieldsFaker);

            var jiraIssue = jiraIssuesFaker.Generate();

            return jiraIssue;
        }

        public static IntegratedUser GenerateUser()
        {
            var userFaker = new Faker<IntegratedUser>()
                .RuleFor(o => o.MsTeamsUserId, f => f.Random.Uuid().ToString())
                .RuleFor(o => o.Id, f => f.Random.Uuid().ToString())
                .RuleFor(o => o.IsUsedForPersonalScope, f => f.Random.Bool())
                .RuleFor(o => o.IsUsedForPersonalScopeBefore, f => f.Random.Bool())
                .RuleFor(o => o.JiraClientKey, f => f.Random.Word())
                .RuleFor(o => o.JiraInstanceUrl, f => f.Internet.Url().ToString())
                .RuleFor(o => o.JiraUserAccountId, f => f.Random.Uuid().ToString());

            var user = userFaker.Generate();

            return user;
        }
    }
}
