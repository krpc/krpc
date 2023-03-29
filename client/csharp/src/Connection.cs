using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Google.Protobuf;
using KRPC.Client.Attributes;
using KRPC.Schema.KRPC;
using Type = KRPC.Schema.KRPC.ConnectionRequest.Types.Type;

namespace KRPC.Client
{
    /// <summary>
    /// A connection to the kRPC server. All interaction with kRPC is performed via an instance of this class.
    /// </summary>
    public class Connection : IConnection, IDisposable
    {
        object invokeLock = new object ();
        TcpClient rpcClient;
        TcpClient streamClient;
        NetworkStream rpcStream;
        CodedOutputStream codedRpcStream;
        byte[] responseBuffer = new byte [BUFFER_INITIAL_SIZE];

        internal StreamManager StreamManager {
            get;
            private set;
        }

        /// <summary>
        /// Connect to a kRPC server on the specified IP address and port numbers. If
        /// streamPort is 0, does not connect to the stream server.
        /// Passes an optional name to the server to identify the client (up to 32 bytes of UTF-8 encoded text).
        /// </summary>
        public Connection (string name = "", IPAddress address = null, int rpcPort = 50000, int streamPort = 50001)
        {
            if (address == null)
                address = IPAddress.Loopback;

            rpcClient = new TcpClient ();
            rpcClient.Connect (address, rpcPort);
            rpcStream = rpcClient.GetStream ();
            codedRpcStream = new CodedOutputStream (rpcStream, true);
            var request = new ConnectionRequest ();
            request.Type = Type.Rpc;
            request.ClientName = name;
            codedRpcStream.WriteLength (request.CalculateSize ());
            request.WriteTo (codedRpcStream);
            codedRpcStream.Flush ();
            int size = ReadMessageData (rpcStream, ref responseBuffer);
            var response = ConnectionResponse.Parser.ParseFrom (new CodedInputStream (responseBuffer, 0, size));
            if (response.Status != ConnectionResponse.Types.Status.Ok)
                throw new ConnectionException (response.Message);

            if (streamPort != 0) {
                streamClient = new TcpClient ();
                streamClient.Connect (address, streamPort);
                var streamStream = streamClient.GetStream ();
                request = new ConnectionRequest ();
                request.Type = Type.Stream;
                request.ClientIdentifier = response.ClientIdentifier;
                var codedStreamStream = new CodedOutputStream (streamStream, true);
                codedStreamStream.WriteLength (request.CalculateSize ());
                request.WriteTo (codedStreamStream);
                codedStreamStream.Flush ();
                size = ReadMessageData (streamStream, ref responseBuffer);
                response = ConnectionResponse.Parser.ParseFrom (new CodedInputStream (responseBuffer, 0, size));
                if (response.Status != ConnectionResponse.Types.Status.Ok)
                    throw new ConnectionException (response.Message);
                StreamManager = new StreamManager (this, streamClient);
            }

            Services.KRPC.Service.AddExceptionTypes (this);
        }

        /// <summary>
        /// Finalize the connection.
        /// </summary>
        ~Connection ()
        {
            Dispose (false);
        }

        bool disposed;

        /// <summary>
        /// Dispose the connection.
        /// </summary>
        public void Dispose ()
        {
            Dispose (true);
            GC.SuppressFinalize (this);
        }

        /// <summary>
        /// Dispose the connection.
        /// </summary>
        protected virtual void Dispose (bool disposing)
        {
            if (!disposed) {
                if (disposing) {
                    rpcClient.Close ();
                    if (streamClient != null)
                        streamClient.Close ();
                }
                disposed = true;
            }
        }

        void CheckDisposed ()
        {
            if (disposed)
                throw new ObjectDisposedException (GetType ().Name);
        }

        /// <summary>
        /// Create a new stream from the given lambda expression.
        /// Returns a stream object that can be used to obtain the latest value of the stream.
        /// </summary>
        public Stream<TResult> AddStream<TResult> (LambdaExpression expression)
        {
            CheckDisposed ();
            return new Stream<TResult> (this, GetCall (expression));
        }

        /// <summary>
        /// See <see ref="AddStream"/>.
        /// </summary>
        public Stream<TResult> AddStream<TResult> (Expression<Func<TResult>> expression)
        {
            CheckDisposed ();
            return new Stream<TResult> (this, GetCall (expression));
        }

