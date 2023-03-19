using System.Diagnostics.CodeAnalysis;
using KRPC.Service.Attributes;

namespace KRPC.Service.KRPC
{
    /// <summary>
    /// A null reference was passed to a method that does not accept it as a valid argument.
    /// </summary>
    [SuppressMessage ("Gendarme.Rules.Design", "AvoidVisibleNestedTypesRule")]
    [KRPCException (Service = "KRPC", MappedException = typeof (System.ArgumentNullException))]
    public sealed class ArgumentNullException : System.Exception
    {
        /// <summary>
        /// Construct the exception.
        /// </summary>
        public ArgumentNullException ()
        {
        }

        /// <summary>
        /// Construct the exception.
        /// </summary>
        public ArgumentNullException (string message) :
            base (message)
        {
        }

        /// <summary>
        /// Construct the exception.
        /// </summary>
        public ArgumentNullException (string message, System.Exception innerException) :
            base (message, innerException)
        {
        }
    }
}
