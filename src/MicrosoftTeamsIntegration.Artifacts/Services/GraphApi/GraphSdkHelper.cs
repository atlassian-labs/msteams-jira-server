using System;
using Microsoft.Graph;
using Microsoft.Kiota.Abstractions.Authentication;
using MicrosoftTeamsIntegration.Artifacts.Services.Interfaces;

namespace MicrosoftTeamsIntegration.Artifacts.Services.GraphApi
{
    public class GraphSdkHelper : IGraphSdkHelper, IDisposable
    {
        private GraphServiceClient? _graphClient;
        private bool _disposed;

        public GraphServiceClient GetAuthenticatedClient(string accessToken)
        {
            _graphClient = new GraphServiceClient(new BaseBearerTokenAuthenticationProvider(new GraphTokenProvider(accessToken)));

            return _graphClient;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _graphClient?.Dispose();
                }

                _disposed = true;
            }
        }
    }
}
