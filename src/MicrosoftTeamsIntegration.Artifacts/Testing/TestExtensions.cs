using FakeItEasy;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Connector;

namespace MicrosoftTeamsIntegration.Artifacts.Testing
{
    public static class TestExtensions
    {
        public static void AddDefaultConnectorClient(this ITurnContext turnContext)
        {
            var assemblyKey = typeof(IConnectorClient).FullName;
            if (turnContext.TurnState.ContainsKey(assemblyKey))
            {
                return;
            }

            var connectorClient = A.Fake<IConnectorClient>();
            turnContext.AddConnectorClient(connectorClient);
        }

        public static void AddConnectorClient(this ITurnContext turnContext, IConnectorClient connectorClient)
            => turnContext.TurnState.Add(typeof(IConnectorClient).FullName, connectorClient);
    }
}
