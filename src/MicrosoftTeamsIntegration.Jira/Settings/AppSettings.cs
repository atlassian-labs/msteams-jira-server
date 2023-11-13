namespace MicrosoftTeamsIntegration.Jira.Settings
{
    public class AppSettings
    {
        public string BaseUrl { get; set; }
        public string DatabaseUrl { get; set; }
        public string AddonKey { get; set; }
        public string MicrosoftAppId { get; set; }
        public string MicrosoftAppPassword { get; set; }
        public string StorageConnectionString { get; set; }
        public string BotDataStoreContainer { get; set; }
        public int JiraServerResponseTimeoutInSeconds { get; set; }
        public string SignalRConnectionString { get; set; }
        public string OAuthConnectionName { get; set; }
        public string CacheConnectionString { get; set; }
        public int JiraServerMaximumReceiveMessageSize { get; set; }
        public string MicrosoftLoginBaseUrl { get; set; }

        // space separated list of uls that should be added to CSP list
        public string CspValidDomains { get; set; }
    }
}
