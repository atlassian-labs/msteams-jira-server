using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MicrosoftTeamsIntegration.Jira.Models.Bot;

namespace MicrosoftTeamsIntegration.Jira.Services.Interfaces
{
    public interface ICommandDialogReferenceService
    {
        JiraActionRegexReference GetActionReference(string searchCommand);
    }
}
