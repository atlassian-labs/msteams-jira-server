using JetBrains.Annotations;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;

namespace MicrosoftTeamsIntegration.Artifacts.Extensions
{
    [PublicAPI]
    public static class WebHostBuilderExtensions
    {
        public static IWebHostBuilder ConfigureMicrosoftTeamsIntegrationDefaults(this IWebHostBuilder hostBuilder)
            => hostBuilder.ConfigureAppConfiguration((context, builder) =>
            {
                var settings = builder.Build();
                var appConfigurationConnectionString = settings["AppConfiguration:ConnectionString"];
                if (appConfigurationConnectionString.HasValue())
                {
                    builder.AddAzureAppConfiguration(options =>
                    {
                        options.Connect(appConfigurationConnectionString);
                        var labelFilter = settings["AppConfiguration:Environment"];
                        if (labelFilter.HasValue())
                        {
                            options
                                .Select(KeyFilter.Any)
                                .Select(KeyFilter.Any, labelFilter);
                        }
                    });
                }
            });
    }
}
