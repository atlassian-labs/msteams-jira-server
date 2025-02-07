using FakeItEasy;
using MicrosoftTeamsIntegration.Artifacts.Services;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace MicrosoftTeamsIntegration.Artifacts.Tests.Services;

public class UserFeedbackServiceTests
{
    private readonly ISendGridClient _fakeSendGridClient;
    private readonly UserFeedbackService _userFeedbackService;
    private const string AppId = "testAppId";
    private const string SendGridApiKey = "testApiKey";
    private const string CustomerSupportEmail = "support@example.com";

    public UserFeedbackServiceTests()
    {
        _fakeSendGridClient = A.Fake<ISendGridClient>();
        _userFeedbackService = new UserFeedbackService(AppId, CustomerSupportEmail, _fakeSendGridClient);
    }

    [Fact]
    public async Task SendFeedbackViaEmail_ShouldSendEmail()
    {
        // Arrange
        var feedbackMessage = "This is a test feedback message.";
        var fromEmail = "user@example.com";
        var cancellationToken = CancellationToken.None;

        A.CallTo(() => _fakeSendGridClient.SendEmailAsync(A<SendGridMessage>._, cancellationToken))
            .Returns(new Response(System.Net.HttpStatusCode.OK, null, null));

        // Act
        await _userFeedbackService.SendFeedbackViaEmail(feedbackMessage, fromEmail, cancellationToken);

        // Assert
        A.CallTo(() => _fakeSendGridClient.SendEmailAsync(
            A<SendGridMessage>.That.Matches(msg =>
                msg.From.Email == fromEmail &&
                msg.Personalizations[0].Tos[0].Email == CustomerSupportEmail &&
                msg.Personalizations[0].Subject == $"New Feedback from {AppId}"), 
            cancellationToken)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public void GetCustomerSupportEmail_ShouldReturnCustomerSupportEmail()
    {
        var userFeedbackService = new UserFeedbackService(AppId, SendGridApiKey, CustomerSupportEmail);
        
        // Act
        var result = userFeedbackService.GetCustomerSupportEmail();

        // Assert
        Assert.Equal(CustomerSupportEmail, result);
    }
}
