using Cake.Common.IO;
using Cake.Frosting;

namespace Build.Tasks
{
    public sealed class ZipFiles : FrostingTask<Context>
    {
        public override bool ShouldRun(Context context) => !context.IsGatedBuild;

        public override void Run(Context context)
        {
            context.Zip(context.DistDirectoryPath, context.PackageFullName);
        }
    }
}
