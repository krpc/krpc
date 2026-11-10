using System;
using KRPC.Service.Attributes;
using KRPC.Utils;

namespace TestServer
{
    /// <summary>
    /// A deprecated service, whose members are deprecated only through it, other than one class.
    /// </summary>
    [KRPCService]
    [Obsolete ("Use TestService instead.")]
    public static class DeprecatedService
    {
        /// <summary>
        /// A procedure.
        /// </summary>
        [KRPCProcedure]
        public static string Procedure ()
        {
            return "value";
        }

        /// <summary>
        /// A property.
        /// </summary>
        [KRPCProperty]
        public static string Property { get; set; } = "value";

        /// <summary>
        /// A class.
        /// </summary>
        [KRPCClass]
        public sealed class ServiceClass : Equatable<ServiceClass>
        {
            public sealed override bool Equals (ServiceClass other)
            {
                return !ReferenceEquals (other, null);
            }

            public sealed override int GetHashCode ()
            {
                return 0;
            }

            /// <summary>
            /// A method.
            /// </summary>
            [KRPCMethod]
            public string Method ()
            {
                return "value";
            }

            /// <summary>
            /// A static method.
            /// </summary>
            [KRPCMethod]
            public static string StaticMethod ()
            {
                return "value";
            }
        }

        /// <summary>
        /// A class deprecated with a reason of its own.
        /// </summary>
        [KRPCClass]
        [Obsolete ("Use \"TestService.TestClass\" instead.")]
        public sealed class ReasonedClass : Equatable<ReasonedClass>
        {
            public sealed override bool Equals (ReasonedClass other)
            {
                return !ReferenceEquals (other, null);
            }

            public sealed override int GetHashCode ()
            {
                return 0;
            }

            /// <summary>
            /// A static method.
            /// </summary>
            [KRPCMethod]
            public static string StaticMethod ()
            {
                return "value";
            }
        }

        /// <summary>
        /// An enumeration.
        /// </summary>
        [KRPCEnum]
        [Serializable]
        public enum ServiceEnum
        {
            /// <summary>
            /// A value.
            /// </summary>
            Value
        }

        /// <summary>
        /// A structure.
        /// </summary>
        [KRPCStruct]
        public struct ServiceStruct
        {
            /// <summary>
            /// A field.
            /// </summary>
            [KRPCProperty]
            public int Field { get; set; }
        }

        /// <summary>
        /// A procedure returning a structure.
        /// </summary>
        [KRPCProcedure]
        public static ServiceStruct StructProcedure ()
        {
            return new ServiceStruct ();
        }

        /// <summary>
        /// An exception.
        /// </summary>
        [KRPCException]
        public sealed class ServiceException : Exception
        {
            public ServiceException ()
            {
            }

            public ServiceException (string message)
            : base (message)
            {
            }

            public ServiceException (string message, Exception innerException)
            : base (message, innerException)
            {
            }
        }
    }
}
