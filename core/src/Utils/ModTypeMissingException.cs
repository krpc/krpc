using System;

namespace KRPC.Utils
{
    /// <summary>
    /// A type of a mod that kRPC requires is not in the installed version of the mod.
    /// </summary>
    public sealed class ModTypeMissingException : Exception
    {
        /// <summary>
        /// Create the exception for the type of the given mod.
        /// </summary>
        public ModTypeMissingException (string mod, string type)
            : base (mod + ": type " + type + " is not available in the installed version")
        {
            Mod = mod;
            Type = type;
        }

        /// <summary>
        /// The name of the mod.
        /// </summary>
        public string Mod { get; private set; }

        /// <summary>
        /// The name of the type.
        /// </summary>
        public string Type { get; private set; }
    }
}
