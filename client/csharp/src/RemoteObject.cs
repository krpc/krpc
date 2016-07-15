using System;

namespace KRPC.Client
{
    /// <summary>
    /// Base class for remote objects.
    /// </summary>
    public class RemoteObject : IEquatable<RemoteObject>
    {
        internal IConnection connection;

        internal UInt64 id { get; private set; }

        internal RemoteObject (IConnection serverConnection, UInt64 objectId = 0)
        {
            connection = serverConnection;
            id = objectId;
        }

        /// <summary>
        /// Check if remote objects are equivalent.
        /// </summary>
        public bool Equals (RemoteObject other)
        {
            return other != null && id == other.id;
        }

        /// <summary>
        /// Check if this remote object is equivalent to the given object.
        /// </summary>
        public override bool Equals (Object obj)
        {
            if (Object.ReferenceEquals (this, obj))
                return true;
            if (Object.ReferenceEquals (obj, null))
                return false;
            var typedObj = obj as RemoteObject;
            return typedObj != null && Equals (typedObj);
        }

        /// <summary>
        /// Hash the remote object.
        /// </summary>
        public override int GetHashCode ()
        {
            return id.GetHashCode ();
        }

        /// <summary>
        /// Check if two remote objects are equivalent.
        /// </summary>
        public static bool operator == (RemoteObject lhs, RemoteObject rhs)
        {
            if (Object.ReferenceEquals (lhs, null) || Object.ReferenceEquals (rhs, null))
                return Object.ReferenceEquals (lhs, rhs);
            else if (Object.ReferenceEquals (lhs, rhs))
                return true;
            else
                return lhs.Equals (rhs);
        }

        /// <summary>
        /// Check if two remote objects are not equivalent.
        /// </summary>
        public static bool operator != (RemoteObject lhs, RemoteObject rhs)
        {
            if (Object.ReferenceEquals (lhs, null) || Object.ReferenceEquals (rhs, null))
                return !Object.ReferenceEquals (lhs, rhs);
            else if (Object.ReferenceEquals (lhs, rhs))
                return false;
            else
                return !(lhs.Equals (rhs));
        }
    }
}
