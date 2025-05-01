using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using MicrosoftTeamsIntegration.Artifacts.Services.Interfaces;
using MicrosoftTeamsIntegration.Jira.Exceptions;
using MicrosoftTeamsIntegration.Jira.Models;
using MicrosoftTeamsIntegration.Jira.Models.Jira;
using MicrosoftTeamsIntegration.Jira.Services;
using MicrosoftTeamsIntegration.Jira.Services.Interfaces;
using MicrosoftTeamsIntegration.Jira.Services.SignalR;
using MicrosoftTeamsIntegration.Jira.Services.SignalR.Interfaces;
using Newtonsoft.Json;
using Xunit;

namespace MicrosoftTeamsIntegration.Jira.Tests.Services;

public class NotificationSubscriptionServiceTests
{
    private readonly INotificationSubscriptionDatabaseService _notificationSubscriptionDatabaseService;
    private readonly IDistributedCacheService _distributedCacheService;
    private readonly ISignalRService _signalRService;
    private readonly NotificationSubscriptionService _service;
    private readonly IntegratedUser _integratedUser;

    public NotificationSubscriptionServiceTests()
    {
        ILogger<NotificationSubscriptionService> logger = A.Fake<ILogger<NotificationSubscriptionService>>();
        _notificationSubscriptionDatabaseService = A.Fake<INotificationSubscriptionDatabaseService>();
        _distributedCacheService = A.Fake<IDistributedCacheService>();
        _signalRService = A.Fake<ISignalRService>();
        _service = new NotificationSubscriptionService(
            logger,
            _notificationSubscriptionDatabaseService,
            _distributedCacheService,
            _signalRService);
        _integratedUser = new IntegratedUser
        {
            MsTeamsUserId = "test-user-id",
            JiraServerId = "test-jira-server-id",
            AccessToken = "test-access-token"
        };
    }

