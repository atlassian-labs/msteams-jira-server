using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AdaptiveCards;
using AutoMapper;
using FakeItEasy;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Options;
using MicrosoftTeamsIntegration.Jira.Models;
using MicrosoftTeamsIntegration.Jira.Models.Jira.Issue;
using MicrosoftTeamsIntegration.Jira.Services;
using MicrosoftTeamsIntegration.Jira.Services.Interfaces;
using MicrosoftTeamsIntegration.Jira.Settings;
using Xunit;

namespace MicrosoftTeamsIntegration.Jira.Tests.Services
{
    public class BotMessagesServiceTests
    {
        public IMapper Mapper => A.Fake<IMapper>();

        private readonly IJiraService _fakeJiraService = A.Fake<IJiraService>();

        private readonly IOptions<AppSettings> _appSettings = new OptionsManager<AppSettings>(
            new OptionsFactory<AppSettings>(
                new List<IConfigureOptions<AppSettings>>(),
                new List<IPostConfigureOptions<AppSettings>>()));

        private readonly string _yahooLink = @"https://uk.yahoo.com";
        private readonly string _googleLink = @"https://google.com/14521/";
        private readonly IAnalyticsService _analyticsService = A.Fake<IAnalyticsService>();
        private string _result = string.Empty;

        private class AttachmentData
        {
            public Activity MockActivity { get; set; }
            public string ImgContent { get; set; }
            public string HtmlContentWithoutTagA { get; set; }
            public string HtmlContentWithTagA0CustomLink { get; set; }
            public string HtmlContentWithTagA1YahooLink { get; set; }
            public string HtmlContentWithTagA2GoogleLink { get; set; }
        }

        // Verify that we get empty string whether html content contains no <a> tag
        [Fact]
        public void HandleHtmlMessageFromUser_HtmlContentWithoutTagA_EmptyString_Test()
        {
            // Arrange
            var testData = BuildTestData();

            testData.MockActivity.Attachments = new List<Attachment>
            {
                new Attachment
                {
                    Content = testData.ImgContent,
                    Name = "Test image",
                    ContentType = "image"
                },
                new Attachment
                {
                    Content = testData.HtmlContentWithoutTagA,
                    Name = "html",
                    ContentType = "text/html"
                }
            };

            // Act
            var service = CreateBotMessagesService();
            _result = service.HandleHtmlMessageFromUser(testData.MockActivity);

            // Assert
            Assert.Equal(string.Empty, _result);
        }

        // Verify that we get proper link, whether html content contains <a> tag and no empty href attribute
        [Fact]
        public void HandleHtmlMessageFromUser_HtmlContentWithTagAContainsHrefGoogleLink_GoogleLink_Test()
        {
            // Arrange
            var testData = BuildTestData(_googleLink);

            testData.MockActivity.Attachments = new List<Attachment>
            {
                new Attachment
                {
                    Content = testData.ImgContent,
                    Name = "Test image",
                    ContentType = "image"
                },
                new Attachment
                {
                    Content = testData.HtmlContentWithTagA0CustomLink,
                    Name = "html",
                    ContentType = "text/html"
                }
            };

            // Act
            var service = CreateBotMessagesService();
            _result = service.HandleHtmlMessageFromUser(testData.MockActivity);

            // Assert
            Assert.Equal(_googleLink, _result);
        }

        // Verify that we get empty string, whether html content contains <a> tag with empty href attribute
        [Fact]
        public void HandleHtmlMessageFromUser_HtmlContentWithTagAContainsEmptyHref_EmptyString_Test()
        {
            // Arrange
            var testData = BuildTestData();

            testData.MockActivity.Attachments = new List<Attachment>
            {
                new Attachment
                {
                    Content = testData.ImgContent,
                    Name = "Test image",
                    ContentType = "image"
                },
                new Attachment
                {
                    Content = testData.HtmlContentWithTagA0CustomLink,
                    Name = "html",
                    ContentType = "text/html"
                }
            };

            // Act
            var service = CreateBotMessagesService();
            _result = service.HandleHtmlMessageFromUser(testData.MockActivity);

            // Assert
            Assert.Equal(string.Empty, _result);
        }

