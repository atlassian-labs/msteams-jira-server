using Cake.Common.Tools.DotNetCore;
using Cake.Common.Tools.DotNetCore.Publish;
using Cake.Frosting;

namespace Build.Tasks
{
    [Dependency(typeof(Cleanup))]
    public sealed class Publish : FrostingTask<Context>
    {
        public override void Run(Context context)
        {
            context.DotNetCorePublish(
                context.AppProjectPath,
                new DotNetCorePublishSettings
                {
                    //Runtime = "win10-x64",
                    NoRestore = true,
                    Configuration = context.BuildConfiguration,
                    OutputDirectory = context.DistDirectoryPath
                });
        }
    }
}
