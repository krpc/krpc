using System;
using System.Collections.Generic;
using System.Linq;
using KRPC.Service;
using KRPC.Service.Attributes;

namespace Extending
{
    [KRPCService]
    public static class Crew {
        // [docs]
        public static class DefaultKerbals
        {
            public static IList<string> Create ()
            {
                return new List<string> { "Jeb", "Bill", "Bob" };
            }
        }

        [KRPCProcedure]
        public static void HireKerbals (
            [KRPCDefaultValue (typeof(DefaultKerbals))] IList<string> names)
        {
            // ...
        }
        // [/docs]
    }
}
