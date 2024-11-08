using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Teamwork.SendActivityNotificationToRecipients;
using MicrosoftTeamsIntegration.Artifacts.Models.GraphApi;
using MicrosoftTeamsIntegration.Artifacts.Services.Interfaces;
using Polly;
using Polly.Retry;

namespace MicrosoftTeamsIntegration.Artifacts.Services
{
    public class GraphApiService : IGraphApiService
    {
        private const int DefaultPolicyRetryCount = 3;
        private const string MethodNameParam = "methodNameParam";

        private readonly ILogger<GraphApiService> _logger;
        private readonly AsyncRetryPolicy _runPolicy;

        public GraphApiService(ILogger<GraphApiService> logger)
        {
            _logger = logger;
            _runPolicy = Policy.Handle<ServiceException>()
                .WaitAndRetryAsync(
                    DefaultPolicyRetryCount,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    (exception, retryCount, retryAttempt, context) =>
                    {
                        if (retryAttempt == DefaultPolicyRetryCount)
                        {
                            _logger.LogError(exception, "{MethodName}.", context[MethodNameParam]);
                        }
                    });
        }

        public async Task<TeamsApp> GetApplicationAsync(GraphServiceClient graphClient, string appId, CancellationToken cancellationToken)
        {
            var teamsApps = await _runPolicy.ExecuteAsync(
                ctx => graphClient
                    .AppCatalogs
                    .TeamsApps
                    .GetAsync(
                        requestConfiguration =>
                    {
                        requestConfiguration.QueryParameters.Filter =
                            $"distributionMethod eq 'organization' and externalId eq '{appId}'";
                    }, cancellationToken),
                new Dictionary<string, object>() { { MethodNameParam, nameof(GetApplicationAsync) } });

            return teamsApps?.Value?.FirstOrDefault()!;
        }

        public async Task SendActivityNotificationAsync(
            GraphServiceClient graphClient,
            ActivityNotification notification,
            string userId,
            CancellationToken cancellationToken)
        {
            try
            {
                var requestBody = new SendActivityNotificationToRecipientsPostRequestBody
                {
                    Topic = new TeamworkActivityTopic
                    {
                        Source = notification.Topic?.Source,
                        Value = notification.Topic?.Value,
                    },
                    ActivityType = notification.ActivityType,
                    PreviewText = new ItemBody
                    {
                        Content = notification.PreviewText?.Content,
                    },
                    Recipients = new List<TeamworkNotificationRecipient>
                    {
                        new AadUserNotificationRecipient
                        {
                            OdataType = notification.Recipient?.Type,
                            UserId = notification.Recipient?.UserId ?? userId,
                        }
                    }
                };

                await graphClient.Teamwork.SendActivityNotificationToRecipients.PostAsync(
                    requestBody, null, cancellationToken);
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Cannot send activity notification");
                throw;
            }
        }
    }
}
