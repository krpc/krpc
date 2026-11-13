using System;

namespace KRPC.Utils
{
    /// <summary>
    /// A member of a mod that kRPC binds to is not in the installed version of the mod.
    /// </summary>
    public sealed class ModMemberMissingException : MissingMemberException
    {
        /// <summary>
        /// Create the exception for the member of the given mod.
        /// </summary>
        public ModMemberMissingException (string mod, string member, string message) : base (message)
        {
            Mod = mod;
            Member = member;
        }

        /// <summary>
        /// The name of the mod.
        /// </summary>
        public string Mod { get; private set; }

        /// <summary>
        /// The member, qualified by the name of its type.
        /// </summary>
        public string Member { get; private set; }
    }
}
