using System;

namespace KRPC.Utils
{
    /// <summary>
    /// Abstract base class for equatable objects.
    /// Provides implementations of comparison operators.
    /// </summary>
    public abstract class Equatable<T> : IEquatable<T>
    {
        /// <summary>
        /// Returns true if the objects are equal
        /// </summary>
        public abstract bool Equals (T obj);

        /// <summary>
        /// Hash function
        /// </summary>
        public override abstract int GetHashCode ();

        /// <summary>
        /// Returns true if the objects are equal
        /// </summary>
        public override bool Equals (object obj)
        {
            if (Object.ReferenceEquals (obj, null))
                return false;
            if (obj is T)
                return Equals ((T)obj);
            return false;
        }

        /// <summary>
        /// Returns true if the lhs equals the rhs
        /// </summary>
        public static bool operator == (Equatable<T> lhs, Equatable<T> rhs)
        {
            if (Object.ReferenceEquals (lhs, rhs))
                return true;
            if (Object.ReferenceEquals (lhs, null))
                return false;
            return lhs.Equals (rhs);
        }

        /// <summary>
        /// Returns true if the lhs does not equal the rhs
        /// </summary>
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
