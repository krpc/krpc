using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Google.Protobuf;
using KRPC.Client.Attributes;
using KRPC.Schema.KRPC;

namespace KRPC.Client
{
    /// <summary>
    /// A connection to the kRPC server. All interaction with kRPC is performed via an instance of this class.
    /// </summary>
    [SuppressMessage ("Gendarme.Rules.Correctness", "DisposableFieldsShouldBeDisposedRule")]
    [SuppressMessage ("Gendarme.Rules.Smells", "AvoidLargeClassesRule")]
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
        [SuppressMessage ("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule")]
        public Connection (string name = "", IPAddress address = null, int rpcPort = 50000, int streamPort = 50001)
        {
            if (address == null)
                address = IPAddress.Loopback;

            rpcClient = new TcpClient ();
            rpcClient.Connect (address, rpcPort);
            rpcStream = rpcClient.GetStream ();
            rpcStream.Write (Encoder.RPCHelloMessage, 0, Encoder.RPCHelloMessage.Length);
            var clientName = Encoder.EncodeClientName (name);
            rpcStream.Write (clientName, 0, clientName.Length);
            var clientIdentifier = new byte[Encoder.ClientIdentifierLength];
            rpcStream.Read (clientIdentifier, 0, Encoder.ClientIdentifierLength);
            codedRpcStream = new CodedOutputStream (rpcStream, true);

            if (streamPort != 0) {
                streamClient = new TcpClient ();
                streamClient.Connect (address, streamPort);
                var streamStream = streamClient.GetStream ();
                streamStream.Write (Encoder.StreamHelloMessage, 0, Encoder.StreamHelloMessage.Length);
                streamStream.Write (clientIdentifier, 0, clientIdentifier.Length);
                var recvOkMessage = new byte [Encoder.OkMessage.Length];
                streamStream.Read (recvOkMessage, 0, Encoder.OkMessage.Length);
                if (!recvOkMessage.SequenceEqual (Encoder.OkMessage))
                    throw new InvalidOperationException ("Did not receive OK message from server");
                StreamManager = new StreamManager (this, streamClient);
            }
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
        [SuppressMessage ("Gendarme.Rules.Design.Generic", "AvoidMethodWithUnusedGenericTypeRule")]
        public Stream<TResult> AddStream<TResult> (LambdaExpression expression)
        {
            CheckDisposed ();
            var request = BuildRequest (expression);
            return new Stream<TResult> (this, request);
        }

        /// <summary>
        /// See <see ref="AddStream"/>.
        /// </summary>
        [SuppressMessage ("Gendarme.Rules.Design.Generic", "DoNotExposeNestedGenericSignaturesRule")]
        [SuppressMessage ("Gendarme.Rules.Maintainability", "AvoidUnnecessarySpecializationRule")]
        public Stream<TResult> AddStream<TResult> (Expression<Func<TResult>> expression)
        {
            CheckDisposed ();
            return AddStream<TResult> ((LambdaExpression)expression);
        }

        /// <summary>
        /// Invoke a remote procedure.
        /// Should not be called directly. This interface is used by service client stubs.
        /// </summary>
        public ByteString Invoke (string service, string procedure, IList<ByteString> arguments = null)
        {
            CheckDisposed ();
            return Invoke (BuildRequest (service, procedure, arguments));
        }

        internal ByteString Invoke (Request request)
        {
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

            if (response.HasError)
                throw new RPCException (response.Error);
            return response.HasReturnValue ? response.ReturnValue : null;
        }

        internal static Request BuildRequest (string service, string procedure, IList<ByteString> arguments = null)
        {
            var request = new Request ();
            request.Service = service;
            request.Procedure = procedure;
            if (arguments != null) {
                uint position = 0;
                foreach (var value in arguments) {
                    var argument = new Argument ();
                    argument.Position = position;
                    argument.Value = value;
                    request.Arguments.Add (argument);
                    position++;
                }
            }
            return request;
        }

        internal static Request BuildRequest (LambdaExpression expression)
        {
            Expression body = expression.Body;

            var methodCallExpression = body as MethodCallExpression;
            if (methodCallExpression != null)
                return BuildRequest (methodCallExpression);

            var memberExpression = body as MemberExpression;
            if (memberExpression != null)
                return BuildRequest (memberExpression);

            throw new ArgumentException ("Invalid expression. Must consist of a method call or property accessor only.");
        }

        internal static Request BuildRequest (MethodCallExpression expression)
        {
            var method = expression.Method;

            // Get the RPCAttribute with service and procedure names
            object[] attributes = method.GetCustomAttributes (typeof(RPCAttribute), false);
            if (attributes.Length != 1)
                throw new ArgumentException ("Invalid expression. Method called must be backed by a RPC.");
            var attribute = (RPCAttribute)attributes [0];

            // Construct the encoded arguments
            var arguments = new List<ByteString> ();

            // Include class instance argument for class methods
            if (ExpressionUtils.IsAClassMethod (expression)) {
                var instance = expression.Object;
                var instanceExpr = Expression.Lambda<Func<object>> (Expression.Convert (instance, typeof(object)));
                var instanceValue = instanceExpr.Compile () ();
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

            // Build the request
            return BuildRequest (attribute.Service, attribute.Procedure, arguments);
        }

        internal static Request BuildRequest (MemberExpression expression)
        {
            var member = expression.Member;

            // Get the RPCAttribute with service and procedure names
            object[] attributes = member.GetCustomAttributes (typeof(RPCAttribute), false);
            if (attributes.Length != 1)
                throw new ArgumentException ("Invalid expression. Property accessed must be backed by a RPC.");
            var attribute = (RPCAttribute)attributes [0];

            // Construct the encoded arguments
            var arguments = new List<ByteString> ();

            // If it's a class property, pass the class instance as an argument
            if (ExpressionUtils.IsAClassProperty (expression)) {
                var instance = expression.Expression;
                var argumentExpr = Expression.Lambda<Func<object>> (Expression.Convert (instance, typeof(object)));
                var value = argumentExpr.Compile () ();
                var type = member.DeclaringType;
                var encodedValue = Encoder.Encode (value, type);
                arguments.Add (encodedValue);
            }

            // Build the request
            return BuildRequest (attribute.Service, attribute.Procedure, arguments);
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
    }
}