        // Verify that we get empty string, whether html content contains few <a> tags with href attributes
        [Fact]
        public void
            HandleHtmlMessageFromUser___HtmlContent_WithAttachmentDimension_MoreThanME_WithTagAContainsHref___EmptyString_Test()
        {
            // Arrange
            var testData = BuildTestData(_googleLink);

            testData.MockActivity.Attachments = new List<Attachment>
            {
                new Attachment
                {
                    Content = testData.ImgContent,
                    ContentType = "image",
                    Name = "Test image"
                },
                new Attachment
                {
                    Content = testData.HtmlContentWithTagA0CustomLink,
                    Name = "html",
                    ContentType = "text/html"
                },
                new Attachment
                {
                    Content = testData.HtmlContentWithTagA1YahooLink,
                    Name = "html_1",
                    ContentType = "text/html"
                },
                new Attachment
                {
                    Content = testData.HtmlContentWithTagA2GoogleLink,
                    Name = "html_2",
                    ContentType = "text/html"
                }
            };

            // Act
            var service = CreateBotMessagesService();
            _result = service.HandleHtmlMessageFromUser(testData.MockActivity);

            // Assert
            Assert.Equal(string.Empty, _result);
        }

        [Fact]
        public async Task SearchIssueAndBuildIssueCard()
        {
            var activity = new Activity
            {
                Recipient = new ChannelAccount()
                {
                    Id = "Id",
                    Name = "Name"
                },
                From = new ChannelAccount()
                {
                    Id = "Id",
                    Name = "Name"
                },
                Id = "Id",
                ServiceUrl = "ServiceUrl",
                ChannelId = "ChannelId",
                Conversation = new ConversationAccount()
                {
                    IsGroup = false,
                    Id = "Id",
                    Name = "Name"
                },
                Locale = "Locale"
            };

            var testAdapter = new TestAdapter(Channels.Test);

            var turnContext = A.Fake<ITurnContext>();

            A.CallTo(() => turnContext.Activity).Returns(activity);
            A.CallTo(() => turnContext.Adapter).Returns(testAdapter);

            A.CallTo(() => _fakeJiraService.Search(A<IntegratedUser>._, A<SearchForIssuesRequest>._))
                .Returns(new JiraIssueSearch()
                {
                    JiraIssues =
                    [
                        new JiraIssue()
                        {
                            Id = "id",
                            Key = "Key",
                            Fields = new JiraIssueFields()
                            {
                                Type = new JiraIssueType()
                                {
                                    Name = "TestName"
                                }
                            }
                        }

                    ]
                });

            A.CallTo(() => _fakeJiraService.GetUserNameOrAccountId(A<IntegratedUser>._)).Returns("user");

            var service = CreateBotMessagesService();
            var result =
                await service.SearchIssueAndBuildIssueCard(turnContext, JiraDataGenerator.GenerateUser(), string.Empty);

            Assert.NotNull(result);
            A.CallTo(() => _fakeJiraService.GetUserNameOrAccountId(A<IntegratedUser>._)).MustHaveHappened();
        }

        [Fact]
        public async Task BuildAndUpdateJiraIssueCard()
        {
            var activity = new Activity
            {
                Recipient = new ChannelAccount()
                {
                    Id = "Id",
                    Name = "Name"
                },
                From = new ChannelAccount()
                {
                    Id = "Id",
                    Name = "Name"
                },
                Id = "Id",
                ServiceUrl = "ServiceUrl",
                ChannelId = "ChannelId",
                Conversation = new ConversationAccount()
                {
                    IsGroup = false,
                    Id = "Id",
                    Name = "Name"
                },
                Locale = "Locale",
            };

            var testAdapter = new TestAdapter(Channels.Test);

            var turnContext = A.Fake<ITurnContext>();

            A.CallTo(() => turnContext.Activity).Returns(activity);
            A.CallTo(() => turnContext.Adapter).Returns(testAdapter);
            A.CallTo(() => turnContext.UpdateActivityAsync(A<Activity>._, CancellationToken.None))
                .Returns(new ResourceResponse(string.Empty));

            A.CallTo(() => _fakeJiraService.Search(A<IntegratedUser>._, A<SearchForIssuesRequest>._))
                .Returns(new JiraIssueSearch()
                {
                    JiraIssues =
                    [
                        new JiraIssue()
                        {
                            Id = "id",
                            Key = "Key",
                            Fields = new JiraIssueFields()
                            {
                                Type = new JiraIssueType()
                                {
                                    Name = "TestName"
                                }
                            }
                        }

                    ],
                });

            A.CallTo(() => _fakeJiraService.GetUserNameOrAccountId(A<IntegratedUser>._)).Returns("user");

            var service = CreateBotMessagesService();

            await service.BuildAndUpdateJiraIssueCard(turnContext, JiraDataGenerator.GenerateUser(), string.Empty);

            A.CallTo(() => turnContext.UpdateActivityAsync(A<Activity>._, CancellationToken.None))
                .MustHaveHappened();
        }

