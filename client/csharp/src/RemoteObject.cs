using System;

namespace KRPC.Client
{
    /// <summary>
    /// Base class for remote objects.
    /// </summary>
    public class RemoteObject : IEquatable<RemoteObject>
    {
        /// <summary>
        /// A connection to the server where the object is stored.
        /// </summary>
        public IConnection connection { get; private set; }

        /// <summary>
        /// The unique identifier for the object on the server.
        /// </summary>
        public ulong id { get; private set; }

        /// <summary>
        /// Construct a remote object.
        /// </summary>
        public RemoteObject (IConnection serverConnection, ulong objectId = 0)
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
        public override bool Equals (object obj)
        {
            if (ReferenceEquals (this, obj))
                return true;
            if (ReferenceEquals (obj, null))
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
            if (ReferenceEquals (lhs, null) || ReferenceEquals (rhs, null))
                return ReferenceEquals (lhs, rhs);
            if (ReferenceEquals (lhs, rhs))
                return true;
            return lhs.Equals (rhs);
        }

        /// <summary>
        /// Check if two remote objects are not equivalent.
        /// </summary>
        public static bool operator != (RemoteObject lhs, RemoteObject rhs)
        {
            if (ReferenceEquals (lhs, null) || ReferenceEquals (rhs, null))
                return !ReferenceEquals (lhs, rhs);
            if (ReferenceEquals (lhs, rhs))
                return false;
            return !(lhs.Equals (rhs));
        }
    }
}
