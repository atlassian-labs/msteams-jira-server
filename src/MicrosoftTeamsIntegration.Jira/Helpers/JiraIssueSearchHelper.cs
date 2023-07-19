using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Bot.Schema.Teams;
using MicrosoftTeamsIntegration.Artifacts.Extensions;
using MicrosoftTeamsIntegration.Jira.Models;
using MicrosoftTeamsIntegration.Jira.Models.Jira.Issue;
using Newtonsoft.Json.Linq;

namespace MicrosoftTeamsIntegration.Jira.Helpers
{
    public static class JiraIssueSearchHelper
    {
        public static string GetEpicFieldNameFromSchema(JToken schema)
        {
            // Schema is a maping fields ids in jira api (to identify customfieldId responsibility)
            // 'com.pyxis.greenhopper.jira:gh-epic-label' is a custom field type responsible for Epic Name in jira api
            // to get schema data in search response, needed to provide 'expand=schema' request param
            // https://developer.atlassian.com/jiradev/jira-apis/jira-rest-apis/jira-rest-api-tutorials/jira-rest-api-version-2-tutorial
            var epicFieldName = string.Empty;
            if (schema != null)
            {
                var customFields =
                    schema.SelectTokens("$..[?(@.custom == 'com.pyxis.greenhopper.jira:gh-epic-label')]");

                // In case there are multiple epics linked to the issue (?), we are picking first one
                // Reported in bug [#56455] Unable to search all Jira issues via the Microsoft team application
                var epicIdFieldId = customFields?.FirstOrDefault()?.Value<int?>("customId");
                if (epicIdFieldId.HasValue)
                {
                    epicFieldName = $"customfield_{epicIdFieldId}";
                }
            }

            return epicFieldName;
        }

        public static string GetSearchJql(string searchTerm)
        {
            if (string.IsNullOrEmpty(searchTerm))
            {
                return string.Empty;
            }

            // Issue Key exact match search
            if (searchTerm.Contains("-"))
            {
                var parts = searchTerm.Split("-", StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 2)
                {
                    var projectKey = parts[0].NormalizeUtterance();
                    var issueNumber = parts[1].NormalizeUtterance();
                    if (projectKey.HasValue() && issueNumber.All(char.IsDigit))
                    {
                        return $"issuekey='{projectKey.ToLower(CultureInfo.InvariantCulture)}-{issueNumber}'";
                    }
                }
            }

            // Issue summary wildcard search
            return $"summary~'{searchTerm}*' order by lastViewed DESC, updated DESC";
        }

        public static SearchForIssuesRequest PrepareSearchParameter(MessagingExtensionQuery composeExtensionQuery, bool isInitialRun)
        {
            var searchParameter = MessagingExtensionHelper.GetQueryParameterByName(composeExtensionQuery, "search");
            var jqlQuery = GetSearchJql(searchParameter);
            if (isInitialRun || string.IsNullOrEmpty(jqlQuery))
            {
                jqlQuery = "order by lastViewed DESC, updated DESC";
            }

            var request = SearchForIssuesRequestBase.CreateDefaultRequest();
            request.Jql = jqlQuery;
            return request;
        }

        public static O365ConnectorCard CreateO365ConnectorCardFromApiResponse(JiraIssueSearch apiResponse, IntegratedUser user, List<JiraIssuePriority> priorities)
        {
            if (apiResponse?.JiraIssues == null || !apiResponse.JiraIssues.Any())
            {
                return null;
            }

            var epicFieldName = GetEpicFieldNameFromSchema(apiResponse.Schema);

            var jiraIssue = apiResponse.JiraIssues.First();
            return JiraIssueTemplateHelper.BuildO365ConnectorCard(user, jiraIssue, epicFieldName, priorities);
        }
    }
}
