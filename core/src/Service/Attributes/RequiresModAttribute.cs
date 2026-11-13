using System;

namespace KRPC.Service.Attributes
{
    /// <summary>
    /// The mod integrations a service, class or member needs, which must all be available. With
    /// several attributes, any one of them will do. A call fails while none is met. The nearest
    /// attributes apply: a member inherits them from its class, and a class from its service. A
    /// static property of a service named <c>Available</c> is exempt.
    /// </summary>
    [AttributeUsage (AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Property,
                     AllowMultiple = true)]
    public sealed class RequiresModAttribute : Attribute
    {
        /// <summary>
        /// Require all of the integrations <paramref name="types"/>, each a subclass of
        /// <see cref="Utils.ModAPI"/>.
        /// </summary>
        public RequiresModAttribute (params Type[] types)
        {
            Types = types ?? new Type [0];
        }

        /// <summary>
        /// The integrations, each a subclass of <see cref="Utils.ModAPI"/>.
        /// </summary>
        public Type[] Types { get; private set; }
    }
}
