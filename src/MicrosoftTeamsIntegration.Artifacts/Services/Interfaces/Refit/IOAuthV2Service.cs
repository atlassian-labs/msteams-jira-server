using System.Threading.Tasks;
using MicrosoftTeamsIntegration.Artifacts.Models;
using Refit;

namespace MicrosoftTeamsIntegration.Artifacts.Services.Interfaces.Refit
{
    public interface IOAuthV2Service
    {
        [Get("/{tenantId}/oauth2/v2.0/token")]
        Task<OAuthToken> GetToken([Body(BodySerializationMethod.UrlEncoded)] OAuthRequest oAuthRequest, string tenantId);
    }
}
