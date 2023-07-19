using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using MicrosoftTeamsIntegration.Jira.Filters;

namespace MicrosoftTeamsIntegration.Jira.Controllers
{
    [Route("api/messages")]
    [ApiController]
    public class BotController : ControllerBase
    {
        private readonly IBot _bot;
        private readonly IBotFrameworkHttpAdapter _adapter;

        public BotController(IBot bot, IBotFrameworkHttpAdapter adapter)
        {
            _bot = bot;
            _adapter = adapter;
        }

        [HttpPost]
        public Task Post(CancellationToken cancellationToken) => _adapter.ProcessAsync(Request, Response, _bot, cancellationToken);
    }
}
