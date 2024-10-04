using System.Collections.Generic;
using AutoMapper;
using MicrosoftTeamsIntegration.Jira.Models.Interfaces;

namespace MicrosoftTeamsIntegration.Jira.Models
{
    public class ResolutionContextWrapper : IResolutionContext
    {
        private readonly ResolutionContext _resolutionContext;

        public ResolutionContextWrapper(ResolutionContext resolutionContext)
        {
            _resolutionContext = resolutionContext;
        }

        public object State => _resolutionContext.State;
        public IDictionary<string, object> Items => _resolutionContext.Items;
        public bool TryGetItems(out Dictionary<string, object> items) => _resolutionContext.TryGetItems(out items);

        public IRuntimeMapper Mapper => _resolutionContext.Mapper;
        public Dictionary<ContextCacheKey, object> InstanceCache => _resolutionContext.InstanceCache;
    }
}
