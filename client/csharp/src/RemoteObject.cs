using System;

namespace KRPC.Client
{
    /// <summary>
    /// Base class for remote objects.
    /// </summary>
    public class RemoteObject : IEquatable<RemoteObject>
    {
        internal IConnection connection;

        internal UInt64 _ID { get; private set; }

        internal RemoteObject (IConnection connection, UInt64 id = 0)
        {
            this.connection = connection;
            _ID = id;
        }

        /// <summary>
        /// Check if remote objects are equivalent.
        /// </summary>
        public bool Equals (RemoteObject other)
        {
            return _ID == other._ID;
        }

        /// <summary>
        /// Check if this remote object is equivalent to the given object.
        /// </summary>
        public override bool Equals (Object obj)
        {
            if (obj == null)
                return false;
            RemoteObject obj2 = obj as RemoteObject;
            if (obj2 == null)
                return false;
            else
                return Equals (obj2);
        }

        /// <summary>
        /// Hash the remote object.
        /// </summary>
        public override int GetHashCode ()
        {
            return _ID.GetHashCode ();
        }

        /// <summary>
        /// Check if two remote objects are equivalent.
        /// </summary>
        public static bool operator == (RemoteObject lhs, RemoteObject rhs)
        {
            if (((object)lhs) == null || ((object)rhs) == null)
                return RemoteObject.Equals (lhs, rhs);
            return lhs.Equals (rhs);
        }

        /// <summary>
        /// Check if two remote objects are not equivalent.
        /// </summary>
        public static bool operator != (RemoteObject lhs, RemoteObject rhs)
        {
            if (((object)lhs) == null || ((object)rhs) == null)
                return !RemoteObject.Equals (lhs, rhs);
            return !lhs.Equals (rhs);
        }
    }
}
