using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace MicrosoftTeamsIntegration.Artifacts.Services.Interfaces
{
    [PublicAPI]
    public interface IUserFeedbackService
    {
        Task SendFeedbackViaEmail(string feedbackMessage, string fromEmail = "", CancellationToken cancellationToken = default);
        string GetCustomerSupportEmail();
    }
}
