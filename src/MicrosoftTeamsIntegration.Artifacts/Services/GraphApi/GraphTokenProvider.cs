using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Kiota.Abstractions.Authentication;

namespace MicrosoftTeamsIntegration.Artifacts.Services.GraphApi;

public class GraphTokenProvider : IAccessTokenProvider
{
    private string _accessToken;

    public GraphTokenProvider(string accessToken)
    {
        _accessToken = accessToken;
    }

    public Task<string> GetAuthorizationTokenAsync(
        Uri uri,
        Dictionary<string, object>? additionalAuthenticationContext = null,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_accessToken);
    }

    public AllowedHostsValidator AllowedHostsValidator { get; } = null!;
}
