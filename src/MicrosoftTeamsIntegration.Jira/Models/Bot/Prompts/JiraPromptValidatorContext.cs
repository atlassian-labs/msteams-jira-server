using System.Collections.Generic;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;

namespace MicrosoftTeamsIntegration.Jira.Models.Bot.Prompts
{
    public class JiraPromptValidatorContext<T>
    {
        internal JiraPromptValidatorContext(ITurnContext turnContext, PromptRecognizerResult<T> recognized, IDictionary<string, object> state, PromptOptions options)
        {
            Context = turnContext;
            Options = options;
            Recognized = recognized;
            State = state;
        }

        public ITurnContext Context { get; }

        public PromptRecognizerResult<T> Recognized { get; }

        public PromptOptions Options { get; }

        public IDictionary<string, object> State { get; }
    }
}
