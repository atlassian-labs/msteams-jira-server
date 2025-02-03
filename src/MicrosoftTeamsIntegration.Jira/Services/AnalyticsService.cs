using System;
using System.Collections.Generic;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Bot.Builder;
using Microsoft.Extensions.Options;
using MicrosoftTeamsIntegration.Jira.Models;
using MicrosoftTeamsIntegration.Jira.Services.Interfaces;
using MicrosoftTeamsIntegration.Jira.Settings;
using Newtonsoft.Json;

namespace MicrosoftTeamsIntegration.Jira.Services
{
    public class AnalyticsService : IAnalyticsService
    {
        private const string AnalyticsProduct = "jiraMsTeams";

        private readonly ITelemetryClient _telemetry;
        private readonly AppSettings _appSettings;

        public AnalyticsService(TelemetryClient telemetry, IOptions<AppSettings> appSettings)
        {
            _telemetry = new TelemetryClientWrapper(telemetry);
            _appSettings = appSettings.Value;
        }

        public AnalyticsService(ITelemetryClient telemetry, IOptions<AppSettings> appSettings)
        {
            _telemetry = telemetry;
            _appSettings = appSettings.Value;
        }

        public void SendBotDialogEvent(ITurnContext context, string dialogName, string dialogAction, string errorMessage = "")
        {
            string userId = context.Activity.From.AadObjectId;
            bool isGroupConversation = context.Activity.Conversation.IsGroup.GetValueOrDefault();

            SendTrackEvent(
                userId,
                dialogName,
                dialogAction,
                "dialog",
                null,
                new DialogAnalyticsEventAttributes()
                {
                    DialogType = dialogName,
                    IsGroupConversation = isGroupConversation,
                    ErrorMessage = errorMessage
                });
        }

        public void SendTrackEvent(
            string userId,
            string source,
            string action,
            string actionSubject,
            string actionSubjectId,
            IAnalyticsEventAttribute attributes = null)
        {
            PageViewTelemetry pageViewTelemetry = new PageViewTelemetry($"{actionSubjectId}{actionSubject}{action}");
            pageViewTelemetry.Properties.Add(new KeyValuePair<string, string>("event", JsonConvert.SerializeObject(new
            {
                analyticsEnv = _appSettings.AnalyticsEnvironment,
                common = new
                {
                    anonymousId = userId,
                    timestamp = DateTime.Now,
                    product = AnalyticsProduct
                },
                data = new
                {
                    type = "track",
                    source,
                    action,
                    actionSubject,
                    actionSubjectId,
                    attributes
                }
            })));

            _telemetry.TrackPageView(pageViewTelemetry);
        }

        public void SendUiEvent(
            string userId,
            string source,
            string action,
            string actionSubject,
            string actionSubjectId,
            IAnalyticsEventAttribute attributes = null)
        {
            PageViewTelemetry pageViewTelemetry = new PageViewTelemetry($"{actionSubjectId}{actionSubject}{action}");
            pageViewTelemetry.Properties.Add(new KeyValuePair<string, string>("event", JsonConvert.SerializeObject(new
            {
                analyticsEnv = _appSettings.AnalyticsEnvironment,
                common = new
                {
                    anonymousId = userId,
                    timestamp = DateTime.Now,
                    product = AnalyticsProduct
                },
                data = new
                {
                    type = "ui",
                    source,
                    action,
                    actionSubject,
                    actionSubjectId,
                    attributes
                }
            })));

            _telemetry.TrackPageView(pageViewTelemetry);
        }

        public void SendScreenEvent(
            string userId,
            string source,
            string action,
            string actionSubject,
            string name,
            IAnalyticsEventAttribute attributes = null)
        {
            PageViewTelemetry pageViewTelemetry = new PageViewTelemetry($"{name}{actionSubject}{action}");
            pageViewTelemetry.Properties.Add(new KeyValuePair<string, string>("event", JsonConvert.SerializeObject(new
            {
                analyticsEnv = _appSettings.AnalyticsEnvironment,
                common = new
                {
                    anonymousId = userId,
                    timestamp = DateTime.Now,
                    product = AnalyticsProduct
                },
                data = new
                {
                    type = "screen",
                    source,
                    action,
                    actionSubject,
                    name,
                    attributes
                }
            })));

            _telemetry.TrackPageView(pageViewTelemetry);
        }
    }
}
