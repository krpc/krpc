using System;

namespace KRPC.Service.Attributes
{
    /// <summary>
    /// A default value for a kRPC procedure parameter.
    /// This attribute can be used as a workaround to set the default value to a
    /// non-compile time constant, which is not ordinarily permitted in C#.
    /// </summary>
    [AttributeUsage (AttributeTargets.Method, AllowMultiple = true)]
    public sealed class KRPCDefaultValueAttribute : Attribute
    {
        /// <summary>
        /// Attach a default parameter to a kRPC procedure parameter.
        /// </summary>
        /// <param name="name">Name of the parameter.</param>
        /// <param name="valueConstructor">
        /// The type of a static class with a static method
        /// named Create that returns an instance of the default value.
        /// </param>
        public KRPCDefaultValueAttribute (string name, Type valueConstructor)
        {
            Name = name;
            ValueConstructor = valueConstructor;
        }

        /// <summary>
        /// Name of the parameter.
        /// </summary>
        public string Name { get; private set; }

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
