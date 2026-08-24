using System;
using System.Collections.Generic;

namespace KRPC.Utils
{
    /// <summary>
    /// A hash code built from the fields that identify an object, written as
    /// <c>Hash.Of (first).And (second).And (third)</c>.
    /// </summary>
    /// <remarks>
    /// Each field is folded in at a place of its own, so which field a value came from is
    /// part of the result and two objects holding the same values in different fields
    /// hash differently. Any number of fields can be folded in.
    ///
    /// A field of null counts as zero. A field is only ever asked for its own hash code,
    /// so nothing it points at is dereferenced, which is what an object standing for
    /// something the game can destroy needs.
    /// </remarks>
    public struct Hash : IEquatable<Hash>
    {
        // An arbitrary starting value, and an odd multiplier that moves what has been
        // folded in so far clear of the field being added.
        const int seed = 17;
        const int multiplier = 31;

        readonly int value;

        Hash (int value)
        {
            this.value = value;
        }

        /// <summary>
        /// Start a hash code with an object's first identifying field.
        /// </summary>
        public static Hash Of<T> (T field)
        {
            return new Hash (seed).And (field);
        }

        /// <summary>
        /// Fold in the next field.
        /// </summary>
        public Hash And<T> (T field)
        {
            unchecked {
                return new Hash (
                    (value * multiplier) + EqualityComparer<T>.Default.GetHashCode (field));
            }
        }

        /// <summary>
        /// The hash code built so far.
        /// </summary>
        public int Value {
            get { return value; }
        }

        /// <summary>
        /// The hash code built so far, so that it can be returned directly from
        /// <c>GetHashCode</c>.
        /// </summary>
        public static implicit operator int (Hash hash)
        {
            return hash.Value;
        }

        /// <summary>
        /// Whether two hash codes are the same.
        /// </summary>
        public bool Equals (Hash other)
        {
            return value == other.value;
        }

        /// <summary>
        /// Whether two hash codes are the same.
        /// </summary>
        public override bool Equals (object obj)
        {
            return obj is Hash && Equals ((Hash)obj);
        }

        /// <summary>
        /// The hash code built so far, so that hashing a hash gives the same answer as
        /// reading it.
        /// </summary>
        public override int GetHashCode ()
        {
            return value;
        }

        /// <summary>
        /// Whether two hash codes are the same.
        /// </summary>
        public static bool operator == (Hash lhs, Hash rhs)
        {
            return lhs.Equals (rhs);
        }

        /// <summary>
        /// Whether two hash codes differ.
        /// </summary>
        public static bool operator != (Hash lhs, Hash rhs)
        {
            return !lhs.Equals (rhs);
        }
    }
}
