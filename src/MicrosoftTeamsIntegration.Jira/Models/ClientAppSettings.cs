﻿using System.Reflection;
using Newtonsoft.Json;

namespace MicrosoftTeamsIntegration.Jira.Models
{
    public sealed class ClientAppSettings
    {
        public ClientAppSettings(
            string clientId,
            string baseUrl,
            string microsoftLoginBaseUrl,
            string instrumentationKey,
            string analyticsEnvironment)
        {
            ClientId = clientId;
            BaseUrl = baseUrl;
            MicrosoftLoginBaseUrl = microsoftLoginBaseUrl;
            InstrumentationKey = instrumentationKey;
            AnalyticsEnvironment = analyticsEnvironment;
        }

        [JsonProperty("clientId")]
        public string ClientId { get; }

        [JsonProperty("instrumentationKey")]
        public string InstrumentationKey { get; }

        [JsonProperty("version")]
        public string Version { get; } = Assembly.GetEntryAssembly()?.GetName().Version?.ToString();

        [JsonProperty("baseUrl")]
        public string BaseUrl { get; }

        [JsonProperty("microsoftLoginBaseUrl")]
        public string MicrosoftLoginBaseUrl { get; }

        [JsonProperty("analyticsEnvironment")]
        public string AnalyticsEnvironment { get; }
    }
}
