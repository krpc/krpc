using System;
using System.Reflection;
using Google.ProtocolBuffers;

namespace KRPC.Utils
{
    class ProtocolBuffers
    {
        /// <summary>
        /// Return the string name of the C# type for the Protocol Buffer message type
        /// </summary>
        public static string GetMessageTypeName (Type type) {
            // TODO: Check if type is actually a protocol buffer message type
            return type.FullName.Replace("KRPC.Schema.", "");
        }

        /// <summary>
        /// Return a builder object for the given Protocol Buffer message type.
        /// The type must be derived from IMessage.
        /// </summary>
        public static IBuilder BuilderForMessageType (Type type)
        {
            // TODO: Throw a ArgumentException if we can't get a builder instance (use IsAMessageType)
            MethodInfo createBuilder = type.GetMethod ("CreateBuilder", new Type[] {});
            return (IBuilder) createBuilder.Invoke (null, null);
        }

        /// <summary>
        /// Returns true if the given type is a Protocol Buffer message type.
        /// </summary>
        public static bool IsAMessageType (Type type)
        {
            // TODO: Use extension methods to add method to Type?!?
            // FIXME: Should only return true if the supplied type is a protocol buffer message type
            return true;
        }
    }
}

