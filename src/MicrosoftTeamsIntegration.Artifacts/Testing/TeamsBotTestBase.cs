using System.Threading;
using JetBrains.Annotations;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace MicrosoftTeamsIntegration.Artifacts.Testing
{
    [PublicAPI]
    public class TeamsBotTestBase
    {
        protected IServiceCollection? Services { get; set; }
        protected ITurnContext? BaseContext { get; set; }
        protected TestAdapter BaseAdapter { get; set; } = new TestAdapter();

        protected TestFlow GetTestFlow()
        {
            var sp = Services.BuildServiceProvider();
            var testFlow = new TestFlow(BaseAdapter, async (context, _) =>
            {
                var bot = sp.GetService<IBot>();
                if (BaseContext != null)
                {
                    context = BaseContext;
                }

                context.AddDefaultConnectorClient();
                await bot.OnTurnAsync(context, CancellationToken.None);
            });

            return testFlow;
        }

        protected void ReplaceService<T>(T service)
            where T : class
            => Services.Replace(ServiceDescriptor.Transient(s => service));
    }
}