        /// <summary>
        /// Invoke a remote procedure.
        /// Should not be called directly. This interface is used by service client stubs.
        /// </summary>
        public ByteString Invoke (string service, string procedure, IList<ByteString> arguments = null)
        {
            CheckDisposed ();
            return Invoke (GetCall (service, procedure, arguments));
        }

        internal ByteString Invoke (ProcedureCall call)
        {
            var request = new Request ();
            request.Calls.Add (call);
            Response response;

            lock (invokeLock) {
                // Send request to server
                codedRpcStream.WriteLength (request.CalculateSize ());
                request.WriteTo (codedRpcStream);
                codedRpcStream.Flush ();
                // Receive response
                int size = ReadMessageData (rpcStream, ref responseBuffer);
                response = Response.Parser.ParseFrom (new CodedInputStream (responseBuffer, 0, size));
            }

            if (response.Error != null)
                throw GetException(response.Error);
            if (response.Results[0].Error != null)
                throw GetException (response.Results [0].Error);
            return response.Results[0].Value;
        }

        internal static ProcedureCall GetCall (string service, string procedure, IList<ByteString> arguments = null)
        {
            var call = new ProcedureCall ();
            call.Service = service;
            call.Procedure = procedure;
            if (arguments != null) {
                uint position = 0;
                foreach (var value in arguments) {
                    var argument = new Argument ();
                    argument.Position = position;
                    argument.Value = value;
                    call.Arguments.Add (argument);
                    position++;
                }
            }
            return call;
        }

        /// <summary>
        /// Return the procedure call message for a remote procedure call.
        /// </summary>
        public static ProcedureCall GetCall<TResult> (Expression<Func<TResult>> expression)
        {
            return GetCall ((LambdaExpression) expression);
        }

        /// <summary>
        /// Return the procedure call message for a remote procedure call.
        /// </summary>
        public static ProcedureCall GetCall (LambdaExpression expression)
        {
            if (ReferenceEquals (expression, null))
                throw new ArgumentNullException (nameof (expression));

            Expression body = expression.Body;

            var methodCallExpression = body as MethodCallExpression;
            if (methodCallExpression != null)
                return GetCall (methodCallExpression);

            var memberExpression = body as MemberExpression;
            if (memberExpression != null)
                return GetCall (memberExpression);

            throw new ArgumentException ("Invalid expression. Must consist of a method call or property accessor only.");
        }

        internal static ProcedureCall GetCall (MethodCallExpression expression)
        {
            var method = expression.Method;

            // Get the RPCAttribute with service and procedure names
            object[] attributes = method.GetCustomAttributes (typeof(RPCAttribute), false);
            if (attributes.Length != 1)
                throw new ArgumentException ("Invalid expression. Method called must be backed by a RPC.");
            var attribute = (RPCAttribute)attributes [0];

            // Construct the encoded arguments
            var arguments = new List<ByteString> ();

            // Evaluate the instance on which the method is called
            // Note: ensures, for example, that the service constructor extension method is called
            //       such that custom exception types are registered
            // Note: in the case of class methods, is used to get the id of the object
            //       with which to make the call
            var instanceValue = GetInstanceValue (expression.Object);

            // Include class instance argument for class methods
            if (ExpressionUtils.IsAClassMethod (expression)) {
                var instanceType = method.DeclaringType;
                arguments.Add (Encoder.Encode (instanceValue, instanceType));
            }

            // Include arguments from the expression
            int position = 0;
            foreach (var argument in expression.Arguments) {
                // Skip connection parameter to static class methods
                if (position == 0 && ExpressionUtils.IsAClassStaticMethod (expression)) {
                    position++;
                    continue;
                }
                var argumentExpr = Expression.Lambda<Func<object>> (Expression.Convert (argument, typeof(object)));
                var value = argumentExpr.Compile () ();
                var type = method.GetParameters () [position].ParameterType;
                var encodedValue = Encoder.Encode (value, type);
                arguments.Add (encodedValue);
                position++;
            }

            return GetCall (attribute.Service, attribute.Procedure, arguments);
        }

