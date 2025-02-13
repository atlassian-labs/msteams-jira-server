﻿using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;

namespace MicrosoftTeamsIntegration.Jira.Services.Interfaces
{
    public interface IUserTokenService
    {
        Task<TokenResponse> GetUserTokenAsync(ITurnContext context, string connectionName, string magicCode, CancellationToken cancellationToken);
        Task<TokenResponse> GetUserTokenAsync(ITurnContext context, CancellationToken cancellationToken);
        Task<string> GetSignInLink(ITurnContext context, CancellationToken cancellationToken);
    }
}
