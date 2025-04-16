using System;
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
                Environment.CurrentManagedThreadId.ToString());

            return _signalRService.Callback(identifier, response);
        }

        [UsedImplicitly]
        public Task Broadcast(Guid identifier, string response)
        {
            _logger.LogTrace(
                "Broadcast: {Identifier} | {ConnectionId} | {ResponseMessage} | {CurrentThreadId}",
                identifier.ToString(),
                Context.ConnectionId,
                response,
                Environment.CurrentManagedThreadId.ToString());

            return _signalRService.Broadcast(identifier, response);
        }

        public override async Task OnConnectedAsync()
        {
            var queryString = Context.GetHttpContext()?.Request.QueryString;
            if (queryString.HasValue)
            {
                var uriComponent = queryString.Value.ToUriComponent();
                var jiraId = HttpUtility.ParseQueryString(uriComponent).Get("atlasId");
                var jiraInstanceUrl = HttpUtility.ParseQueryString(uriComponent).Get("atlasUrl");
                var version = HttpUtility.ParseQueryString(uriComponent).Get("pluginVersion");
                var groupName = HttpUtility.ParseQueryString(uriComponent).Get("groupName");

                _logger.LogTrace(
                    "OnConnectedAsync: {ConnectionId} | {JiraId} | {JiraInstanceUrl} | {Version} | {CurrentThreadId}",
                    Context.ConnectionId,
                    jiraId,
                    jiraInstanceUrl,
                    version,
                    Environment.CurrentManagedThreadId.ToString());

                if (!string.IsNullOrEmpty(jiraId) && jiraId != "null")
                {
                    await _databaseService.CreateOrUpdateJiraServerAddonSettings(jiraId, jiraInstanceUrl, Context.ConnectionId, version);
                }

                if (!string.IsNullOrEmpty(groupName) && groupName != "null")
                {
                    try
                    {
                        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, "Cannot add client to the group: {GroupName}", groupName);
                    }
                }
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            _logger.LogTrace(
                "OnDisconnectedAsync: {ConnectionId} | {CurrentThreadId}",
                Context.ConnectionId,
                Environment.CurrentManagedThreadId.ToString());

            await _databaseService.DeleteJiraServerAddonSettingsByConnectionId(Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }
    }
}