    [Theory]
    [InlineData(SubscriptionType.Personal)]
    [InlineData(SubscriptionType.Channel)]
    public async Task CreateNotificationSubscription_ShouldAddNotification_WhenValidDataProvided(SubscriptionType subscriptionType)
    {
        var notification = new NotificationSubscription
        {
            SubscriptionId = "test-subscription-id",
            MicrosoftUserId = "test-user-id",
            JiraId = "test-jira-id",
            SubscriptionType = subscriptionType
        };
        var conversationReferenceId = "test-conversation-ref";
        var conversationReference = "test-conversation-reference";
        var response = new JiraResponse<JiraAuthResponse>()
        {
            Response = new JiraAuthResponse()
            {
                IsSuccess = true
            },
            ResponseCode = (int)HttpStatusCode.OK,
            Message = "Test"
        };

        A.CallTo(() => _distributedCacheService.Get<string>(conversationReferenceId, CancellationToken.None))
            .Returns(conversationReference);
        A.CallTo(() => _signalRService.SendRequestAndWaitForResponse(A<string>._, A<string>._, A<CancellationToken>._))
            .Returns(new SignalRResponse()
            {
                Message = JsonConvert.SerializeObject(response),
                Received = true
            });

        await _service.CreateNotificationSubscription(_integratedUser, notification, conversationReferenceId);

        A.CallTo(() => _distributedCacheService.Get<string>(conversationReferenceId, CancellationToken.None))
            .MustHaveHappenedTwiceExactly();
        A.CallTo(() => _notificationSubscriptionDatabaseService.AddNotificationSubscription(notification))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _signalRService.SendRequestAndWaitForResponse(A<string>._, A<string>._, A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
        Assert.Equal(conversationReference, notification.ConversationReference);
    }

    [Theory]
    [InlineData(SubscriptionType.Personal)]
    [InlineData(SubscriptionType.Channel)]
    public async Task CreateNotificationSubscription_ShouldNotTryToSetAddonSettings_WhenWeDoHavePersonalSubscriptionForJira(SubscriptionType subscriptionType)
    {
        var newNotificationSubscription = new NotificationSubscription
        {
            SubscriptionId = "test-subscription-id",
            MicrosoftUserId = "test-user-id",
            JiraId = "test-jira-id",
            SubscriptionType = subscriptionType
        };
        var conversationReferenceId = "test-conversation-ref";
        var conversationReference = "test-conversation-reference";
        var response = new JiraResponse<JiraAuthResponse>()
        {
            Response = new JiraAuthResponse()
            {
                IsSuccess = true
            },
            ResponseCode = (int)HttpStatusCode.OK,
            Message = "Test"
        };

        A.CallTo(() =>
            _notificationSubscriptionDatabaseService.GetNotificationSubscriptionByJiraId(newNotificationSubscription
                .JiraId)).Returns([
            new NotificationSubscription
            {
                SubscriptionId = "test-subscription-id-2",
                MicrosoftUserId = "test-user-id-2",
                JiraId = "test-jira-id",
                SubscriptionType = SubscriptionType.Personal
            },
            new NotificationSubscription
            {
                SubscriptionId = "test-subscription-id-3",
                MicrosoftUserId = "test-user-id-3",
                JiraId = "test-jira-id",
                SubscriptionType = SubscriptionType.Personal
            },
            new NotificationSubscription
            {
                SubscriptionId = "test-subscription-id-4",
                MicrosoftUserId = "test-user-id-4",
                JiraId = "test-jira-id",
                SubscriptionType = SubscriptionType.Channel
            }

        ]);
        A.CallTo(() => _distributedCacheService.Get<string>(conversationReferenceId, CancellationToken.None))
            .Returns(conversationReference);
        A.CallTo(() => _signalRService.SendRequestAndWaitForResponse(A<string>._, A<string>._, A<CancellationToken>._))
            .Returns(new SignalRResponse()
            {
                Message = JsonConvert.SerializeObject(response),
                Received = true
            });

        await _service.CreateNotificationSubscription(_integratedUser, newNotificationSubscription, conversationReferenceId);

        A.CallTo(() => _distributedCacheService.Get<string>(conversationReferenceId, CancellationToken.None))
            .MustHaveHappenedTwiceExactly();
        A.CallTo(() => _notificationSubscriptionDatabaseService.AddNotificationSubscription(newNotificationSubscription))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _signalRService.SendRequestAndWaitForResponse(A<string>._, A<string>._, A<CancellationToken>._))
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task CreateNotificationSubscription_ShouldNotAddNotification_WhenAddSettingsCanNotBeSet()
    {
        var notification = new NotificationSubscription
        {
            SubscriptionId = "test-subscription-id",
            MicrosoftUserId = "test-user-id"
        };
        var conversationReferenceId = "test-conversation-ref";
        var conversationReference = "test-conversation-reference";
        var response = new JiraResponse<JiraAuthResponse>()
        {
            Response = new JiraAuthResponse()
            {
                IsSuccess = false
            },
            ResponseCode = (int)HttpStatusCode.Forbidden,
            Message = "Test"
        };

        A.CallTo(() => _distributedCacheService.Get<string>(conversationReferenceId, CancellationToken.None))
            .Returns(conversationReference);
        A.CallTo(() => _signalRService.SendRequestAndWaitForResponse(A<string>._, A<string>._, A<CancellationToken>._))
            .Returns(new SignalRResponse()
            {
                Message = JsonConvert.SerializeObject(response),
                Received = true
            });

        await Assert.ThrowsAsync<BadRequestException>(() => _service.CreateNotificationSubscription(_integratedUser, notification, conversationReferenceId));

        A.CallTo(() => _distributedCacheService.Get<string>(conversationReferenceId, CancellationToken.None))
            .MustNotHaveHappened();
        A.CallTo(() => _notificationSubscriptionDatabaseService.AddNotificationSubscription(notification))
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task CreateNotificationSubscription_ShouldLogError_WhenExceptionOccurs()
    {
        var notification = new NotificationSubscription();
        var conversationReferenceId = "test-conversation-ref";
        var exception = new Exception("Test exception");
        var response = new JiraResponse<JiraAuthResponse>()
        {
            Response = new JiraAuthResponse()
            {
                IsSuccess = true
            },
            ResponseCode = (int)HttpStatusCode.OK,
            Message = "Test"
        };

        A.CallTo(() => _distributedCacheService.Get<string>(conversationReferenceId, CancellationToken.None))
            .Throws(exception);
        A.CallTo(() => _signalRService.SendRequestAndWaitForResponse(A<string>._, A<string>._, A<CancellationToken>._))
            .Returns(new SignalRResponse()
            {
                Message = JsonConvert.SerializeObject(response),
                Received = true
            });

        await _service.CreateNotificationSubscription(_integratedUser, notification, conversationReferenceId);

        A.CallTo(() => _notificationSubscriptionDatabaseService.AddNotificationSubscription(A<NotificationSubscription>._))
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task GetNotification_ShouldReturnFirstNotification_WhenNotificationsExist()
    {
        var microsoftUserId = _integratedUser.MsTeamsUserId;
        var expectedNotification = new NotificationSubscription
        {
            MicrosoftUserId = microsoftUserId
        };

        A.CallTo(() => _notificationSubscriptionDatabaseService.GetNotificationSubscriptionByMicrosoftUserId(microsoftUserId))
            .Returns(new[] { expectedNotification });

        var result = await _service.GetNotificationSubscription(_integratedUser);

        Assert.Equal(expectedNotification, result);
        A.CallTo(() => _notificationSubscriptionDatabaseService.GetNotificationSubscriptionByMicrosoftUserId(microsoftUserId))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task GetNotification_ShouldReturnNull_WhenExceptionOccurs()
    {
        var microsoftUserId = _integratedUser.MsTeamsUserId;
        var exception = new Exception("Test exception");

        A.CallTo(() => _notificationSubscriptionDatabaseService.GetNotificationSubscriptionByMicrosoftUserId(microsoftUserId))
            .Throws(exception);

        var result = await _service.GetNotificationSubscription(_integratedUser);

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateNotificationSubscription_ShouldNotUpdateConversationReference_WhenEmptyConversationReferenceId()
    {
        var notification = new NotificationSubscription
        {
            SubscriptionId = "test-subscription-id",
            MicrosoftUserId = _integratedUser.MsTeamsUserId,
            EventTypes = Array.Empty<string>(),
            IsActive = false
        };

        await _service.UpdateNotificationSubscription(_integratedUser, notification);

        A.CallTo(() => _distributedCacheService.Get<string>(A<string>._, CancellationToken.None))
            .MustNotHaveHappened();
        A.CallTo(() => _notificationSubscriptionDatabaseService.UpdateNotificationSubscription(notification.SubscriptionId, notification))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task UpdateNotificationSubscription_ShouldMuteSubscription_WhenThereAreNoEventTypesConfigured()
    {
        var notification = new NotificationSubscription
        {
            SubscriptionId = "test-subscription-id",
            MicrosoftUserId = _integratedUser.MsTeamsUserId,
            EventTypes = Array.Empty<string>(),
            IsActive = true
        };

        await _service.UpdateNotificationSubscription(_integratedUser, notification);

        A.CallTo(() => _distributedCacheService.Get<string>(A<string>._, CancellationToken.None))
            .MustNotHaveHappened();
        A.CallTo(() => _notificationSubscriptionDatabaseService.UpdateNotificationSubscription(notification.SubscriptionId, A<NotificationSubscription>.That.Matches(x => !x.IsActive)))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task UpdateNotificationSubscription_ShouldLogError_WhenExceptionOccurs()
    {
        var notification = new NotificationSubscription();
        var conversationReferenceId = "test-conversation-ref";
        var exception = new Exception("Test exception");

        A.CallTo(() => _distributedCacheService.Get<string>(conversationReferenceId, CancellationToken.None))
            .Throws(exception);

        await _service.UpdateNotificationSubscription(_integratedUser, notification, conversationReferenceId);

        A.CallTo(() => _notificationSubscriptionDatabaseService.UpdateNotificationSubscription(A<string>._, A<NotificationSubscription>._))
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task DeleteNotificationSubscriptionByMicrosoftUserId_ShouldDeletePersonalNotification_WhenExists()
    {
        var microsoftUserId = _integratedUser.MsTeamsUserId;
        var personalNotification = new NotificationSubscription
        {
            SubscriptionId = "test-subscription-id",
            SubscriptionType = SubscriptionType.Personal,
            MicrosoftUserId = microsoftUserId
        };

        A.CallTo(() => _notificationSubscriptionDatabaseService.GetNotificationSubscriptionByMicrosoftUserId(microsoftUserId))
            .Returns(new[] { personalNotification });

        await _service.DeleteNotificationSubscriptionByMicrosoftUserId(_integratedUser);

        A.CallTo(() => _notificationSubscriptionDatabaseService.DeleteNotificationSubscriptionBySubscriptionId(personalNotification.SubscriptionId))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _notificationSubscriptionDatabaseService.GetNotificationSubscriptionByMicrosoftUserId(microsoftUserId))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task DeleteNotificationSubscriptionByMicrosoftUserId_ShouldDeletePersonalNotification_WhenCannotUpdateAddonSettings()
    {
        var microsoftUserId = _integratedUser.MsTeamsUserId;
        var personalNotification = new NotificationSubscription
        {
            SubscriptionId = "test-subscription-id",
            SubscriptionType = SubscriptionType.Personal,
            MicrosoftUserId = microsoftUserId
        };
        var response = new JiraResponse<JiraAuthResponse>()
        {
            Response = new JiraAuthResponse()
            {
                IsSuccess = false
            },
            ResponseCode = (int)HttpStatusCode.Forbidden,
            Message = "Test"
        };

        A.CallTo(() => _notificationSubscriptionDatabaseService.GetNotificationSubscriptionByMicrosoftUserId(microsoftUserId))
            .Returns(new[] { personalNotification });
        A.CallTo(() => _signalRService.SendRequestAndWaitForResponse(A<string>._, A<string>._, A<CancellationToken>._))
            .Returns(new SignalRResponse()
            {
                Message = JsonConvert.SerializeObject(response),
                Received = true
            });

        await _service.DeleteNotificationSubscriptionByMicrosoftUserId(_integratedUser);

        A.CallTo(() => _notificationSubscriptionDatabaseService.DeleteNotificationSubscriptionBySubscriptionId(personalNotification.SubscriptionId))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _notificationSubscriptionDatabaseService.GetNotificationSubscriptionByMicrosoftUserId(microsoftUserId))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task DeleteNotificationSubscriptionByMicrosoftUserId_ShouldNotDelete_WhenNoPersonalNotificationExists()
    {
        var microsoftUserId = _integratedUser.MsTeamsUserId;

        A.CallTo(() => _notificationSubscriptionDatabaseService.GetNotificationSubscriptionByMicrosoftUserId(microsoftUserId))
            .Returns(Array.Empty<NotificationSubscription>());

        await _service.DeleteNotificationSubscriptionByMicrosoftUserId(_integratedUser);

        A.CallTo(() => _notificationSubscriptionDatabaseService.DeleteNotificationSubscriptionBySubscriptionId(A<string>._))
            .MustNotHaveHappened();
        A.CallTo(() => _notificationSubscriptionDatabaseService.GetNotificationSubscriptionByMicrosoftUserId(microsoftUserId))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task DeleteNotificationSubscriptionByMicrosoftUserId_ShouldLogError_WhenExceptionOccurs()
    {
        var microsoftUserId = _integratedUser.MsTeamsUserId;
        var exception = new Exception("Test exception");

        A.CallTo(() => _notificationSubscriptionDatabaseService.GetNotificationSubscriptionByMicrosoftUserId(microsoftUserId))
            .Throws(exception);

        await _service.DeleteNotificationSubscriptionByMicrosoftUserId(_integratedUser);

        A.CallTo(() => _notificationSubscriptionDatabaseService.DeleteNotificationSubscriptionBySubscriptionId(A<string>._))
            .MustNotHaveHappened();
        A.CallTo(() => _notificationSubscriptionDatabaseService.GetNotificationSubscriptionByMicrosoftUserId(microsoftUserId))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task DeleteNotificationSubscriptionByMicrosoftUserId_ShouldNotTryToSetPersonalAddonSettings_WhenWeDoHavePersonalSubscriptionForJira()
    {
        var notificationSubscriptionToRemove = new NotificationSubscription
        {
            SubscriptionId = "test-subscription-id",
            MicrosoftUserId = "test-user-id",
            JiraId = "test-jira-id",
            SubscriptionType = SubscriptionType.Personal
        };
        var conversationReferenceId = "test-conversation-ref";
        var conversationReference = "test-conversation-reference";
        var response = new JiraResponse<JiraAuthResponse>()
        {
            Response = new JiraAuthResponse()
            {
                IsSuccess = true
            },
            ResponseCode = (int)HttpStatusCode.OK,
            Message = "Test"
        };

        A.CallTo(() =>
            _notificationSubscriptionDatabaseService.GetNotificationSubscriptionByJiraId(notificationSubscriptionToRemove
                .JiraId)).Returns([
            new NotificationSubscription
            {
                SubscriptionId = "test-subscription-id-2",
                MicrosoftUserId = "test-user-id-2",
                JiraId = "test-jira-id",
                SubscriptionType = SubscriptionType.Personal
            },
            new NotificationSubscription
            {
                SubscriptionId = "test-subscription-id-3",
                MicrosoftUserId = "test-user-id-3",
                JiraId = "test-jira-id",
                SubscriptionType = SubscriptionType.Personal
            },
            new NotificationSubscription
            {
                SubscriptionId = "test-subscription-id-4",
                MicrosoftUserId = "test-user-id-4",
                JiraId = "test-jira-id",
                SubscriptionType = SubscriptionType.Channel
            }

        ]);
        A.CallTo(() => _notificationSubscriptionDatabaseService.GetNotificationSubscriptionByMicrosoftUserId(A<string>._))
            .Returns([notificationSubscriptionToRemove]);
        A.CallTo(() => _notificationSubscriptionDatabaseService.DeleteNotificationSubscriptionByMicrosoftUserId(A<string>._))
            .Returns(Task.CompletedTask);
        A.CallTo(() => _distributedCacheService.Get<string>(conversationReferenceId, CancellationToken.None))
            .Returns(conversationReference);
        A.CallTo(() => _signalRService.SendRequestAndWaitForResponse(A<string>._, A<string>._, A<CancellationToken>._))
            .Returns(new SignalRResponse()
            {
                Message = JsonConvert.SerializeObject(response),
                Received = true
            });

        await _service.DeleteNotificationSubscriptionByMicrosoftUserId(_integratedUser);

        A.CallTo(() => _notificationSubscriptionDatabaseService.DeleteNotificationSubscriptionBySubscriptionId(notificationSubscriptionToRemove.SubscriptionId))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _signalRService.SendRequestAndWaitForResponse(A<string>._, A<string>._, A<CancellationToken>._))
            .MustNotHaveHappened();
    }
}