        [Fact]
        public async Task SendAuthorizationCard()
        {
            var activity = new Activity
            {
                Conversation = new ConversationAccount()
                {
                    TenantId = "Id"
                }
            };

            var turnContext = A.Fake<ITurnContext>();

            A.CallTo(() => turnContext.Activity).Returns(activity);

            var service = CreateBotMessagesService();

            await service.SendAuthorizationCard(turnContext, string.Empty, CancellationToken.None);

            A.CallTo(() => turnContext.Activity).MustHaveHappened();
        }

        [Fact]
        public async Task HandleConversationUpdates_BotAddedToConversation_ShouldSendWelcomeCard()
        {
            // Arrange
            var activity = new Activity
            {
                MembersAdded = new List<ChannelAccount> { new ChannelAccount { Id = "botId" } },
                Recipient = new ChannelAccount { Id = "botId" },
                Conversation = new ConversationAccount { IsGroup = false },
                From = new ChannelAccount { AadObjectId = "userId" }
            };

            var turnContext = A.Fake<ITurnContext>();

            A.CallTo(() => turnContext.Activity).Returns(activity);

            var service = CreateBotMessagesService();

            // Act
            await service.HandleConversationUpdates(turnContext, CancellationToken.None);

            // Assert
            A.CallTo(() => _analyticsService.SendTrackEvent(
                "userId",
                "bot",
                "installed",
                "botApplication",
                string.Empty,
                A<InstallationUpdatesTrackEventAttributes>._)).MustHaveHappened();
        }

        [Fact]
        public async Task HandleConversationUpdates_NoMembersAddedOrRemoved_ShouldNotSendAnyEvent()
        {
            // Arrange
            var activity = new Activity
            {
                MembersAdded = new List<ChannelAccount>(),
                MembersRemoved = new List<ChannelAccount>(),
                Recipient = new ChannelAccount { Id = "botId" },
                Conversation = new ConversationAccount { IsGroup = false },
                From = new ChannelAccount { AadObjectId = "userId" }
            };

            var turnContext = A.Fake<ITurnContext>();

            A.CallTo(() => turnContext.Activity).Returns(activity);

            var service = CreateBotMessagesService();

            // Act
            await service.HandleConversationUpdates(turnContext, CancellationToken.None);

            // Assert
            A.CallTo(() => _analyticsService.SendTrackEvent(
                A<string>._,
                A<string>._,
                A<string>._,
                A<string>._,
                A<string>._,
                A<InstallationUpdatesTrackEventAttributes>._)).MustNotHaveHappened();
        }

        [Fact]
        public async Task SendConnectCard_ShouldSendConnectCard()
        {
            // Arrange
            var activity = new Activity
            {
                Conversation = new ConversationAccount
                {
                    TenantId = "Id",
                    IsGroup = true
                }
            };

            var turnContext = A.Fake<ITurnContext>();

            A.CallTo(() => turnContext.Activity).Returns(activity);

            var service = CreateBotMessagesService();

            // Act
            await service.SendConnectCard(turnContext, CancellationToken.None);

            // Assert
            A.CallTo(() => turnContext.SendActivityAsync(A<IActivity>._, A<CancellationToken>._)).MustHaveHappened();
        }

        [Fact]
        public void BuildConfigureNotificationsCard_GroupConversation_ShouldReturnCorrectCard()
        {
            // Arrange
            var activity = new Activity
            {
                Conversation = new ConversationAccount { IsGroup = true }
            };
            var turnContext = A.Fake<ITurnContext>();
            A.CallTo(() => turnContext.Activity).Returns(activity);

            var service = CreateBotMessagesService();

            // Act
            var card = service.BuildConfigureNotificationsCard(turnContext);

            // Assert
            Assert.NotNull(card);
            Assert.Contains("🔔 Channel notifications", card.Body.OfType<AdaptiveTextBlock>().FirstOrDefault()?.Text);
        }

        [Fact]
        public void BuildConfigureNotificationsCard_PersonalConversation_ShouldReturnCorrectCard()
        {
            // Arrange
            var activity = new Activity
            {
                Conversation = new ConversationAccount { IsGroup = false }
            };
            var turnContext = A.Fake<ITurnContext>();
            A.CallTo(() => turnContext.Activity).Returns(activity);

            var service = CreateBotMessagesService();

            // Act
            var card = service.BuildConfigureNotificationsCard(turnContext);

            // Assert
            Assert.NotNull(card);
            Assert.Contains("🔔 Personal notifications", card.Body.OfType<AdaptiveTextBlock>().FirstOrDefault()?.Text);
        }

