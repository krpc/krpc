using KRPC.Service.Attributes;

namespace TestServer.Names
{
    /// <summary>
    /// A class named after the class a generated module or namespace declares for its service.
    /// </summary>
    [KRPCClass (Service = "TestService")]
    public sealed class Service
    {
    }

    /// <summary>
    /// A class named after the class that reaches a service in the generated C# code.
    /// </summary>
    [KRPCClass (Service = "TestService")]
    public sealed class ExtensionMethods
    {
    }

    /// <summary>
    /// A class named after the namespace of server forms in the generated JavaScript.
    /// </summary>
    [KRPCClass (Service = "TestService")]
    public sealed class Server
    {
    }

    /// <summary>
    /// A class named after the alias of the service in the generated JavaScript.
    /// </summary>
    [KRPCClass (Service = "TestService")]
    public sealed class ClientService
    {
    }

    /// <summary>
    /// A class named after the connection type of the generated Python module.
    /// </summary>
    [KRPCClass (Service = "TestService")]
    public sealed class TestServiceConnection
    {
    }

    /// <summary>
    /// A class named Client.
    /// </summary>
    [KRPCClass (Service = "TestService")]
    public sealed class Client
    {
    }

    /// <summary>
    /// A class named Stream.
    /// </summary>
    [KRPCClass (Service = "TestService")]
    public sealed class Stream
    {
    }

    /// <summary>
    /// A class named Connection.
    /// </summary>
    [KRPCClass (Service = "TestService")]
    public sealed class Connection
    {
    }

    /// <summary>
    /// A class named Encoder.
    /// </summary>
    [KRPCClass (Service = "TestService")]
    public sealed class Encoder
    {
    }

    /// <summary>
    /// A class named ByteString.
    /// </summary>
    [KRPCClass (Service = "TestService")]
    public sealed class ByteString
    {
    }

    /// <summary>
    /// A class named RemoteObject.
    /// </summary>
    [KRPCClass (Service = "TestService")]
    public sealed class RemoteObject
    {
    }

    /// <summary>
    /// A class named RPCException.
    /// </summary>
    [KRPCClass (Service = "TestService")]
    public sealed class RPCException
    {
    }

    /// <summary>
    /// A class named String.
    /// </summary>
    [KRPCClass (Service = "TestService")]
    public sealed class String
    {
    }

    /// <summary>
    /// A class named Exception.
    /// </summary>
    [KRPCClass (Service = "TestService")]
    public sealed class Exception
    {
    }

    /// <summary>
    /// A class named Override.
    /// </summary>
    [KRPCClass (Service = "TestService")]
    public sealed class Override
    {
    }

    /// <summary>
    /// A class named IConnection.
    /// </summary>
    [KRPCClass (Service = "TestService")]
    public sealed class IConnection
    {
    }

    /// <summary>
    /// A class named TypeSpecs.
    /// </summary>
    [KRPCClass (Service = "TestService")]
    public sealed class TypeSpecs
    {
    }

    /// <summary>
    /// A class named Serializable.
    /// </summary>
    [KRPCClass (Service = "TestService")]
    public sealed class Serializable
    {
    }

    /// <summary>
    /// A class named IEquatable.
    /// </summary>
    [KRPCClass (Service = "TestService")]
    public sealed class IEquatable
    {
    }

    /// <summary>
    /// A class named UInt64.
    /// </summary>
    [KRPCClass (Service = "TestService")]
    public sealed class UInt64
    {
    }

    /// <summary>
    /// A class named Tuple.
    /// </summary>
    [KRPCClass (Service = "TestService")]
    public sealed class Tuple
    {
    }

    /// <summary>
    /// A class named List.
    /// </summary>
    [KRPCClass (Service = "TestService")]
    public sealed class List
    {
    }

    /// <summary>
    /// A class named Optional.
    /// </summary>
    [KRPCClass (Service = "TestService")]
    public sealed class Optional
    {
    }

    /// <summary>
    /// A class named Event.
    /// </summary>
    [KRPCClass (Service = "TestService")]
    public sealed class Event
    {
    }

    /// <summary>
    /// A class named DocEnum.
    /// </summary>
    [KRPCClass (Service = "TestService")]
    public sealed class DocEnum
    {
    }

    /// <summary>
    /// A class named NamedTuple.
    /// </summary>
    [KRPCClass (Service = "TestService")]
    public sealed class NamedTuple
    {
    }

    /// <summary>
    /// A class named after a keyword of Python.
    /// </summary>
    [KRPCClass (Service = "TestService")]
    public sealed class None
    {
    }

    /// <summary>
    /// A class named after the class a class with static methods nests.
    /// </summary>
    [KRPCClass (Service = "TestService")]
    public sealed class Bound
    {
        /// <summary>
        /// A static method, which gives the class its nested class of static methods.
        /// </summary>
        [KRPCMethod]
        public static int Make ()
        {
            return 0;
        }
    }

    /// <summary>
    /// A class named ClassBase.
    /// </summary>
    [KRPCClass (Service = "TestService")]
    public sealed class ClassBase
    {
    }

