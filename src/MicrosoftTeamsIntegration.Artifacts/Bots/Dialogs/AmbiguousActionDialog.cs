using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using MicrosoftTeamsIntegration.Artifacts.Bots.DialogRouter;

namespace MicrosoftTeamsIntegration.Artifacts.Bots.Dialogs
{
    public class AmbiguousActionDialog : Dialog
    {
        private readonly bool _isDevelopmentMode;

        public AmbiguousActionDialog(bool isDevelopmentMode)
            : base(nameof(AmbiguousActionDialog))
        {
            _isDevelopmentMode = isDevelopmentMode;
            AmbiguousRoutes = Array.Empty<DialogRoute>();
        }

        public DialogRoute[] AmbiguousRoutes { get; set; }

        public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object? options = null, CancellationToken cancellationToken = default)
        {
            string message;

            if (_isDevelopmentMode)
            {
                message = "Your input is ambiguous. Following Regex commands overlap each other:";
                foreach (var route in AmbiguousRoutes)
                {
                    message += $"{Environment.NewLine} - {route.Dialog!.Id}";
                }
            }
            else
            {
                message = "Sorry, I didn't understand your request. Try to re-phrase or use **help** to explore commands.";
            }

            await dc.Context.SendActivityAsync(message, cancellationToken: cancellationToken);

            return await dc.EndDialogAsync(cancellationToken: cancellationToken);
        }
    }
}
