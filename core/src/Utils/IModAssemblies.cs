using System.Collections.Generic;
using System.Reflection;

namespace KRPC.Utils
{
    /// <summary>
    /// The assemblies a game has loaded, where <see cref="ModAPI"/> looks for each mod.
    /// Each server implements it over its game's list of loaded assemblies.
    /// </summary>
    public interface IModAssemblies
    {
        /// <summary>
        /// The loaded assemblies, each with the path of its file.
        /// </summary>
        IEnumerable<LoadedAssembly> Loaded { get; }
    }

    /// <summary>
    /// An assembly the game has loaded, and the path of its file.
    /// </summary>
    public sealed class LoadedAssembly
    {
        /// <summary>
        /// Create a loaded assembly.
        /// </summary>
        public LoadedAssembly (Assembly assembly, string path)
        {
            Assembly = assembly;
            Path = path;
        }

        /// <summary>
        /// The assembly.
        /// </summary>
        public Assembly Assembly { get; private set; }

        /// <summary>
        /// The path of the assembly's file.
        /// </summary>
        public string Path { get; private set; }
    }
}
