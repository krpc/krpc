using System;

namespace KRPC.Client
{
    /// <summary>
    /// Base class for remote objects.
    /// </summary>
    public class RemoteObject : IEquatable<RemoteObject>
    {
        internal Connection connection;

        internal UInt64 _ID { get; private set; }

        internal RemoteObject (Connection connection, UInt64 id = 0)
        {
            this.connection = connection;
            this._ID = id;
        }

        public bool Equals (RemoteObject other)
        {
            return this._ID == other._ID;
        }

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

        public override int GetHashCode ()
        {
            return this._ID.GetHashCode ();
        }

        public static bool operator == (RemoteObject lhs, RemoteObject rhs)
        {
            if (((object)lhs) == null || ((object)rhs) == null)
                return RemoteObject.Equals (lhs, rhs);
            return lhs.Equals (rhs);
        }

        public static bool operator != (RemoteObject lhs, RemoteObject rhs)
        {
            if (((object)lhs) == null || ((object)rhs) == null)
                return !RemoteObject.Equals (lhs, rhs);
            return !lhs.Equals (rhs);
        }
    }
}
