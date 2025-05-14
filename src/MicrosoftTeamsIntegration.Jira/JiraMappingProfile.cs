using AdaptiveCards;
using AutoMapper;
using JetBrains.Annotations;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Schema.Teams;
using MicrosoftTeamsIntegration.Jira.Models;
using MicrosoftTeamsIntegration.Jira.Models.Dto;
using MicrosoftTeamsIntegration.Jira.Models.Jira.Issue;
using MicrosoftTeamsIntegration.Jira.Models.Notifications;
using MicrosoftTeamsIntegration.Jira.Settings;
using MicrosoftTeamsIntegration.Jira.TypeConverters;

namespace MicrosoftTeamsIntegration.Jira
{
    [UsedImplicitly]
    public class JiraMappingProfile : Profile
    {
        public JiraMappingProfile(
            AppSettings appSettings)
        {
            CreateMap<JiraIssueFields, JiraIssueFieldsDto>()
                ;

            CreateMap<JiraIssue, JiraIssueDto>()
                .ForMember(d => d.Id, opt => opt.MapFrom(s => s.Id))
                .ForMember(d => d.Key, opt => opt.MapFrom(s => s.Key))
                .ForMember(d => d.Self, opt => opt.MapFrom(s => s.Self))
                .ForMember(d => d.Fields, opt => opt.MapFrom(s => s.Fields))
                ;

            CreateMap<JiraIssueSearch, JiraIssueSearchResponseDto>()
                .ForMember(d => d.ErrorMessages, opt => opt.MapFrom(s => s.ErrorMessages))
                .ForMember(d => d.JiraIssues, opt => opt.MapFrom(s => s.JiraIssues))
                .ForMember(d => d.Total, opt => opt.MapFrom(s => s.Total))
                ;

            CreateMap<BotAndMessagingExtensionJiraIssue, AdaptiveCard>()
                .ConvertUsing(new JiraIssueToAdaptiveCardTypeConverter(appSettings));

            CreateMap<BotAndMessagingExtensionJiraIssue, ThumbnailCard>()
                .ConvertUsing(new JiraIssueToThumbnailCardTypeConverter(appSettings));

            CreateMap<BotAndMessagingExtensionJiraIssue, MessagingExtensionAttachment>()
                .ConvertUsing(new JiraIssueToMessagingExtensionAttachmentTypeConverter());

            CreateMap<NotificationMessageCardPayload, AdaptiveCard>()
                .ConvertUsing(new NotificationMessageToAdaptiveCardConverter());
        }
    }
}
