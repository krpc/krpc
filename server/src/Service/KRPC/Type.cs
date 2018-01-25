using System.Diagnostics.CodeAnalysis;
using KRPC.Service.Attributes;

namespace KRPC.Service.KRPC
{
    /// <summary>
    /// A server side expression.
    /// </summary>
    [KRPCClass (Service = "KRPC")]
    [SuppressMessage ("Gendarme.Rules.Smells", "AvoidSpeculativeGeneralityRule")]
    public class Type
    {
        internal System.Type InternalType { get; private set; }

        internal Type (System.Type type)
        {
            InternalType = type;
        }

        /// <summary>
        /// Double type.
        /// </summary>
        [KRPCMethod]
        public static Type Double ()
        {
            return new Type (typeof (double));
        }

        /// <summary>
        /// Float type.
        /// </summary>
        [KRPCMethod]
        public static Type Float ()
        {
            return new Type (typeof (float));
        }

        /// <summary>
        /// Int type.
        /// </summary>
        [KRPCMethod]
        public static Type Int ()
        {
            return new Type (typeof (int));
        }

        /// <summary>
        /// Bool type.
        /// </summary>
        [KRPCMethod]
        public static Type Bool ()
        {
            return new Type (typeof (bool));
        }

        /// <summary>
        /// String type.
        /// </summary>
        [KRPCMethod]
        public static Type String ()
        {
            return new Type (typeof (string));
        }
    }
}
