using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using MicrosoftTeamsIntegration.Artifacts.Extensions;

namespace MicrosoftTeamsIntegration.Jira
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            AppDomain.CurrentDomain.SetData("REGEX_DEFAULT_MATCH_TIMEOUT", TimeSpan.FromSeconds(2));
            CreateHostBuilder(args).Build().Run();
        }

        private static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.ConfigureMicrosoftTeamsIntegrationDefaults();
                    webBuilder.UseStartup<Startup>();
                });
    }
}
