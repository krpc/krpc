using System;
using KRPC.Service.Attributes;

namespace Extending
{
    // [docs]
    [Flags]
    [KRPCEnum (Service = "EVA")]
    public enum SuitLights {
        None = 0,
        Helmet = 1,
        Shoulders = 2,
        All = Helmet | Shoulders
    }
    // [/docs]
}
