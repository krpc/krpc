using System;
using System.Reflection;
using System.Collections.Generic;
using Google.ProtocolBuffers; // TODO: remove dependency

namespace KRPC.Utils
{
    class Reflection
    {
        /// <summary>
        /// Returns all types with the specified attribute, from all assemblies.
        /// </summary>
        public static IEnumerable<Type> GetTypesWith<TAttribute>(bool inherit = false)
            where TAttribute : System.Attribute
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
                List<Type> types = new List<Type>();
                try {
                    foreach (var type in assembly.GetTypes()) {
                        if (type.IsDefined (typeof(TAttribute), inherit)) {
                            types.Add(type);
                        }
                    }
                } catch {
                    // Assembly is not accessible
                }
                foreach (var type in types) {
                    yield return type;
                }
            }
        }

        /// <summary>
        /// Returns all methods within a given type that have the specified attribute.
        /// </summary>
        public static IEnumerable<MethodInfo> GetMethodsWith<TAttribute>(Type cls, bool inherit = false)
            where TAttribute : System.Attribute
        {
            foreach (var method in cls.GetMethods ()) {
                if (method.IsDefined (typeof(TAttribute), inherit)) {
                    yield return method;
                }
            }
        }

        /// <summary>
        /// Return the string type name of the protocol buffer message represented by the given type
        /// </summary>
        // TODO: move somewhere else?
        public static string GetMessageTypeName (Type type) {
            // TODO: error checking - check if type is an IMessage?
            return type.FullName.Replace("KRPC.Schema.", "");
        }

        // TODO: move somewhere else?
        public static IBuilder GetBuilderForType (Type type)
        {
            // TODO: Throw a ArgumentException if we can't get a builder instance
            MethodInfo createBuilder = type.GetMethod ("CreateBuilder", new Type[] {});
            return (IBuilder) createBuilder.Invoke (null, null);
        }

        // TODO: Move somewhere else?
        public static bool IsAMessageType (Type type)
        {
            // TODO: Use extension methods to add method to Type?!?
            // FIXME: Returns true if the supplied type is a protocol buffer type (implements IMessage)
            return true;
        }
    }
}
