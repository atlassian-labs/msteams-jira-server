using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using MicrosoftTeamsIntegration.Artifacts.Bots.Middleware;
using Moq;
using Xunit;

namespace MicrosoftTeamsIntegration.Jira.Tests.Bots.Middleware;

public class ConsoleOutputMiddlewareTests
{
    [Fact]
    public async Task OnTurnAsync_Should_LogMessageActivity_And_CallNext()
    {
        const string testMessage = "Test message";
        var mockTurnContext = new Mock<ITurnContext>();
        var mockNextDelegate = new Mock<NextDelegate>();
        var activity = new Activity(ActivityTypes.Message) { Text = testMessage };
        mockTurnContext.Setup(c => c.Activity).Returns(activity);
        mockNextDelegate.Setup(nd => nd(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var middleware = new ConsoleOutputMiddleware();

        using (var consoleOutput = new ConsoleOutput())
        {
            await middleware.OnTurnAsync(mockTurnContext.Object, mockNextDelegate.Object);

            var output = consoleOutput.GetOutput();
            Assert.Contains(testMessage, output);
        }

        mockTurnContext.Verify(c => c.Activity, Times.Once);
        mockNextDelegate.Verify(nd => nd(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnTurnAsync_Should_LogEventActivity_And_CallNext()
    {
        const string testEvent = "Test event";
        var mockTurnContext = new Mock<ITurnContext>();
        var mockNextDelegate = new Mock<NextDelegate>();
        var activity = new Activity(ActivityTypes.Event) { Name = testEvent };
        mockTurnContext.Setup(c => c.Activity).Returns(activity);
        mockNextDelegate.Setup(nd => nd(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var middleware = new ConsoleOutputMiddleware();

        using (var consoleOutput = new ConsoleOutput())
        {
            await middleware.OnTurnAsync(mockTurnContext.Object, mockNextDelegate.Object);

            var output = consoleOutput.GetOutput();
            Assert.Contains(testEvent, output);
        }

        mockTurnContext.Verify(c => c.Activity, Times.Once);
        mockNextDelegate.Verify(nd => nd(It.IsAny<CancellationToken>()), Times.Once);
    }
}
