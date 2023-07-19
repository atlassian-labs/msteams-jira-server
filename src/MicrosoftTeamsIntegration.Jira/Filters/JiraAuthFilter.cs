using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MicrosoftTeamsIntegration.Jira.Settings;

namespace MicrosoftTeamsIntegration.Jira.Filters
{
    public class JiraAuthFilter : IAsyncActionFilter
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<JiraAuthFilter> _logger;

        public JiraAuthFilter(
            IServiceProvider services,
            IConfiguration configuration,
            ILogger<JiraAuthFilter> logger)
        {
            _services = services;
            _logger = logger;
            var appSettings = configuration.Get<AppSettings>();
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            await next();
        }
    }
}
