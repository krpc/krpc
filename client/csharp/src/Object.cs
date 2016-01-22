using System;

namespace KRPC.Client
{
    public class Object : IEquatable<Object>
    {
        internal Connection connection;

        internal UInt64 _ID { get; private set; }

        public Object (Connection connection, UInt64 id = 0)
        {
            this.connection = connection;
            this._ID = id;
        }

        public bool Equals (Object other)
        {
            return this._ID == other._ID;
        }

        public override bool Equals (System.Object obj)
        {
            if (obj == null)
                return false;
            Object obj2 = obj as Object;
            if (obj2 == null)
                return false;
            else
                return Equals (obj2);
        }

        public override int GetHashCode ()
        {
            return this._ID.GetHashCode ();
        }

        public static bool operator == (Object lhs, Object rhs)
        {
            if (((object)lhs) == null || ((object)rhs) == null)
                return Object.Equals (lhs, rhs);
            return lhs.Equals (rhs);
        }

        public static bool operator != (Object lhs, Object rhs)
        {
            if (((object)lhs) == null || ((object)rhs) == null)
                return !Object.Equals (lhs, rhs);
            return !lhs.Equals (rhs);
        }
    }
}
