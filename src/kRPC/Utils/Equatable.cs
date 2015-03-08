using System;

namespace KRPC.Utils
{
    public abstract class Equatable<T> : IEquatable<T>
    {
        public abstract bool Equals (T obj);

        public override abstract int GetHashCode ();

        public override bool Equals (object obj)
        {
            if (Object.ReferenceEquals (obj, null))
                return false;
            if (obj is T)
                return Equals ((T)obj);
            return false;
        }

        public static bool operator == (Equatable<T> lhs, Equatable<T> rhs)
        {
            if (Object.ReferenceEquals (lhs, rhs))
                return true;
            if (Object.ReferenceEquals (lhs, null))
                return false;
            return lhs.Equals (rhs);
        }

        public static bool operator != (Equatable<T> lhs, Equatable<T> rhs)
        {
            if (Object.ReferenceEquals (lhs, rhs))
                return false;
            if (Object.ReferenceEquals (lhs, null))
                return true;
            return !(lhs.Equals (rhs));
        }
    }
}
