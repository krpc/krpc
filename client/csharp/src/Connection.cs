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
        MessageReader rpcReader;

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
            // A call writes a request and then waits for its response, so there is never a
            // second small write for Nagle's algorithm to hold the first one back for. Left on,
            // it can only delay a request the server is already waiting for.
            rpcClient.NoDelay = true;
            rpcStream = rpcClient.GetStream ();
            codedRpcStream = new CodedOutputStream (rpcStream, true);
            rpcReader = new MessageReader (rpcStream);
            var request = new ConnectionRequest ();
            request.Type = Type.Rpc;
            request.ClientName = name;
            codedRpcStream.WriteLength (request.CalculateSize ());
            request.WriteTo (codedRpcStream);
            codedRpcStream.Flush ();
            rpcReader.Read ();
            var response = ConnectionResponse.Parser.ParseFrom (
                rpcReader.Buffer, rpcReader.Offset, rpcReader.Size);
            if (response.Status != ConnectionResponse.Types.Status.Ok)
                throw new ConnectionException (response.Message);

            if (streamPort != 0) {
                streamClient = new TcpClient ();
                streamClient.Connect (address, streamPort);
                streamClient.NoDelay = true;
                var streamStream = streamClient.GetStream ();
                request = new ConnectionRequest ();
                request.Type = Type.Stream;
                request.ClientIdentifier = response.ClientIdentifier;
                var codedStreamStream = new CodedOutputStream (streamStream, true);
                codedStreamStream.WriteLength (request.CalculateSize ());
                request.WriteTo (codedStreamStream);
                codedStreamStream.Flush ();
                // The reader that reads the reply goes on to read the stream updates that
                // follow it. It may have taken the first of them along with the reply, so a
                // second reader on the same socket would never see it.
                var streamReader = new MessageReader (streamStream);
                streamReader.Read ();
                response = ConnectionResponse.Parser.ParseFrom (
                    streamReader.Buffer, streamReader.Offset, streamReader.Size);
                if (response.Status != ConnectionResponse.Types.Status.Ok)
                    throw new ConnectionException (response.Message);
                StreamManager = new StreamManager (this, streamReader);
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
                    // Join the update thread, so that it has ended by the time this returns
                    // rather than at some later point of its own choosing. This has to come
                    // after the sockets are closed: the thread spends its time blocked in a
                    // read that does not observe the stop event, and closing the socket
                    // underneath it is what releases it.
                    if (StreamManager != null)
                        StreamManager.Dispose ();
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
        public ProcedureResult Invoke (string service, string procedure, IList<ByteString> arguments = null)
        {
            CheckDisposed ();
            return Invoke (GetCall (service, procedure, arguments));
        }

        internal ProcedureResult Invoke (ProcedureCall call)
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
                rpcReader.Read ();
                response = Response.Parser.ParseFrom (
                    rpcReader.Buffer, rpcReader.Offset, rpcReader.Size);
            }

            if (response.Error != null)
                throw GetException(response.Error);
            if (response.Results[0].Error != null)
                throw GetException (response.Results [0].Error);
            return response.Results[0];
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
                    // A null encoding signals a null value, carried out-of-band by is_null
                    // with the value field left unset.
                    if (value == null)
                        argument.IsNull = true;
                    else
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
                System.Type exnType;
                if (!exceptionTypes.TryGetValue (key, out exnType)) {
                    // The type is unknown here if the service it belongs to was never
                    // registered. Report the error itself, named by its type on the server,
                    // rather than the failure to build an exception for it, which would say
                    // nothing about what actually went wrong.
                    return new RPCException (key + ": " + message);
                }
                return (System.Exception)Activator.CreateInstance (exnType, new [] { message });
            }
            return new RPCException (message);
        }
    }
}
