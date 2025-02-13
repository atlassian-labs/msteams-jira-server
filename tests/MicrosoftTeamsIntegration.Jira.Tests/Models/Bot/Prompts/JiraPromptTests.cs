using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FakeItEasy;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using MicrosoftTeamsIntegration.Jira.Models.Bot.Prompts;
using Xunit;

namespace MicrosoftTeamsIntegration.Jira.Tests.Models.Bot.Prompts;

public class JiraPromptTests : JiraPrompt<string>
{
        private readonly DialogContext _dialogContext;
        private readonly ITurnContext _turnContext;
        private readonly PromptOptions _promptOptions;
        private readonly List<DialogInstance> _dialogInstances = new List<DialogInstance>
        {
            new DialogInstance
            {
                Id = "testId",
                State = new Dictionary<string, object>
                {
                    { "state", new Dictionary<string, object>() },
                    { "options", new PromptOptions() }
                }
            }
        };

        public JiraPromptTests()
            : base("test")
        {
            _turnContext = A.Fake<ITurnContext>();
            _dialogContext = new DialogContext(A.Fake<DialogSet>(), _turnContext, new DialogState(_dialogInstances));
            _promptOptions = new PromptOptions
            {
                Prompt = MessageFactory.Text("Test prompt")
            };
        }

        [Fact]
        public async Task BeginDialogAsync_ShouldInitializeStateAndSendPrompt()
        {
            // Arrange
            var cancellationToken = CancellationToken.None;

            // Act
            var result = await BeginDialogAsync(_dialogContext, _promptOptions, cancellationToken);

            // Assert
            Assert.Equal(DialogTurnStatus.Waiting, result.Status);
        }

        [Fact]
        public async Task ContinueDialogAsync_ShouldReturnEndOfTurnForNonMessageActivity()
        {
            // Arrange
            var cancellationToken = CancellationToken.None;
            _turnContext.Activity.Type = ActivityTypes.ConversationUpdate;

            // Act
            var result = await ContinueDialogAsync(_dialogContext, cancellationToken);

            // Assert
            Assert.Equal(DialogTurnStatus.Waiting, result.Status);
        }

        [Fact]
        public async Task ContinueDialogAsync_ShouldEndDialogIfValid()
        {
            // Arrange
            var cancellationToken = CancellationToken.None;
            _turnContext.Activity.Type = ActivityTypes.Message;

            // Act
            var result = await ContinueDialogAsync(_dialogContext, cancellationToken);

            // Assert
            Assert.Equal(DialogTurnStatus.Complete, result.Status);
            Assert.Equal("recognized", result.Result);
        }

        [Fact]
        public async Task ResumeDialogAsync_ShouldReturnDialogTurnResult()
        {
            // Arrange
            var cancellationToken = CancellationToken.None;
            var dialogInstance = new DialogInstance { State = new Dictionary<string, object> { { "state", new Dictionary<string, object>() }, { "options", _promptOptions } } };

            // Act
            var dialogTurnResult = await ResumeDialogAsync(_dialogContext, DialogReason.BeginCalled,  dialogInstance, cancellationToken);

            // Assert
            Assert.Equal(DialogTurnStatus.Waiting, dialogTurnResult.Status);
        }

        protected override Task OnPromptAsync(
            ITurnContext turnContext,
            IDictionary<string, object> state,
            PromptOptions options,
            bool isRetry,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        protected override Task<PromptRecognizerResult<string>> OnRecognizeAsync(
            ITurnContext turnContext,
            IDictionary<string, object> state,
            PromptOptions options,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new PromptRecognizerResult<string> { Succeeded = true, Value = "recognized" });
        }
}
