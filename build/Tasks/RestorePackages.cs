using Cake.Common.Build;
using Cake.Common.Tools.DotNetCore;
using Cake.Frosting;
using Cake.Npm;
using Cake.Npm.Install;

namespace Build.Tasks
{
    [Dependency(typeof(Cleanup))]
    public sealed class RestorePackages: FrostingTask<Context>
    {
        public override void Run(Context context)
        {
            if (context.BuildSystem().IsLocalBuild)
            {
                context.DotNetCoreRestore(context.SolutionFilePath);
            }
            
            context.NpmInstall(new NpmInstallSettings
            {
                WorkingDirectory = context.ClientAppPath,
                Production = false,
                NoOptional = true,
                LogLevel = NpmLogLevel.Error
            });
        }
    }
}