        [Fact]
        public void BuildNotificationConfigurationSummaryCard_ShouldReturnCorrectCard()
        {
            // Arrange
            var subscription = new NotificationSubscription
            {
                EventTypes = new string[] { "ActivityIssueAssignee", "MentionedOnIssue" }
            };
            var service = CreateBotMessagesService();

            // Act
            var card = service.BuildNotificationConfigurationSummaryCard(subscription, showSuccessMessage: true);

            // Assert
            Assert.NotNull(card);
            Assert.Contains("You successfully subscribed to notifications", card.Body.OfType<AdaptiveContainer>().FirstOrDefault()?.Items.OfType<AdaptiveTextBlock>().FirstOrDefault()?.Text);
            Assert.Contains("Updates on issues you **assigned** to", card.Body.OfType<AdaptiveContainer>().FirstOrDefault()?.Items.OfType<AdaptiveTextBlock>().Skip(2).FirstOrDefault()?.Text);
            Assert.Contains("Someone **mentioned** you", card.Body.OfType<AdaptiveContainer>().FirstOrDefault()?.Items.OfType<AdaptiveTextBlock>().LastOrDefault()?.Text);
        }

        [Fact]
        public async Task SendConfigureNotificationsCard_GroupConversation_ShouldSendCard()
        {
            // Arrange
            var activity = new Activity
            {
                Conversation = new ConversationAccount { IsGroup = true }
            };
            var turnContext = A.Fake<ITurnContext>();
            A.CallTo(() => turnContext.Activity).Returns(activity);

            var service = CreateBotMessagesService();

            // Act
            await service.SendConfigureNotificationsCard(turnContext, CancellationToken.None);

            // Assert
            A.CallTo(() => turnContext.SendActivityAsync(A<IActivity>._, A<CancellationToken>._)).MustHaveHappened();
        }

        private AttachmentData BuildTestData(string href = "")
        {
            var testData = new AttachmentData
            {
                MockActivity = A.Fake<Activity>(),
                ImgContent = "<img src=\"pic_trulli.jpg\" alt=\"Italian Trulli\">",

                HtmlContentWithoutTagA =
                    "<!DOCTYPE html>" +
                    "<html>" +
                    "<title> HTML Tutorial </title>" +
                    "<body>" +
                    "<div class=\"Header\">" +
                    "<h1>This is a Heading</h1>" +
                    "<p><span style=\"color:blue\">This is a paragraph.<br></span></p>" +
                    "</div>" +
                    "</body>" +
                    "</html>",

                HtmlContentWithTagA0CustomLink =
                    "<!DOCTYPE html>" +
                    "<html>" +
                    "<title> HTML Tutorial </title>" +
                    "<body>" +
                    "<div class=\"Header\">" +
                    "<h1>This is a Heading</h1>" +
                    "<p><span style=\"color:blue\">This is a paragraph.<br></span></p>" +
                    $"<div class\"http\"><p>\\n\\n\\n\\n\\n\\n\\n\\n\\This part is for determine\\r\\r\\r\\r\\r\\r\\r\\r\\r\\ parse behavior with tag a \\n \\r <a href=\"{href}\">This is title for tag a</a></p></div>" +
                    "</div>" +
                    "</body>" +
                    "</html>",

                HtmlContentWithTagA1YahooLink =
                    "<!DOCTYPE html>" +
                    "<html>" +
                    "<title> HTML Tutorial </title>" +
                    "<body>" +
                    "<div class=\"Header\">" +
                    "<h1>This is a Heading</h1>" +
                    "<p><span style=\"color:blue\">This is a paragraph.<br></span></p>" +
                    $"<div class\"http\"><p>\\n\\n\\n\\n\\n\\n\\n\\n\\This part is for determine\\r\\r\\r\\r\\r\\r\\r\\r\\r\\ parse behavior with tag a \\n \\r <a href=\"{_yahooLink}\">This is title for tag a</a></p></div>" +
                    "</div>" +
                    "</body>" +
                    "</html>",

                HtmlContentWithTagA2GoogleLink =
                    "<!DOCTYPE html>" +
                    "<html>" +
                    "<title> HTML Tutorial </title>" +
                    "<body>" +
                    "<div class=\"Header\">" +
                    "<h1>This is a Heading</h1>" +
                    "<p><span style=\"color:blue\">This is a paragraph.<br></span></p>" +
                    $"<div class\"http\"><p>\\n\\n\\n\\n\\n\\n\\n\\n\\This part is for determine\\r\\r\\r\\r\\r\\r\\r\\r\\r\\ parse behavior with tag a \\n \\r <a href=\"{_googleLink}\">This is title for tag a</a></p></div>" +
                    "</div>" +
                    "</body>" +
                    "</html>"
            };
            return testData;
        }

        private IBotMessagesService CreateBotMessagesService()
        {
            return new BotMessagesService(_appSettings, Mapper, _fakeJiraService, _analyticsService);
        }
    }
}
