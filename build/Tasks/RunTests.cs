using Cake.Common.Tools.DotNetCore;
using Cake.Common.Tools.DotNetCore.Test;
using Cake.Frosting;
using Cake.Npm;
using Cake.Npm.RunScript;

namespace Build.Tasks
{
    [Dependency(typeof(Build))]
    public sealed class RunTests : FrostingTask<Context>
    {
        public override void Run(Context context)
        {
            context.NpmRunScript(new NpmRunScriptSettings
            {
                WorkingDirectory = context.ClientAppPath,
                ScriptName = "test",
                LogLevel = NpmLogLevel.Error
            });

            context.DotNetCoreTest(context.TestsProjectFilePath, new DotNetCoreTestSettings
            {
                NoBuild = true,
                NoRestore = true,
                Configuration = context.BuildConfiguration,
                Logger = "trx"
            });
        }
    }
}
