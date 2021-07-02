using System.IO;
using Cake.Common;
using Cake.Common.Build;
using Cake.Common.IO;
using Cake.Frosting;
using JetBrains.Annotations;

namespace Build
{
    [UsedImplicitly]
    public sealed class Lifetime : FrostingLifetime<Context>
    {
        public override void Setup(Context context)
        {
            // arguments
            context.Target = context.Argument("target", "Default");
            context.BuildConfiguration = context.Argument("configuration", "Release");
            context.IsGatedBuild = context.Argument("isGatedBuild", false);

            // constants
            context.AppProjectPath = "./src/MicrosoftTeamsIntegration.Jira";
            context.SolutionFilePath = "./MicrosoftTeamsJiraIntegration.sln";
            context.TestsProjectFilePath = "./tests/MicrosoftTeamsIntegration.Jira.Tests/MicrosoftTeamsIntegration.Jira.Tests.csproj";
            context.TestResultsPath = "./tests/MicrosoftTeamsIntegration.Jira.Tests/TestResults";
            context.DistDirectoryPath = "./dist";
            context.ClientAppPath = $"{context.AppProjectPath}/ClientApp";

            // global variables
            context.BuildNumber = context.TFBuild().Environment.Build.Number;
            if (string.IsNullOrEmpty(context.BuildNumber))
            {
                context.BuildNumber = "255.255.255.255";
            }

            var type = context.Environment.GetEnvironmentVariable("IntegrationType");
            context.PackageNameFormat = !string.IsNullOrEmpty(type) && type.Equals(Constants.JiraServer) ?
                $"JiraServer.{context.BuildNumber}.zip" :
                $"JiraCloud.{context.BuildNumber}.zip";
            
            var artifactDirectoryPath = context.EnvironmentVariable("BUILD_ARTIFACTSTAGINGDIRECTORY");
            if (string.IsNullOrEmpty(artifactDirectoryPath))
            {
                artifactDirectoryPath = "./Build.ArtifactStagingDirectory";
                context.CleanDirectory(artifactDirectoryPath);
            }
            
            context.PackageFullName = Path.Combine(artifactDirectoryPath, context.PackageNameFormat);
        }
    }
}
