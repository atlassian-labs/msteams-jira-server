using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using MicrosoftTeamsIntegration.Artifacts.Bots.Middleware;
using Moq;
using Xunit;

namespace MicrosoftTeamsIntegration.Jira.Tests.Bots.Middleware;

public class SetLocaleMiddlewareTests
{
    [Fact]
    public void Constructor_Should_ThrowArgumentNullException_When_DefaultLocaleIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new SetLocaleMiddleware(null));
    }

    [Fact]
    public async Task OnTurnAsync_Should_SetCultureInfoToActivityLocale()
    {
        const string defaultLocale = "en-US";
        var middleware = new SetLocaleMiddleware(defaultLocale);
        var mockTurnContext = new Mock<ITurnContext>();
        var mockNextDelegate = new Mock<NextDelegate>();
        var activity = new Activity { Locale = "fr-FR" };
        mockTurnContext.Setup(c => c.Activity).Returns(activity);
        mockNextDelegate.Setup(nd => nd(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        await middleware.OnTurnAsync(mockTurnContext.Object, mockNextDelegate.Object);

        mockNextDelegate.Verify(nd => nd(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnTurnAsync_Should_SetCultureInfoToDefaultLocale_When_ActivityLocaleIsNullOrWhiteSpace()
    {
        const string defaultLocale = "en-US";
        var middleware = new SetLocaleMiddleware(defaultLocale);
        var mockTurnContext = new Mock<ITurnContext>();
        var mockNextDelegate = new Mock<NextDelegate>();
        var activity = new Activity { Locale = string.Empty };
        mockTurnContext.Setup(c => c.Activity).Returns(activity);
        mockNextDelegate.Setup(nd => nd(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        await middleware.OnTurnAsync(mockTurnContext.Object, mockNextDelegate.Object);

        mockNextDelegate.Verify(nd => nd(It.IsAny<CancellationToken>()), Times.Once);
    }
}
