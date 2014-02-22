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
        public static string GetMessageTypeName (Type type)
        {
            if (type == null)
                throw new ArgumentException ("null is not a Protocol Buffer message type");
            if (!IsAMessageType (type))
                throw new ArgumentException (type.ToString () + " is not a Protocol Buffer message type");
            return type.FullName.Replace ("KRPC.Schema.", "");
        }

        /// <summary>
        /// Return a builder object for the given Protocol Buffer message type.
        /// The type must be derived from IMessage.
        /// </summary>
        public static IBuilder BuilderForMessageType (Type type)
        {
            if (type == null)
                throw new ArgumentException ("null is not a Protocol Buffer message type");
            if (!IsAMessageType (type))
                throw new ArgumentException (type.ToString () + " is not a Protocol Buffer message type");
            MethodInfo createBuilder = type.GetMethod ("CreateBuilder", new Type[] { });
            return (IBuilder)createBuilder.Invoke (null, null);
        }

        /// <summary>
        /// Returns true if the given type is a Protocol Buffer message type.
        /// </summary>
        public static bool IsAMessageType (Type type)
        {
            return typeof(IMessage).IsAssignableFrom (type);
        }
    }
}

