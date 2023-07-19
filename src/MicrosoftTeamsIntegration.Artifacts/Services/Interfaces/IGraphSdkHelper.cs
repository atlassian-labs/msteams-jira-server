using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Graph;

namespace MicrosoftTeamsIntegration.Artifacts.Services.Interfaces
{
    public interface IGraphSdkHelper
    {
        GraphServiceClient GetAuthenticatedClient(string accessToken);
    }
}