    /// <summary>
    /// An exception named RuntimeError.
    /// </summary>
    [KRPCException (Service = "TestService")]
    public sealed class RuntimeError : System.Exception
    {
        /// <summary>
        /// Create the exception.
        /// </summary>
        public RuntimeError (string message) : base (message)
        {
        }
    }

    /// <summary>
    /// A class whose members are named after names in the generated code.
    /// </summary>
    [KRPCClass (Service = "TestService")]
    public sealed class Names
    {
        /// <summary>
        /// A method named after a C++ integer type.
        /// </summary>
        [KRPCMethod]
        public static int Uint64T ()
        {
            return 0;
        }

        /// <summary>
        /// A property named after a Python builtin.
        /// </summary>
        [KRPCProperty]
        public int Property { get; set; }

        /// <summary>
        /// A property declared after the property named Property.
        /// </summary>
        [KRPCProperty]
        public int Value { get; set; }

        /// <summary>
        /// A method named after a Python builtin.
        /// </summary>
        [KRPCMethod]
        public int Float ()
        {
            return 0;
        }

        /// <summary>
        /// A method named after the Python client package.
        /// </summary>
        [KRPCMethod]
        public int Krpc ()
        {
            return 0;
        }

        /// <summary>
        /// A method declared after methods named after Python builtins, with defaults that
        /// name them.
        /// </summary>
        [KRPCMethod]
        public double WithDefaults (double x = double.NaN, int y = int.MaxValue)
        {
            return x + y;
        }

        /// <summary>
        /// A structure with the given value in its field named Bound.
        /// </summary>
        /// <param name="bound">The value.</param>
        [KRPCMethod]
        public static NamesStruct MakeStruct (int bound)
        {
            return new NamesStruct { Bound = bound };
        }

        /// <summary>
        /// A static method named after the method that binds the static methods of a class
        /// to a connection.
        /// </summary>
        [KRPCMethod]
        public static int Bind ()
        {
            return 0;
        }

        /// <summary>
        /// A method named after the class that holds the bound static methods of a class.
        /// </summary>
        [KRPCMethod]
        public int Bound ()
        {
            return 0;
        }

        /// <summary>
        /// A method named after a method of a Penlight class.
        /// </summary>
        [KRPCMethod]
        public int ClassOf ()
        {
            return 0;
        }

        /// <summary>
        /// A method named after a method of java.lang.Object.
        /// </summary>
        [KRPCMethod]
        public int HashCode ()
        {
            return 0;
        }

        /// <summary>
        /// A method named after a method of java.lang.Object.
        /// </summary>
        [KRPCMethod]
        public string ToString (int value)
        {
            return string.Empty;
        }

        /// <summary>
        /// A static method with parameters named after names in the generated code.
        /// </summary>
        [KRPCMethod]
        public static int Parameters (int client, int connection, int instance, int returnValue, int returnValueIsNull, int NULL, int krpc, int java, int com, int org, int nameof, int self, int cls, int invoke, int procedureCall)
        {
            return client + connection + instance + returnValue + returnValueIsNull + NULL + krpc + java + com + org + nameof + self + cls + invoke + procedureCall;
        }

        /// <summary>
        /// A method with parameters named after names in the generated code.
        /// </summary>
        [KRPCMethod]
        public int InstanceParameters (int client, int connection, int instance, int returnValue, int returnValueIsNull, int NULL, int krpc, int java, int com, int org, int nameof, int self, int cls, int invoke, int procedureCall)
        {
            return client + connection + instance + returnValue + returnValueIsNull + NULL + krpc + java + com + org + nameof + self + cls + invoke + procedureCall;
        }
    }

    /// <summary>
    /// A structure whose fields are named after names in the generated code.
    /// </summary>
    [KRPCStruct (Service = "TestService")]
    public struct NamesStruct
    {
        /// <summary>
        /// A value.
        /// </summary>
        [KRPCProperty]
        public int Other { get; set; }

        /// <summary>
        /// A value.
        /// </summary>
        [KRPCProperty]
        public int Result { get; set; }

        /// <summary>
        /// A value.
        /// </summary>
        [KRPCProperty]
        public int Java { get; set; }

        /// <summary>
        /// A value.
        /// </summary>
        [KRPCProperty]
        public int Krpc { get; set; }

        /// <summary>
        /// A value.
        /// </summary>
        [KRPCProperty]
        public int Class { get; set; }

        /// <summary>
        /// A value named after the class of static methods bound to a connection.
        /// </summary>
        [KRPCProperty]
        public int Bound { get; set; }
    }

    /// <summary>
    /// An enumeration whose values are named after names in the generated code.
    /// </summary>
    [KRPCEnum (Service = "TestService")]
    public enum NamesEnum
    {
        /// <summary>
        /// A value named after the method of a Python class that Enum reserves.
        /// </summary>
        Mro,

        /// <summary>
        /// A value named after a member of a JavaScript object.
        /// </summary>
        ToString
    }
}
