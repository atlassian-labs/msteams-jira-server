using Cake.Common.IO;
using Cake.Frosting;

namespace Build.Tasks
{
    public sealed class Cleanup: FrostingTask<Context>
    {
        public override void Run(Context context)
        {
            context.CleanDirectory(context.DistDirectoryPath);
            context.CleanDirectory(context.TestResultsPath);
        }
    }
}
