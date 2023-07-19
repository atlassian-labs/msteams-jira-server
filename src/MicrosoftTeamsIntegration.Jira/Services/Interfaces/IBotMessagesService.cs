using System.Threading;
using System.Threading.Tasks;
using AdaptiveCards;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using MicrosoftTeamsIntegration.Jira.Models;

namespace MicrosoftTeamsIntegration.Jira.Services.Interfaces
{
    public interface IBotMessagesService
    {
        string HandleHtmlMessageFromUser(Activity activity);
        Task HandleConversationUpdates(ITurnContext turnContext, CancellationToken cancellationToken);
        Task BuildAndUpdateJiraIssueCard(ITurnContext turnContext, IntegratedUser user, string issueKey);
        Task<AdaptiveCard> SearchIssueAndBuildIssueCard(ITurnContext turnContext, IntegratedUser user, string jiraIssueKey);
        Task SendAuthorizationCard(ITurnContext turnContext, string jiraUrl, CancellationToken cancellationToken = default);
    }
}
