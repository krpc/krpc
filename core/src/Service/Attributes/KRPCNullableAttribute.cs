using System;
using System.Collections.Generic;

namespace KRPC.Service.Attributes
{
    /// <summary>
    /// A nullable value in a kRPC procedure, method or property. On a parameter it marks the
    /// argument as nullable, and on a method or property the return value. Giving it a path of
    /// <see cref="Position"/> values marks a position nested inside that value instead, such as
    /// the elements of a list. The attribute can be applied more than once, for a type with
    /// several nullable positions.
    /// </summary>
    [AttributeUsage (AttributeTargets.Parameter | AttributeTargets.Method |
                     AttributeTargets.Property, AllowMultiple = true)]
    public sealed class KRPCNullableAttribute : Attribute
    {
        readonly Position[] path;

        /// <summary>
        /// Mark the position the given path reaches as nullable. Each step names a position of
        /// the type the step before it reached, outermost first, and an empty path is the value
        /// itself.
        /// </summary>
        public KRPCNullableAttribute (params Position[] path)
        {
            this.path = path ?? new Position [0];
        }

        /// <summary>
        /// The positions to step through to reach the nullable one, outermost first.
        /// </summary>
        public IList<Position> Path {
            get { return path; }
        }
    }
}
