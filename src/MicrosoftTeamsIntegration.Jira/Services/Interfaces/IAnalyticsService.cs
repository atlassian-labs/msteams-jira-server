using Microsoft.Bot.Builder;
using MicrosoftTeamsIntegration.Jira.Models;

namespace MicrosoftTeamsIntegration.Jira.Services.Interfaces
{
    public interface IAnalyticsService
    {
        void SendBotDialogEvent(ITurnContext context, string dialogName, string dialogAction);
        void SendTrackEvent(string userId, string source, string action, string actionSubject, string actionSubjectId, IAnalyticsEventAttribute attributes = null);
        void SendUiEvent(string userId, string source, string action, string actionSubject, string actionSubjectId, IAnalyticsEventAttribute attributes = null);
        void SendScreenEvent(string userId, string source, string action, string actionSubject, string name, IAnalyticsEventAttribute attributes = null);
    }
}
