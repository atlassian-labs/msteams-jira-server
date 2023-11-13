namespace MicrosoftTeamsIntegration.Jira
{
    public static class JiraConstants
    {
        public const string JiraIssueRegex = @"(?i)((?!([A-Z0-9]{1,10})-?$)[A-Z0-9]{1}[A-Z0-9]+-\d+)";
        public const string JiraIssueStrictMatchRegex = @"(?i)(^(\s*)[A-Z0-9]{1}[A-Z0-9]+-\d+)";

        // check it here https://regex101.com/r/1nR3ZK/1
        public const string JiraIssueUrlRegex = @"^https:\/\/(.+)\.(atlassian\.net|jira\.com)\/browse\/(.+)";

        public const string CancelCommandRegex = @"(^(\s*)cancel)";

        public const string AddonIsNotInstalledMessage = "Addon is not installed";
        public const string UserNotAuthorizedMessage = "User not authorized";

        // Should match the MessageService.INVALID_JWT_TOKEN constant in Jira Server app
        public const string InvalidJwtToken = "Invalid JWT token";

        // Should match the RequestService.CONSENT_WAS_REVOKED constant in Jira Server app
        public const string ConsentWasRevoked = "Consent token was revoked";

        // Should correspond to AuthParamMessageHandler.java constants in Jira Server app
        public const string AuthorizationLinkErrorMessage = "Failed to generate authorization link. Please contact your Jira Server administrator and confirm Application link for Microsoft Teams app has been properly configured.";
        public const string TokenRejectedErrorMessage = "OAuth token rejected. Please check if verification code is correct and try to resubmit it or re-authorize again to get new code.";
        public const string UnknownPermissionsErrorMessage = "Unknown permissions. Please attempt to re-authorize again.";

        public const string PiCdnBaseUrl = "https://product-integrations-cdn.atl-paas.net";
    }
}
