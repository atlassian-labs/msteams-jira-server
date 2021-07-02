using System;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using MicrosoftTeamsIntegration.Jira.Models;
using MicrosoftTeamsIntegration.Jira.Models.Bot;

namespace MicrosoftTeamsIntegration.Jira
{
    public class JiraBotAccessors
    {
        public JiraBotAccessors(ConversationState conversationState, UserState userState)
        {
            ConversationState = conversationState ?? throw new ArgumentNullException(nameof(conversationState));
            UserState = userState ?? throw new ArgumentNullException(nameof(userState));

            ConversationDialogState = conversationState.CreateProperty<DialogState>("DialogState");
            JiraIssueState = userState.CreateProperty<JiraIssueState>(nameof(JiraIssueState));
            User = userState.CreateProperty<IntegratedUser>(nameof(IntegratedUser));
            DialogStartTime = conversationState.CreateProperty<long>("DialogStartTime");
            DialogSessionId = conversationState.CreateProperty<string>("DialogSessionId");
        }

        public IStatePropertyAccessor<DialogState> ConversationDialogState { get; set; }
        public IStatePropertyAccessor<JiraIssueState> JiraIssueState { get; set; }
        public IStatePropertyAccessor<IntegratedUser> User { get; set; }
        public ConversationState ConversationState { get; }
        public UserState UserState { get; }
        public IStatePropertyAccessor<long> DialogStartTime { get; set; }
        public IStatePropertyAccessor<string> DialogSessionId { get; set; }
    }
}
