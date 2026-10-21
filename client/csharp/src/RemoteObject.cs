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

        readonly ulong id;

        /// <summary>
        /// Construct a remote object.
        /// </summary>
        public RemoteObject (IConnection serverConnection, ulong objectId = 0)
        {
            connection = serverConnection;
            id = objectId;
        }

        /// <summary>
        /// The identifier of a remote object on the server. Throws an exception if the object
        /// is null.
        /// </summary>
        /// <param name="obj">The remote object.</param>
        /// <returns>The identifier, e.g. to pass to <c>Expression.ConstantObject</c>.</returns>
        public static ulong GetObjectId (RemoteObject obj)
        {
            if (ReferenceEquals (obj, null))
                throw new ArgumentNullException (nameof (obj));
            return obj.id;
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
