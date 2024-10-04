using MicrosoftTeamsIntegration.Jira.Models.Interfaces;

namespace MicrosoftTeamsIntegration.Jira.Extensions
{
    public static class ResolutionContextExtensions
    {
        public static MappingOptions ExtractMappingOptions(this IResolutionContext context)
        {
            var isQueryLinkRequest = false;
            var previewIconPath = default(string);

            if (context.Items.TryGetValue("isQueryLinkRequest", out var val1))
            {
                if (val1 != null)
                {
                    bool.TryParse(val1.ToString(), out isQueryLinkRequest);
                }
            }

            if (context.Items.TryGetValue("previewIconPath", out var val2))
            {
                if (val2 != null)
                {
                    if (!string.IsNullOrWhiteSpace(val2.ToString()))
                    {
                        previewIconPath = val2.ToString();
                    }
                }
            }

            return new MappingOptions
            {
                IsQueryLinkRequest = isQueryLinkRequest,
                PreviewIconPath = previewIconPath
            };
        }
    }
}
