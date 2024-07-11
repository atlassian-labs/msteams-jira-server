using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using MicrosoftTeamsIntegration.Artifacts.Bots.Middleware;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace MicrosoftTeamsIntegration.Jira.Tests.Bots.Middleware;

public class EventDebuggerMiddlewareTests
{
    [Fact]
    public async Task OnTurnAsync_Should_ConvertMessageToEvent_When_TextStartsWithEvent()
    {
        var mockTurnContext = new Mock<ITurnContext>();
        var mockNextDelegate = new Mock<NextDelegate>();
        var json = JsonConvert.SerializeObject(new { Name = "testEvent", Text = "testText", Value = "testValue" });
        var activity = new Activity(ActivityTypes.Message) { Text = $"/event:{json}" };
        mockTurnContext.Setup(c => c.Activity).Returns(activity);
        mockNextDelegate.Setup(nd => nd(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var middleware = new EventDebuggerMiddleware();

        await middleware.OnTurnAsync(mockTurnContext.Object, mockNextDelegate.Object);

        Assert.Equal(ActivityTypes.Event, activity.Type);
        Assert.Equal("testEvent", activity.Name);
        Assert.Equal("testText", activity.Text);
        Assert.Equal("testValue", activity.Value);
        mockNextDelegate.Verify(nd => nd(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnTurnAsync_Should_ConvertMessageToEvent_When_ValueContainsEvent()
    {
        var mockTurnContext = new Mock<ITurnContext>();
        var mockNextDelegate = new Mock<NextDelegate>();
        var value = new JObject
        {
            ["event"] = true,
            ["name"] = "testEvent",
            ["text"] = "testText",
            ["value"] = "testValue"
        };
        var activity = new Activity(ActivityTypes.Message) { Value = value.ToString(), Text = "some text" };
        mockTurnContext.Setup(c => c.Activity).Returns(activity);
        mockNextDelegate.Setup(nd => nd(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var middleware = new EventDebuggerMiddleware();

        await middleware.OnTurnAsync(mockTurnContext.Object, mockNextDelegate.Object);

        Assert.Equal(ActivityTypes.Event, activity.Type);
        Assert.Equal("testEvent", activity.Name);
        Assert.Equal("testText", activity.Text);
        Assert.Equal("testValue", activity.Value);
        mockNextDelegate.Verify(nd => nd(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnTurnAsync_Should_NotConvertMessageToEvent_When_TextDoesNotStartWithEvent()
    {
        var mockTurnContext = new Mock<ITurnContext>();
        var mockNextDelegate = new Mock<NextDelegate>();
        var activity = new Activity(ActivityTypes.Message) { Text = "regular text message" };
        mockTurnContext.Setup(c => c.Activity).Returns(activity);
        mockNextDelegate.Setup(nd => nd(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var middleware = new EventDebuggerMiddleware();

        await middleware.OnTurnAsync(mockTurnContext.Object, mockNextDelegate.Object);

        Assert.Equal(ActivityTypes.Message, activity.Type);
        Assert.Equal("regular text message", activity.Text);
        mockNextDelegate.Verify(nd => nd(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnTurnAsync_Should_NotConvertMessageToEvent_When_ValueDoesNotContainEvent()
    {
        var mockTurnContext = new Mock<ITurnContext>();
        var mockNextDelegate = new Mock<NextDelegate>();
        var value = new JObject
        {
            ["name"] = "testEvent",
            ["text"] = "testText",
            ["value"] = "testValue"
        };
        var activity = new Activity(ActivityTypes.Message) { Value = value.ToString() };
        mockTurnContext.Setup(c => c.Activity).Returns(activity);
        mockNextDelegate.Setup(nd => nd(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var middleware = new EventDebuggerMiddleware();

        await middleware.OnTurnAsync(mockTurnContext.Object, mockNextDelegate.Object);

        Assert.Equal(ActivityTypes.Message, activity.Type);
        Assert.Equal(value.ToString(), activity.Value);
        mockNextDelegate.Verify(nd => nd(It.IsAny<CancellationToken>()), Times.Once);
    }
}
