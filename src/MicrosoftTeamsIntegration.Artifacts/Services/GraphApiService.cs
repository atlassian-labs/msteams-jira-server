using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
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

        public async Task<TeamsApp> GetApplicationAsync(IGraphServiceClient graphClient, string appId, CancellationToken cancellationToken)
        {
            var teamsApps = await _runPolicy.ExecuteAsync(
                ctx => graphClient
                    .AppCatalogs
                    .TeamsApps
                    .Request()
                    .Filter($"distributionMethod eq 'organization' and externalId eq '{appId}'")
                    .GetAsync(cancellationToken),
                new Dictionary<string, object>() { { MethodNameParam, nameof(GetApplicationAsync) } });

            return teamsApps.FirstOrDefault()!;
        }

        public async Task SendActivityNotificationAsync(IGraphServiceClient graphClient, ActivityNotification notification, string userId, CancellationToken cancellationToken)
        {
            try
            {
                var baseRequest = new BaseRequest($"https://graph.microsoft.com/beta/users/{userId}/teamwork/sendActivityNotification", graphClient)
                {
                    Method = HttpMethod.Post.Method,
                    ContentType = MediaTypeNames.Application.Json
                };

                await baseRequest.SendAsync<ActivityNotification>(notification, cancellationToken);
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Cannot send activity notification");
                throw;
            }
        }
    }
}