        internal static ProcedureCall GetCall (MemberExpression expression)
        {
            var member = expression.Member;

            // Get the RPCAttribute with service and procedure names
            object[] attributes = member.GetCustomAttributes (typeof(RPCAttribute), false);
            if (attributes.Length != 1)
                throw new ArgumentException ("Invalid expression. Property accessed must be backed by a RPC.");
            var attribute = (RPCAttribute)attributes [0];

            // Construct the encoded arguments
            var arguments = new List<ByteString> ();

            // Evaluate the instance on which the method is called
            // Note: ensures, for example, that the service constructor extension method is called
            //       such that custom exception types are registered
            // Note: in the case of class methods, is used to get the id of the object
            //       with which to make the call
            var instanceValue = GetInstanceValue (expression.Expression);

            // If it's a class property, pass the class instance as an argument
            if (ExpressionUtils.IsAClassProperty (expression)) {
                var instanceType = member.DeclaringType;
                arguments.Add (Encoder.Encode (instanceValue, instanceType));
            }

            return GetCall (attribute.Service, attribute.Procedure, arguments);
        }

        static object GetInstanceValue (Expression instance) {
            if (instance == null)
                return null;
            var instanceExpr = Expression.Lambda<Func<object>> (
                Expression.Convert (instance, typeof(object)));
            return instanceExpr.Compile () ();
        }

        // Initial buffer size of 1 MB
        internal const int BUFFER_INITIAL_SIZE = 1 * 1024 * 1024;
        // Initial increases in increments of 512 KB
        internal const int BUFFER_INCREASE_SIZE = 512 * 1024;

        /// <summary>
        /// Read the data from a message from the given stream into the given buffer.
        /// May reallocate the buffer if it is too small to receive the message.
        /// Returns the lenght of the message in bytes.
        /// If a stopEvent is specified, this method will return 0 if the event is triggered.
        /// </summary>
        internal static int ReadMessageData (System.IO.Stream stream, ref byte[] buffer, EventWaitHandle stopEvent = null)
        {
            bool stop = stopEvent != null && stopEvent.WaitOne (0);
            int bufferSize = 0;
            int messageSize = 0;

            // Read the offset and size of the message data
            while (!stop) {
                bufferSize += stream.Read (buffer, bufferSize, 1);
                stop |= stopEvent != null && stopEvent.WaitOne (0);
                try {
                    var codedStream = new CodedInputStream (buffer, 0, bufferSize);
                    messageSize = (int)codedStream.ReadUInt32 ();
                    stop |= stopEvent != null && stopEvent.WaitOne (0);
                    break;
                } catch (InvalidProtocolBufferException) {
                }
            }
            if (stop)
                return 0;

            // Read the response data
            bufferSize = 0;
            while (!stop && bufferSize < messageSize) {
                // Increase the size of the buffer if the remaining space is low
                if (buffer.Length - bufferSize < BUFFER_INCREASE_SIZE) {
                    var newBuffer = new byte [buffer.Length + BUFFER_INCREASE_SIZE];
                    Array.Copy (buffer, newBuffer, bufferSize);
                    buffer = newBuffer;
                }
                bufferSize += stream.Read (buffer, bufferSize, messageSize - bufferSize);
                stop |= stopEvent != null && stopEvent.WaitOne (0);
            }
            if (stop)
                return 0;

            return messageSize;
        }

        readonly IDictionary<string, System.Type> exceptionTypes = new Dictionary<string, System.Type>();

        /// <summary>
        /// Add an exception type to the client.
        /// Should only be called by generated client stubs.
        /// </summary>
        public void AddExceptionType (string service, string name, System.Type exnType)
        {
            CheckDisposed ();
            exceptionTypes [service + "." + name] = exnType;
        }

        internal System.Exception GetException (Error error)
        {
            var message = error.Description;
            if (error.StackTrace.Length > 0) {
                var newline = Environment.NewLine;
                message += newline + "Server stack trace: " + newline + error.StackTrace;
            }
            if (error.Service.Length > 0 && error.Name.Length > 0) {
                var key = error.Service + "." + error.Name;
                if (key == "KRPC.InvalidOperationException")
                    return new InvalidOperationException (message);
                if (key == "KRPC.ArgumentException")
                    return new ArgumentException (string.Empty, message);
                if (key == "KRPC.ArgumentNullException")
                    return new ArgumentNullException (string.Empty, message);
                if (key == "KRPC.ArgumentOutOfRangeException")
                    return new ArgumentOutOfRangeException (string.Empty, message);
                var exnType = exceptionTypes [key];
                return (System.Exception)Activator.CreateInstance (exnType, new [] { message });
            }
            return new RPCException (message);
        }
    }
}
