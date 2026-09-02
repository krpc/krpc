using System;
using System.Collections.Concurrent;
using System.Reflection;
using KRPC.Client.Attributes;

namespace KRPC.Client
{
    /// <summary>
    /// The type specs a generated service declares for its procedures, reached through the RPC
    /// attribute on a stub. A stub names the spec of every value it encodes and decodes, and a
    /// call built from an expression has only the C# types of those values, which cannot say
    /// that a reference-typed position inside a collection is nullable.
    /// </summary>
    static class ProcedureSpecs
    {
        sealed class Lookup
        {
            public Func<string, TypeSpec> ReturnSpec;
            public Func<string, TypeSpec[]> ParameterSpecs;
        }

        static readonly ConcurrentDictionary<Type, Lookup> lookups =
            new ConcurrentDictionary<Type, Lookup> ();

        /// <summary>
        /// The spec of the value the procedure returns, or the spec the given type gives on
        /// its own where the service declares no nullable position inside it.
        /// </summary>
        public static TypeSpec ReturnSpec (RPCAttribute attribute, Type type)
        {
            var lookup = LookupFor (attribute.Types);
            var spec = lookup == null ? null : lookup.ReturnSpec (attribute.Procedure);
            return spec ?? TypeSpec.For (type);
        }

        /// <summary>
        /// The spec of the argument at the given position of the procedure, or the spec the
        /// given type gives on its own.
        /// </summary>
        public static TypeSpec ParameterSpec (RPCAttribute attribute, int position, Type type)
        {
            var lookup = LookupFor (attribute.Types);
            var specs = lookup == null ? null : lookup.ParameterSpecs (attribute.Procedure);
            if (specs != null && position < specs.Length)
                return specs [position];
            return TypeSpec.For (type);
        }

        /// <summary>
        /// The lookup a service's generated spec class gives. A service generated before the
        /// class existed names none, leaving the C# type as all there is.
        /// </summary>
        static Lookup LookupFor (Type types)
        {
            if (ReferenceEquals (types, null))
                return null;
            Lookup lookup;
            if (lookups.TryGetValue (types, out lookup))
                return lookup;
            return lookups.GetOrAdd (types, BuildLookup (types));
        }

        static Lookup BuildLookup (Type types)
        {
            var returnSpec = types.GetMethod (
                "ReturnSpec", BindingFlags.Public | BindingFlags.Static);
            var parameterSpecs = types.GetMethod (
                "ParameterSpecs", BindingFlags.Public | BindingFlags.Static);
            if (returnSpec == null || parameterSpecs == null)
                return null;
            return new Lookup {
                ReturnSpec = (Func<string, TypeSpec>)Delegate.CreateDelegate (
                    typeof(Func<string, TypeSpec>), returnSpec),
                ParameterSpecs = (Func<string, TypeSpec[]>)Delegate.CreateDelegate (
                    typeof(Func<string, TypeSpec[]>), parameterSpecs)
            };
        }
    }
}
