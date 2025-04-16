using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MicrosoftTeamsIntegration.Jira.Services.Interfaces;
using Polly;
using Polly.Retry;

namespace MicrosoftTeamsIntegration.Jira.Services.SignalR;

public class SignalRBroadcastClient : IHostedService
{
    public static readonly string BroadcastGroupName = "BroadcastGroup";

    private const int DefaultPoliceRetryCount = 3;
    private readonly ILogger<SignalRBroadcastClient> _logger;
    private readonly IConfiguration _configuration;
    private readonly AsyncRetryPolicy _startPolicy;
    private IHubConnectionWrapper _hubConnectionWrapper;

    public SignalRBroadcastClient(ILogger<SignalRBroadcastClient> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        _startPolicy = Policy.Handle<Exception>()
            .WaitAndRetryAsync(
                DefaultPoliceRetryCount,
                retryAttempts => TimeSpan.FromSeconds(Math.Pow(2, retryAttempts)),
                (exception, retryCount, retryAttempt, context) =>
                {
                    if (retryAttempt == DefaultPoliceRetryCount)
                    {
                        _logger.LogError(exception, "Cannot start SignalRBroadcastClient. Error connecting to SignalR server.");
                    }
                });
    }

    public SignalRBroadcastClient(
        ILogger<SignalRBroadcastClient> logger,
        IConfiguration configuration,
        IHubConnectionWrapper hubConnectionWrapper)
        : this(logger, configuration)
    {
        _hubConnectionWrapper = hubConnectionWrapper;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        string appBaseUrl = _configuration.GetValue<string>("AppBaseUrl");
        string baseUrl = _configuration.GetValue<string>("BaseUrl");
        string hubBaseUrl = string.IsNullOrEmpty(appBaseUrl) ? baseUrl : appBaseUrl;
        string hubUrl = $"{hubBaseUrl}/JiraGateway?groupName={BroadcastGroupName}";

        await _startPolicy.ExecuteAsync(async () =>
        {
            if (_hubConnectionWrapper == null)
            {
                var hubConnection = new HubConnectionBuilder()
                    .WithUrl(hubUrl)
                    .Build();

                hubConnection.On<string, string>(
                    "Broadcast",
                    async (identifier, response) =>
                    {
                        await hubConnection.InvokeAsync(
                            "Broadcast",
                            identifier,
                            response,
                            cancellationToken: cancellationToken);
                    });

                hubConnection.Closed += async (error) =>
                 {
                     await Task.Delay(new Random().Next(0, 5) * 1000, cancellationToken);
                     await hubConnection.StartAsync(cancellationToken);
                     _logger.LogInformation("SignalR broadcast client reconnected due to error {ErrorMessage}.", error.Message);
                 };

                _hubConnectionWrapper = new HubConnectionWrapper(hubConnection);
            }

            await _hubConnectionWrapper.StartAsync(cancellationToken);
            _logger.LogInformation("SignalR broadcast client connected.");
        });
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_hubConnectionWrapper != null)
        {
            await _hubConnectionWrapper.StopAsync(cancellationToken);
            _logger.LogInformation("SignalR broadcast client disconnected.");
        }
    }
}
