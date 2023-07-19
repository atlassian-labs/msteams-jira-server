using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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

namespace MicrosoftTeamsIntegration.Jira.Tests
{
    public class BotMessagesServiceTests
    {

        public IMapper Mapper => A.Fake<IMapper>();

        #region Arragements and Helpers
        private readonly IJiraService _fakeJiraService = A.Fake<IJiraService>();
        private readonly IOptions<AppSettings> _appSettings = new OptionsManager<AppSettings>(
            new OptionsFactory<AppSettings>(new List<IConfigureOptions<AppSettings>>(), new List<IPostConfigureOptions<AppSettings>>()));
        private readonly string _yahooLink = @"https://uk.yahoo.com";
        private readonly string _googleLink = @"https://google.com/14521/";
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

        #endregion

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
        public void HandleHtmlMessageFromUser___HtmlContent_WithAttachmentDimension_MoreThanME_WithTagAContainsHref___EmptyString_Test()
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
                    JiraIssues = new JiraIssue[1]
                    {
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
                    }
                });

            A.CallTo(() => _fakeJiraService.GetUserNameOrAccountId(A<IntegratedUser>._)).Returns("user");

            var service = CreateBotMessagesService();
            var result = await service.SearchIssueAndBuildIssueCard(turnContext, JiraDataGenerator.GenerateUser(), string.Empty);

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
                    JiraIssues = new JiraIssue[1]
                    {
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
                    },
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

        private IBotMessagesService CreateBotMessagesService()
        {
            return new BotMessagesService(_appSettings, Mapper, _fakeJiraService);
        }
    }
}
