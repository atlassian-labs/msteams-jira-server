using System.Collections.Generic;
using FakeItEasy;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MicrosoftTeamsIntegration.Jira.Controllers;
using MicrosoftTeamsIntegration.Jira.Settings;
using Xunit;

namespace MicrosoftTeamsIntegration.Jira.Tests.Controllers
{
    public class ClientAppControllerTest
    {
        private readonly IOptions<AppSettings> _appSettings = new OptionsManager<AppSettings>(
            new OptionsFactory<AppSettings>(
                new List<IConfigureOptions<AppSettings>>(),
                new List<IPostConfigureOptions<AppSettings>>()));

        private readonly IOptions<TelemetryConfiguration> _telemetryConfiguration =
            new OptionsManager<TelemetryConfiguration>(
                new OptionsFactory<TelemetryConfiguration>(
                    new List<IConfigureOptions<TelemetryConfiguration>>(),
                    new List<IPostConfigureOptions<TelemetryConfiguration>>()));

        [Fact]
        public void GetClientAppSettingsTest()
        {
            // arrange
            var clientAppController = CreateClientAppController();

            // act
            var result = clientAppController.GetClientAppSettings();
            var okResult = result.Result as ObjectResult;

            // assert
            Assert.NotNull(okResult);
            Assert.Equal(200, okResult.StatusCode);
        }

        private ClientAppController CreateClientAppController()
        {
            return A.Fake<ClientAppController>(
                x => x.WithArgumentsForConstructor(new object[] {
                    _appSettings,
                    _telemetryConfiguration
                }));
        }
    }
}
