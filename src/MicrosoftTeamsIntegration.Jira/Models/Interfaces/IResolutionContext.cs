using System.Collections.Generic;
using AutoMapper;

namespace MicrosoftTeamsIntegration.Jira.Models.Interfaces
{
    public interface IResolutionContext
    {
        object State { get; }
        IDictionary<string, object> Items { get; }
        bool TryGetItems(out Dictionary<string, object> items);
        IRuntimeMapper Mapper { get; }
        Dictionary<ContextCacheKey, object> InstanceCache { get; }
    }
}
