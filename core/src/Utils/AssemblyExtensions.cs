using System.Reflection;

namespace KRPC.Utils
{
    /// <summary>
    /// Extension methods for assemblies.
    /// </summary>
    public static class AssemblyExtensions
    {
        /// <summary>
        /// The version people read, with the dev marker of a dev build.
        /// </summary>
        public static string InformationalVersion (this Assembly assembly)
        {
            return assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute> ().InformationalVersion;
        }
    }
}
