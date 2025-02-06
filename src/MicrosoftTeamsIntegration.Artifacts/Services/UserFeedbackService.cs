using System;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MicrosoftTeamsIntegration.Artifacts.Services.Interfaces;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace MicrosoftTeamsIntegration.Artifacts.Services
{
    [PublicAPI]
    public class UserFeedbackService : IUserFeedbackService
    {
        private readonly string _appId;
        private readonly string _customerSupportEmail;
        private readonly ISendGridClient _sendGridClient;

        public UserFeedbackService(string appId, string sendGridApiKey, string customerSupportEmail)
        {
            _appId = appId;
            _customerSupportEmail = customerSupportEmail;
            _sendGridClient = new SendGridClient(sendGridApiKey);
        }

        public UserFeedbackService(string appId, string customerSupportEmail, ISendGridClient sendGridClient)
        {
            _appId = appId;
            _customerSupportEmail = customerSupportEmail;
            _sendGridClient = sendGridClient;
        }

        public async Task SendFeedbackViaEmail(string feedbackMessage, string fromEmail = "", CancellationToken cancellationToken = default)
        {
            var content = $"{feedbackMessage}{Environment.NewLine}{(!string.IsNullOrEmpty(fromEmail) ? $"User Email: {fromEmail}" : string.Empty)}";
            if (string.IsNullOrEmpty(fromEmail))
            {
                fromEmail = "feedback@do-not-reply.com";
            }

            await _sendGridClient.SendEmailAsync(
                MailHelper.CreateSingleEmail(
                    new EmailAddress(fromEmail),
                    new EmailAddress(_customerSupportEmail),
                    $"New Feedback from {_appId}",
                    content,
                    content), cancellationToken);
        }

        public string GetCustomerSupportEmail() => _customerSupportEmail;
    }
}
