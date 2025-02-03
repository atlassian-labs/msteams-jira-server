using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Options;
using MicrosoftTeamsIntegration.Jira.Services;
using MicrosoftTeamsIntegration.Jira.Services.Interfaces;
using MicrosoftTeamsIntegration.Jira.Settings;
using Moq;
using Xunit;

namespace MicrosoftTeamsIntegration.Jira.Tests.Services;

public class AnalyticsServiceTests
{
    private readonly Mock<ITelemetryClient> _telemetryMock;
    private readonly AnalyticsService _analyticsService;

    public AnalyticsServiceTests()
    {
        _telemetryMock = new Mock<ITelemetryClient>();
        var appSettingsMock = new Mock<IOptions<AppSettings>>();
        appSettingsMock.Setup(ap => ap.Value).Returns(new AppSettings { AnalyticsEnvironment = "test-env" });

        _analyticsService = new AnalyticsService(_telemetryMock.Object, appSettingsMock.Object);
    }

    [Fact]
    public void SendBotDialogEvent_ShouldTrackEvent()
    {
        // Arrange
        Activity activity = new Activity
        {
            From = new ChannelAccount { AadObjectId = "test-user-id" },
            Conversation = new ConversationAccount { IsGroup = true }
        };
        Mock<ITurnContext> turnContextMock = new Mock<ITurnContext>();
        turnContextMock.Setup(context => context.Activity).Returns(activity);
        turnContextMock.Setup(context => context.Activity).Returns(activity);

        // Act
        _analyticsService.SendBotDialogEvent(
            turnContextMock.Object,
            "test-dialog",
            "test-action",
            "test-error");

        // Assert
        _telemetryMock.Verify(t => t.TrackPageView(It.IsAny<PageViewTelemetry>()), Times.Once);
    }

    [Fact]
    public void SendTrackEvent_ShouldTrackEvent()
    {
        // Act
        _analyticsService.SendTrackEvent(
            "test-user-id",
            "test-source",
            "test-action",
            "test-subject",
            "test-id");

        // Assert
        _telemetryMock.Verify(t => t.TrackPageView(It.IsAny<PageViewTelemetry>()), Times.Once);
    }

    [Fact]
    public void SendUiEvent_ShouldTrackEvent()
    {
        // Act
        _analyticsService.SendUiEvent(
            "test-user-id",
            "test-source",
            "test-action",
            "test-subject",
            "test-id");

        // Assert
        _telemetryMock.Verify(t => t.TrackPageView(It.IsAny<PageViewTelemetry>()), Times.Once);
    }

    [Fact]
    public void SendScreenEvent_ShouldTrackEvent()
    {
        // Act
        _analyticsService.SendScreenEvent(
            "test-user-id",
            "test-source",
            "test-action",
            "test-subject",
            "test-name");

        // Assert
        _telemetryMock.Verify(t => t.TrackPageView(It.IsAny<PageViewTelemetry>()), Times.Once);
    }
}
