using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema.Teams;
using MicrosoftTeamsIntegration.Jira.Models;
using MicrosoftTeamsIntegration.Jira.Models.FetchTask;

namespace MicrosoftTeamsIntegration.Jira.Services.Interfaces
{
    public interface IMessagingExtensionService
    {
        Task<MessagingExtensionResponse> HandleMessagingExtensionQuery(ITurnContext turnContext, IntegratedUser user);
        Task<FetchTaskResponseEnvelope> HandleMessagingExtensionFetchTask(ITurnContext turnContext, IntegratedUser user);
        Task<FetchTaskResponseEnvelope> HandleBotFetchTask(ITurnContext turnContext, IntegratedUser user);
        Task<object> HandleMessagingExtensionSubmitActionAsync(ITurnContext turnContext, IntegratedUser user);
        bool TryValidateMessageExtensionFetchTask(ITurnContext turnContext, IntegratedUser user, out FetchTaskResponseEnvelope response);
        Task<MessagingExtensionResponse> HandleMessagingExtensionQueryLinkAsync(ITurnContext turnContext, IntegratedUser user, string jiraIssueIdOrKey);
        bool TryValidateMessagingExtensionQueryLink(ITurnContext turnContext, IntegratedUser user, out string jiraIssueIdOrKey);
        Task<FetchTaskResponseEnvelope> HandleTaskSubmitActionAsync(ITurnContext turnContext, IntegratedUser user);
    }
}
