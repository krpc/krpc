using System;
using System.Diagnostics.CodeAnalysis;

namespace KRPC.Utils
{
    /// <summary>
    /// Abstract base class for equatable objects.
    /// Provides implementations of comparison operators.
    /// </summary>
    public abstract class Equatable<T> : IEquatable<T> where T : class
    {
        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public abstract bool Equals (T other);

        /// <summary>
        /// Hash the object.
        /// </summary>
        public override abstract int GetHashCode ();

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public sealed override bool Equals (object obj)
        {
            if (Object.ReferenceEquals (this, obj))
                return true;
            if (Object.ReferenceEquals (obj, null))
                return false;
            var typedObj = obj as T;
            return typedObj != null && Equals (typedObj);
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        [SuppressMessage ("Gendarme.Rules.Design.Generic", "DoNotDeclareStaticMembersOnGenericTypesRule")]
        public static bool operator == (Equatable<T> lhs, Equatable<T> rhs)
        {
            if (Object.ReferenceEquals (lhs, null) || Object.ReferenceEquals (rhs, null))
                return Object.ReferenceEquals (lhs, rhs);
            else if (Object.ReferenceEquals (lhs, rhs))
                return true;
            else
                return lhs.Equals (rhs);
        }

        /// <summary>
        /// Returns true if the objects are not equal.
        /// </summary>
        [SuppressMessage ("Gendarme.Rules.Design.Generic", "DoNotDeclareStaticMembersOnGenericTypesRule")]
        public static bool operator != (Equatable<T> lhs, Equatable<T> rhs)
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
