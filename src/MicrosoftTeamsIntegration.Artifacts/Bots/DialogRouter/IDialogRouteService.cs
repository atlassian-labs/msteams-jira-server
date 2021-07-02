using JetBrains.Annotations;
using Microsoft.Bot.Builder.Dialogs;

namespace MicrosoftTeamsIntegration.Artifacts.Bots.DialogRouter
{
    [PublicAPI]
    public interface IDialogRouteService
    {
        Dialog[] GetRegisteredDialogs();
        DialogRoute? FindBestMatch(string messageText);
    }
}
