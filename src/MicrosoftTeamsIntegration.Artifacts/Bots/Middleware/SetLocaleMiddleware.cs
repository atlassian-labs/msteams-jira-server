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

        public async Task OnTurnAsync(ITurnContext context, NextDelegate next, CancellationToken cancellationToken = default(CancellationToken))
        {
            var cultureInfo = !string.IsNullOrWhiteSpace(context.Activity.Locale) ? new CultureInfo(context.Activity.Locale) : new CultureInfo(this._defaultLocale);

            CultureInfo.CurrentUICulture = CultureInfo.CurrentCulture = cultureInfo;

            await next(cancellationToken).ConfigureAwait(false);
        }
    }
}
