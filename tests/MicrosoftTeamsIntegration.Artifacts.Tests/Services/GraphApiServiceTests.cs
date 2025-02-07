using FakeItEasy;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Serialization;
using MicrosoftTeamsIntegration.Artifacts.Models.GraphApi;
using MicrosoftTeamsIntegration.Artifacts.Services;

namespace MicrosoftTeamsIntegration.Artifacts.Tests.Services;

public class GraphApiServiceTests
{
    private readonly GraphApiService _graphApiService;
    private readonly GraphServiceClient _graphClient;
    private readonly IRequestAdapter _fakeRequestAdapter;

    public GraphApiServiceTests()
    {
        var fakeLogger = A.Fake<ILogger<GraphApiService>>();
        _graphApiService = new GraphApiService(fakeLogger);
        _fakeRequestAdapter = A.Fake<IRequestAdapter>();
        _graphClient = new GraphServiceClient(_fakeRequestAdapter);
    }

    [Fact]
    public async Task GetApplicationAsync_ShouldReturnTeamsApp()
    {
        // Arrange
        var appId = "testAppId";
        var cancellationToken = CancellationToken.None;
        var expectedTeamsApp = new TeamsApp { Id = "testAppId" };
        var teamsAppCollectionResponse = new TeamsAppCollectionResponse
        {
            Value = new List<TeamsApp> { expectedTeamsApp }
        };

        A.CallTo(() => _fakeRequestAdapter.SendAsync(
                A<RequestInformation>._,
                TeamsAppCollectionResponse.CreateFromDiscriminatorValue,
                A<Dictionary<string, ParsableFactory<IParsable>>?>._,
                A<CancellationToken>._))
            .Returns(teamsAppCollectionResponse);

        // Act
        var result = await _graphApiService.GetApplicationAsync(_graphClient, appId, cancellationToken);

        // Assert
        Assert.Equal(expectedTeamsApp, result);
    }

    [Fact]
    public async Task SendActivityNotificationAsync_ShouldSendNotification()
    {
        // Arrange
        var notification = new ActivityNotification
        {
            ActivityType = "testActivity",
            PreviewText = new ActivityNotificationPreviewText { Content = "testContent" },
            Recipient = new ActivityNotificationRecipient { UserId = "testUserId" }
        };
        var userId = "testUserId";
        var cancellationToken = CancellationToken.None;

        var fakeSerializationWriterFactory = A.Fake<ISerializationWriterFactory>();
        A.CallTo(() => fakeSerializationWriterFactory.GetSerializationWriter(A<string>._))
            .Returns(A.Fake<ISerializationWriter>());
        A.CallTo(() => _fakeRequestAdapter.SerializationWriterFactory).Returns(fakeSerializationWriterFactory);
        A.CallTo(() => _fakeRequestAdapter.SendNoContentAsync(
                A<RequestInformation>._,
                A<Dictionary<string, ParsableFactory<IParsable>>?>._,
                A<CancellationToken>._))
            .Returns(Task.CompletedTask);

        // Act
        await _graphApiService.SendActivityNotificationAsync(_graphClient, notification, userId, cancellationToken);

        // Assert
        A.CallTo(() => _fakeRequestAdapter.SendNoContentAsync(
                A<RequestInformation>._,
                A<Dictionary<string, ParsableFactory<IParsable>>?>._,
                A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }
}
