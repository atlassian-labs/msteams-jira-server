using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MicrosoftTeamsIntegration.Jira.Exceptions;
using MicrosoftTeamsIntegration.Jira.Helpers;
using MicrosoftTeamsIntegration.Jira.Models.Jira;
using MicrosoftTeamsIntegration.Jira.Services.Interfaces;
using MicrosoftTeamsIntegration.Jira.Services.SignalR.Interfaces;
using MicrosoftTeamsIntegration.Jira.Settings;
using NonBlocking;

namespace MicrosoftTeamsIntegration.Jira.Services.SignalR
{
    public class SignalRService : ISignalRService
    {
        private readonly IHubContext<GatewayHub> _hub;
        private readonly IDatabaseService _databaseService;
        private readonly ILogger<SignalRService> _logger;
        private readonly AppSettings _appSettings;

        private static readonly ConcurrentDictionary<Guid, TaskCompletionSource<string>> ClientResponses = new ConcurrentDictionary<Guid, TaskCompletionSource<string>>();

        public SignalRService(
            IHubContext<GatewayHub> hub,
            IDatabaseService databaseService,
            ILogger<SignalRService> logger,
            IOptionsMonitor<AppSettings> appSettings)
        {
            _hub = hub;
            _databaseService = databaseService;
            _logger = logger;
            _appSettings = appSettings.CurrentValue;
        }

        public async Task<IOperationResponse> SendRequestAndWaitForResponse(string jiraServerId, string message, CancellationToken cancellationToken)
        {
            if (!(await _databaseService.GetJiraServerAddonSettingsByJiraId(jiraServerId) is JiraAddonSettings addonSettings))
            {
                var errorMessage =
                    $"Please check if {jiraServerId} is valid Jira instance and you have latest version of Microsoft Teams for Jira Server installed.";
                return new SignalRResponse
                {
                    Received = false,
                    Message = $"code: 500, response: \"\", message: {errorMessage}"
                };
            }

            // Create an entry in the dictionary that will be used to track the client response
            var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            var identifier = Guid.NewGuid();
            ClientResponses.Add(identifier, tcs);

            _logger.LogTrace(
                "SignalRClient SendRequest started: {Identifier} | {Message} | {CurrentThreadId} | {ClientResponses.Keys}",
                identifier.ToString(),
                SanitizingHelpers.SanitizeMessage(message),
                Thread.CurrentThread.ManagedThreadId.ToString(),
                ClientResponses.GetLog());

            var connectionId = addonSettings.ConnectionId;

            // Call MakeRequest method on the client passing the identifier
            await _hub.Clients.Client(connectionId).SendAsync("MakeRequest", identifier, message, cancellationToken);

            _logger.LogTrace(
                "SignalRClient SendRequest request sent: {Identifier} | {Message} | {CurrentThreadId} | {ClientResponses.Keys}",
                identifier.ToString(),
                SanitizingHelpers.SanitizeMessage(message),
                Thread.CurrentThread.ManagedThreadId.ToString(),
                ClientResponses.GetLog());

            try
            {
                using (var timeoutCancellation = new CancellationTokenSource())
                {
                    using (var combinedCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCancellation.Token))
                    {
                        var delayTask = Task.Delay(TimeSpan.FromSeconds(_appSettings.JiraServerResponseTimeoutInSeconds), combinedCancellation.Token);
                        var originalTask = tcs.Task;
                        var completedTask = await Task.WhenAny(originalTask, delayTask);
                        if (completedTask == originalTask)
                        {
                            timeoutCancellation.Cancel();
                            return new SignalRResponse
                            {
                                Received = true,
                                Message = await originalTask
                            };
                        }
                    }
                }
            }
            finally
            {
                // Remove the tcs from the dictionary so that we don't leak memory
                var removeResult = ClientResponses.Remove(identifier);

                _logger.LogTrace(
                    "SignalRClient SendRequest request sent: {Identifier} | {Message} | {CurrentThreadId} | {RemoveResult} | {ClientResponses.Keys}",
                    identifier.ToString(),
                    SanitizingHelpers.SanitizeMessage(message),
                    Thread.CurrentThread.ManagedThreadId.ToString(),
                    removeResult.ToString(),
                    ClientResponses.GetLog());
            }

            var jiraServerGeneralException =
                new JiraGeneralException(
                    "Microsoft Teams app for Jira Server is not responding. Please try later.");

            _logger.LogError(
                jiraServerGeneralException,
                "{Identifier} | {Message} | {CurrentThreadId} | {ClientResponses.Keys}",
                identifier.ToString(),
                SanitizingHelpers.SanitizeMessage(message),
                Thread.CurrentThread.ManagedThreadId.ToString(),
                ClientResponses.GetLog());

            throw jiraServerGeneralException;
        }

        public Task Callback(Guid identifier, string response)
        {
            _logger.LogTrace(
                "SignalRClient Callback called: {Identifier} | {Response} | {CurrentThreadId} | {ClientResponses.Keys}",
                identifier.ToString(),
                response,
                Thread.CurrentThread.ManagedThreadId.ToString(),
                ClientResponses.GetLog());

            if (ClientResponses.TryGetValue(identifier, out var tcs))
            {
                _logger.LogTrace(
                    "SignalRClient Callback getting response from ClientResponses successful: {Identifier} | {Response} | {CurrentThreadId} | {ClientResponses.Keys}",
                    identifier.ToString(),
                    response,
                    Thread.CurrentThread.ManagedThreadId.ToString(),
                    ClientResponses.GetLog());

                // Trigger the task continuation
                tcs.TrySetResult(response);
            }
            else
            {
                // Client response for something that isn't being tracked, might be an error
                _logger.LogWarning("Not tracked response from Jira addon.");
            }

            return Task.CompletedTask;
        }
    }
}
