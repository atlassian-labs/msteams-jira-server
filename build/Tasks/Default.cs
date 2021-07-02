using Cake.Frosting;
using JetBrains.Annotations;

namespace Build.Tasks
{
	[Dependency(typeof(Cleanup))]
    [Dependency(typeof(RestorePackages))]
	[Dependency(typeof(Build))]
	[Dependency(typeof(RunTests))]
	[Dependency(typeof(Publish))]
	[Dependency(typeof(ZipFiles))]
	[UsedImplicitly]
	public sealed class Default : FrostingTask<Context>
	{
	}
}
