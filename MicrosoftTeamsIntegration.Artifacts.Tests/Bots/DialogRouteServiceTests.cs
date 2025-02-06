using FakeItEasy;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Bot.Builder.Dialogs;
using MicrosoftTeamsIntegration.Artifacts.Bots.Dialogs;
using MicrosoftTeamsIntegration.Artifacts.Bots.DialogRouter;

namespace MicrosoftTeamsIntegration.Artifacts.Tests.Bots
{
    public class DialogRouteServiceTests
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly DialogRouteService _dialogRouteService;

        public DialogRouteServiceTests()
        {
            _serviceProvider = A.Fake<IServiceProvider>();
            var hostingEnvironment = A.Fake<IWebHostEnvironment>();

            A.CallTo(() => _serviceProvider.GetService(typeof(TestDialog))).Returns(new TestDialog());
            A.CallTo(() => _serviceProvider.GetService(typeof(AnotherTestDialog))).Returns(new AnotherTestDialog());
            A.CallTo(() => _serviceProvider.GetService(typeof(IWebHostEnvironment))).Returns(hostingEnvironment);

            var dialogRoutes = new[]
            {
                new DialogRoute(typeof(TestDialog), "test", isAuthenticationRequired: false)
            };

            _dialogRouteService = new DialogRouteService(_serviceProvider, dialogRoutes);
        }

        [Fact]
        public void GetRegisteredDialogs_ShouldReturnAllDialogs()
        {
            var dialogs = _dialogRouteService.GetRegisteredDialogs();
            Assert.NotNull(dialogs);
            Assert.Contains(dialogs, d => d is TestDialog);
        }

        [Fact]
        public void FindBestMatch_ShouldReturnBestMatchedRoute()
        {
            var messageText = "test";
            var route = _dialogRouteService.FindBestMatch(messageText);
            Assert.NotNull(route);
            Assert.Equal("test", route.TextCommandList.First());
        }

        [Fact]
        public void FindBestMatch_ShouldReturnNullForNoMatch()
        {
            var messageText = "unknown";
            var route = _dialogRouteService.FindBestMatch(messageText);
            Assert.Null(route);
        }

        [Fact]
        public void FindBestMatch_ShouldReturnBestMatchedRegexRoute()
        {
            var regex = new System.Text.RegularExpressions.Regex("regex");
            var regexRoute = new DialogRoute(typeof(TestDialog), regex, order: 1);
            var dialogRouteService = new DialogRouteService(_serviceProvider, regexRoute);

            var messageText = "regex";
            var route = dialogRouteService.FindBestMatch(messageText);
            Assert.NotNull(route);
            Assert.Equal(regexRoute, route);
        }

        [Fact]
        public void FindBestMatch_ShouldHandleCancelRoutes()
        {
            var regex = new System.Text.RegularExpressions.Regex("regex1");
            var regexRoute = new DialogRoute(typeof(TestDialog), regex, order: 1);

            var dialogRouteService = new DialogRouteService(_serviceProvider, regexRoute);

            var messageText = "cancel";
            var route = dialogRouteService.FindBestMatch(messageText);
            Assert.NotNull(route);
            Assert.Equal(typeof(CancelDialog), route.DialogType);
        }

        [Fact]
        public void FindBestMatch_ShouldReturnBestMatchedTextCommandRoute()
        {
            var textRoute = new DialogRoute(typeof(TestDialog), "text", isAuthenticationRequired: false);
            var dialogRouteService = new DialogRouteService(_serviceProvider, textRoute);

            var messageText = "text";
            var route = dialogRouteService.FindBestMatch(messageText);
            Assert.NotNull(route);
            Assert.Equal(textRoute, route);
        }

        [Fact]
        public void FindBestMatch_ShouldReturnNullForNoTextCommandMatch()
        {
            var textRoute = new DialogRoute(typeof(TestDialog), "text", isAuthenticationRequired: false);
            var dialogRouteService = new DialogRouteService(_serviceProvider, textRoute);

            var messageText = "unknown";
            var route = dialogRouteService.FindBestMatch(messageText);
            Assert.Null(route);
        }

        [Fact]
        public void FindBestMatch_ShouldReturnBestMatchedRouteWithHighestScore()
        {
            var textRoute1 = new DialogRoute(typeof(TestDialog), "text", isAuthenticationRequired: false, threshold: 1.01);
            var textRoute2 = new DialogRoute(typeof(AnotherTestDialog), "text", isAuthenticationRequired: false, threshold: 0.99);
            var dialogRouteService = new DialogRouteService(_serviceProvider, textRoute1, textRoute2);

            var messageText = "text";
            var route = dialogRouteService.FindBestMatch(messageText);
            Assert.NotNull(route);
            Assert.Equal(textRoute2, route);
        }

        private class TestDialog() : Dialog(nameof(TestDialog))
        {
            public override Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object? options = null,
                CancellationToken cancellationToken = new CancellationToken())
            {
                throw new NotImplementedException();
            }
        }
        
        private class AnotherTestDialog() : Dialog(nameof(AnotherTestDialog))
        {
            public override Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object? options = null,
                CancellationToken cancellationToken = new CancellationToken())
            {
                throw new NotImplementedException();
            }
        }
    }
}
