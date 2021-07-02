using System;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using JetBrains.Annotations;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using MicrosoftTeamsIntegration.Jira.Services.Interfaces;
using MicrosoftTeamsIntegration.Jira.Services.SignalR.Interfaces;

namespace MicrosoftTeamsIntegration.Jira
{
    public class GatewayHub : Hub
    {
        private readonly IDatabaseService _databaseService;
        private readonly ILogger<GatewayHub> _logger;
        private readonly ISignalRService _signalRService;

        public GatewayHub(
            IDatabaseService databaseService,
            ILogger<GatewayHub> logger,
            ISignalRService signalRService)
        {
            _databaseService = databaseService;
            _logger = logger;
            _signalRService = signalRService;
        }

        [UsedImplicitly]
        public Task Callback(Guid identifier, string response)
        {
            _logger.LogTrace(
                "Callback: {Identifier} | {ConnectionId} | {ResponseMessage} | {CurrentThreadId}",
                identifier.ToString(),
                Context.ConnectionId,
                response,
                Thread.CurrentThread.ManagedThreadId.ToString());

            return _signalRService.Callback(identifier, response);
        }

        public override async Task OnConnectedAsync()
        {
            var queryString = Context.GetHttpContext()?.Request?.QueryString;
            if (queryString.HasValue)
            {
                var uriComponent = queryString.Value.ToUriComponent();
                var jiraId = HttpUtility.ParseQueryString(uriComponent).Get("atlasId");
                var jiraInstanceUrl = HttpUtility.ParseQueryString(uriComponent).Get("atlasUrl");
                var version = HttpUtility.ParseQueryString(uriComponent).Get("pluginVersion");

                _logger.LogTrace(
                    "OnConnectedAsync: {ConnectionId} | {JiraId} | {JiraInstanceUrl} | {Version} | {CurrentThreadId}",
                    Context.ConnectionId,
                    jiraId,
                    jiraInstanceUrl,
                    version,
                    Thread.CurrentThread.ManagedThreadId.ToString());

                if (!string.IsNullOrEmpty(jiraId) && jiraId != "null")
                {
                    await _databaseService.CreateOrUpdateJiraServerAddonSettings(jiraId, jiraInstanceUrl, Context.ConnectionId, version);
                }
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            _logger.LogTrace(
                "OnDisconnectedAsync: {ConnectionId} | {CurrentThreadId}",
                Context.ConnectionId,
                Thread.CurrentThread.ManagedThreadId.ToString());

            await _databaseService.DeleteJiraServerAddonSettingsByConnectionId(Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }
    }
}
