using Cake.Common.IO;
using Cake.Common.Tools.DotNetCore;
using Cake.Common.Tools.DotNetCore.Build;
using Cake.Core.IO;
using Cake.Frosting;

namespace Build.Tasks
{
    [Dependency(typeof(RestorePackages))]
    public sealed class Build: FrostingTask<Context>
    {
        public override void Run(Context context)
        {
            var path = context.MakeAbsolute(new DirectoryPath(context.SolutionFilePath));
            context.DotNetCoreBuild(path.FullPath, new DotNetCoreBuildSettings
            {
                Configuration = context.BuildConfiguration,
                NoRestore = true
            });
        }
    }
}