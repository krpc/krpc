using System;
using KRPC.Service.Attributes;

namespace Extending
{
    [KRPCService]
    public static class Lights {
        // [docs]
        /// <summary>
        /// Whether the lights are on. Throws <see cref="InvalidOperationException"/> if set
        /// while <see cref="Broken"/> is true.
        /// </summary>
        [KRPCProperty]
        public static bool Active
        {
            get { throw new NotImplementedException (); }
            set { throw new NotImplementedException (); }
        }
        // [/docs]

        /// <summary>
        /// Whether the lights are broken.
        /// </summary>
        [KRPCProperty]
        public static bool Broken
        {
            get { throw new NotImplementedException (); }
        }
    }
}
