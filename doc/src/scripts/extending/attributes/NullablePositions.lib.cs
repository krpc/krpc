using System;
using System.Collections.Generic;
using KRPC.Service.Attributes;

namespace Extending
{
    using KRPC.SpaceCenter.Services;

    // [docs]
    using static KRPC.Service.Attributes.Position;

    [KRPCService]
    public static class Fleet {
        [KRPCProcedure]
        public static void Assign (
            [KRPCNullable (Element)] IList<Vessel> crew, // the vessels may be null
            [KRPCNullable (Value)] IDictionary<string, Vessel> byName, // the values may be null
            [KRPCNullable (Item2)] Tuple<string, Vessel> named, // the second item may be null
            [KRPCNullable (Element, Element)] IList<IList<Vessel>> byStage) // the vessels may be null
        {
            // ...
        }
    }
    // [/docs]
}
