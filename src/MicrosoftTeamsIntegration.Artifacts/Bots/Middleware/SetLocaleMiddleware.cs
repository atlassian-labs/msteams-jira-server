using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;

namespace MicrosoftTeamsIntegration.Artifacts.Bots.Middleware
{
    public class SetLocaleMiddleware : IMiddleware
    {
        private readonly string _defaultLocale;

        public SetLocaleMiddleware(string defaultLocale)
        {
            _defaultLocale = defaultLocale ?? throw new ArgumentNullException(nameof(defaultLocale));
        }

        public async Task OnTurnAsync(ITurnContext turnContext, NextDelegate next, CancellationToken cancellationToken = default(CancellationToken))
        {
            var cultureInfo = !string.IsNullOrWhiteSpace(turnContext.Activity.Locale) ? new CultureInfo(turnContext.Activity.Locale) : new CultureInfo(this._defaultLocale);

            CultureInfo.CurrentUICulture = CultureInfo.CurrentCulture = cultureInfo;

            await next(cancellationToken).ConfigureAwait(false);
        }
    }
}
