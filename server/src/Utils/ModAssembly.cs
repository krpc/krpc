using System.Linq;

namespace KRPC.Utils
{
    /// <summary>
    /// The main assembly of a mod the game has loaded, found by name.
    /// </summary>
    public static class ModAssembly
    {
        /// <summary>
        /// Whether the game has loaded the assembly of the given name.
        /// </summary>
        public static bool IsLoaded (string name)
        {
            return AssemblyLoader.loadedAssemblies.Any (a => a.assembly.GetName ().Name == name);
        }
    }
}
