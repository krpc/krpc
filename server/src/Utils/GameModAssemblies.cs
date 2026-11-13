using System.Collections.Generic;
using System.Linq;

namespace KRPC.Utils
{
    /// <summary>
    /// The assemblies the game has loaded from GameData, where the load pass looks for each mod.
    /// </summary>
    sealed class GameModAssemblies : IModAssemblies
    {
        public IEnumerable<LoadedAssembly> Loaded {
            get { return AssemblyLoader.loadedAssemblies.Select (a => new LoadedAssembly (a.assembly, a.path)); }
        }
    }
}
