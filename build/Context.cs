using Cake.Core;
using Cake.Frosting;
using JetBrains.Annotations;

namespace Build
{
    [UsedImplicitly]
    public class Context : FrostingContext
    {
        public string Target { get; set; }
        public string BuildConfiguration { get; set; }
        public string BuildNumber { get; set; }
        public string DistDirectoryPath { get; set; }
        public string AppProjectPath { get; set; }
        public string TestsProjectFilePath { get; set; }
        public string PackageFullName { get; set; }
        public string PackageNameFormat { get; set; }
        public bool IsGatedBuild { get; set; }
        public string SolutionFilePath { get; set; }
        public string TestResultsPath { get; set; }
        public string ClientAppPath { get; set; }

        public Context(ICakeContext context) 
            : base(context)
        {
        }
    }
}
