using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Graph;
using MicrosoftTeamsIntegration.Artifacts.Services.Interfaces;

namespace MicrosoftTeamsIntegration.Artifacts.Services.GraphApi
{
    public class GraphSdkHelper : IGraphSdkHelper
    {
        private GraphServiceClient? _graphClient;

        public GraphServiceClient GetAuthenticatedClient(string accessToken)
        {
            _graphClient = new GraphServiceClient(authenticationProvider: new DelegateAuthenticationProvider(
                requestMessage =>
                {
                    requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                    return Task.CompletedTask;
                }));

            return _graphClient;
        }
    }
}
