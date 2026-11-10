using System;
using System.Linq;
using System.Reflection;
using KRPC.Conformance;

namespace KRPC.Client.Conformance
{
    /// <summary>
    /// The <c>[Obsolete]</c> attribute on the generated stub of a member, which the compiler warns
    /// from. A stream or a call names the member in a lambda, which warns from the same attribute.
    /// </summary>
    static class Deprecation
    {
        public static Value Signals (DeprecationSignals command)
        {
            var value = new Value { List = new Items () };
            var attribute = Member (command.Service, command.Member).GetCustomAttribute<ObsoleteAttribute> ();
            if (attribute != null)
                value.List.Items_.Add (new Value { String = attribute.Message ?? "Obsolete" });
            return value;
        }

        static MemberInfo Member (string service, string member)
        {
            var assembly = typeof (Deprecation).Assembly;
            var prefix = "KRPC.Client.Services." + service + ".";
            // A program reaches a service through its extension method
            if (member.Length == 0)
                return assembly.GetType (prefix + "ExtensionMethods").GetMethod (service);
            var parts = member.Split ('.');
            var type = assembly.GetType (prefix + parts [0]);
            if (parts.Length == 1 && type != null)
                return type;
            if (parts.Length == 1)
                return assembly.GetType (prefix + "Service").GetMember (parts [0]).First ();
            return type.GetMember (parts [1]).First ();
        }
    }
}
