using System.Diagnostics.CodeAnalysis;
using KRPC.Service.Attributes;

namespace KRPC.Service.KRPC
{
    /// <summary>
    /// A method call was made to a method that is invalid
    /// given the current state of the object.
    /// </summary>
    [SuppressMessage ("Gendarme.Rules.Design", "AvoidVisibleNestedTypesRule")]
    [KRPCException (Service = "KRPC", MappedException = typeof (System.InvalidOperationException))]
    public sealed class InvalidOperationException : System.Exception
    {
        /// <summary>
        /// Construct the exception.
        /// </summary>
        public InvalidOperationException ()
        {
        }

        /// <summary>
        /// Construct the exception.
        /// </summary>
        public InvalidOperationException (string message) :
            base (message)
        {
        }

        /// <summary>
        /// Construct the exception.
        /// </summary>
        public InvalidOperationException (string message, System.Exception innerException) :
            base (message, innerException)
        {
        }
    }
}
