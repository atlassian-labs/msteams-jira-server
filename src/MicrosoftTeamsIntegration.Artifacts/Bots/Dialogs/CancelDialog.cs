using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;

namespace MicrosoftTeamsIntegration.Artifacts.Bots.Dialogs
{
    [PublicAPI]
    public class CancelDialog : ComponentDialog
    {
        private const string CancelPromptDialogId = "cancelPrompt";

        public CancelDialog()
            : base(nameof(CancelDialog))
        {
            InitialDialogId = nameof(CancelDialog);

            var cancel = new WaterfallStep[] { AskToCancel, FinishCancelDialog };

            AddDialog(new WaterfallDialog(InitialDialogId, cancel));
            AddDialog(new ConfirmPrompt(CancelPromptDialogId));
        }

        protected override async Task<DialogTurnResult> EndComponentAsync(DialogContext dc, object result, CancellationToken cancellationToken)
        {
            var doCancel = (bool)result;
            if (doCancel)
            {
                // If user chose to cancel
                await dc.Context.SendActivityAsync("Ok, let's start over.", cancellationToken: cancellationToken);

                // Cancel all in outer stack of component i.e. the stack the component belongs to
                return await dc.CancelAllDialogsAsync(cancellationToken);
            }
            else
            {
                // else if user chose not to cancel
                await dc.Context.SendActivityAsync("Ok, let's keep going.", cancellationToken: cancellationToken);

                // End this component. Will trigger reprompt/resume on outer stack
                return await dc.EndDialogAsync(cancellationToken: cancellationToken);
            }
        }

        private static Task<DialogTurnResult> AskToCancel(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            return sc.PromptAsync(
                CancelPromptDialogId,
                new PromptOptions
                {
                    Prompt = MessageFactory.Text("Are you sure you want to cancel?")
                },
                cancellationToken);
        }

        private static Task<DialogTurnResult> FinishCancelDialog(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            return sc.EndDialogAsync(sc.Result, cancellationToken);
        }
    }
}
