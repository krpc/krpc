using System;
using System.Reflection;
using System.Collections.Generic;

namespace KRPC.Utils
{
    static class Reflection
    {
        /// <summary>
        /// Returns all types with the specified attribute, from all assemblies.
        /// </summary>
        public static IEnumerable<Type> GetTypesWith<TAttribute> (bool inherit = false)
            where TAttribute : Attribute
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
                var types = new List<Type> ();
                try {
                    foreach (var type in assembly.GetTypes()) {
                        if (type.IsDefined (typeof(TAttribute), inherit)) {
                            types.Add (type);
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
        public static IEnumerable<MethodInfo> GetMethodsWith<TAttribute> (Type cls, bool inherit = false)
            where TAttribute : Attribute
        {
            foreach (var method in cls.GetMethods ()) {
                if (method.IsDefined (typeof(TAttribute), inherit)) {
                    yield return method;
                }
            }
        }

        /// <summary>
        /// Returns all properties within a given type that have the specified attribute.
        /// </summary>
        public static IEnumerable<PropertyInfo> GetPropertiesWith<TAttribute> (Type cls, bool inherit = false)
            where TAttribute : Attribute
        {
            foreach (var property in cls.GetProperties ()) {
                if (property.IsDefined (typeof(TAttribute), inherit)) {
                    yield return property;
                }
            }
        }

        /// <summary>
        /// Returns all methods within a given type that have the specified attribute.
        /// </summary>
        public static IEnumerable<Type> GetClassesWith<TAttribute> (Type cls, bool inherit = false)
            where TAttribute : Attribute
        {
            foreach (var nestedType in cls.GetNestedTypes()) {
                if (nestedType.IsClass && nestedType.IsDefined (typeof(TAttribute), inherit)) {
                    yield return nestedType;
                }
            }
        }
    }
}
