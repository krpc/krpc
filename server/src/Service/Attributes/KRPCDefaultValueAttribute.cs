using System;

namespace KRPC.Service.Attributes
{
    /// <summary>
    /// A default value for a kRPC procedure/class method parameter.
    /// This attribute can be used as a workaround to set the default value to a
    /// non-compile time constant, which is not ordinarily permitted in C#.
    /// </summary>
    [AttributeUsage (AttributeTargets.Parameter)]
    public sealed class KRPCDefaultValueAttribute : Attribute
    {
        /// <summary>
        /// Attach a default parameter to a kRPC procedure/class method parameter.
        /// </summary>
        /// <param name="valueConstructor">
        /// The type of a static class with a static method
        /// named Create that returns an instance of the default value.
        /// </param>
        public KRPCDefaultValueAttribute (Type valueConstructor)
        {
            ValueConstructor = valueConstructor;
        }

        /// <summary>
        /// Type of the class containing a static Create method that
        /// returns an instance of the default value.
        /// </summary>
        public Type ValueConstructor { get; private set; }

        /// <summary>
        /// The default value.
        /// </summary>
        public object Value {
            get {
                return ValueConstructor.GetMethod ("Create").Invoke (null, null);
            }
        }
    }
}
