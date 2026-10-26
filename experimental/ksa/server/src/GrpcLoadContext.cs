using System;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;

namespace KRPC.KSA
{
    /// <summary>
    /// The load context for the gRPC server, in the mod's grpc folder, and the ASP.NET Core
    /// assemblies it runs on, in the subfolder for the platform.
    /// </summary>
    /// <remarks>
    /// The game loads its own Microsoft.Extensions assemblies, which ASP.NET Core shares names
    /// with. Assemblies in the folders load here, so ASP.NET Core sees only its own copies. All
    /// others, including KRPC.Core, come from the mod's context, so the gRPC server shares the
    /// server core.
    /// </remarks>
    sealed class GrpcLoadContext : AssemblyLoadContext
    {
        readonly string[] directories;
        readonly AssemblyLoadContext parent;

        public GrpcLoadContext (string directory, AssemblyLoadContext parent) : base ("kRPC.KSA.Grpc")
        {
            // The ASP.NET Core assemblies are compiled ahead of time for one platform, and do
            // not load on another
            string rid;
            if (OperatingSystem.IsWindows ())
                rid = "win-x64";
            else if (OperatingSystem.IsLinux ())
                rid = "linux-x64";
            else
                throw new PlatformNotSupportedException ("The gRPC server runs on Windows and Linux only");
            directories = new [] { directory, Path.Combine (directory, rid) };
            this.parent = parent;
        }

        protected override Assembly Load (AssemblyName name)
        {
            foreach (var directory in directories) {
                var path = Path.Combine (directory, name.Name + ".dll");
                if (File.Exists (path))
                    return LoadFromAssemblyPath (path);
            }
            return parent.LoadFromAssemblyName (name);
        }
    }
}
