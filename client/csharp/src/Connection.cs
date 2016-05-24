using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using Google.Protobuf;
using KRPC.Client.Attributes;
using KRPC.Schema.KRPC;

namespace KRPC.Client
{
    /// <summary>
    /// A connection to the kRPC server. All interaction with kRPC is performed via an instance of this class.
    /// </summary>
    public class Connection : IConnection
    {
        TcpClient rpcClient;

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
            var rpcStream = rpcClient.GetStream ();
            rpcStream.Write (Encoder.RPCHelloMessage, 0, Encoder.RPCHelloMessage.Length);
            var clientName = Encoder.EncodeClientName (name);
            rpcStream.Write (clientName, 0, clientName.Length);
            var clientIdentifier = new byte[Encoder.ClientIdentifierLength];
            rpcStream.Read (clientIdentifier, 0, Encoder.ClientIdentifierLength);

            StreamManager = streamPort == 0 ? null : new StreamManager (this, address, streamPort, clientIdentifier);
        }

        /// <summary>
        /// Create a new stream from the given lambda expression.
        /// Returns a stream object that can be used to obtain the latest value of the stream.
        /// </summary>
        public Stream<TResult> AddStream<TResult> (LambdaExpression expression)
        {
            var request = BuildRequest (expression);
            return new Stream<TResult> (this, request);
        }

        /// <summary>
        /// See <see ref="AddStream"/>.
        /// </summary>
        public Stream<TResult> AddStream<TResult> (Expression<Func<TResult>> expression)
        {
            return AddStream<TResult> ((LambdaExpression)expression);
        }

        /// <summary>
        /// Invoke a remote procedure.
        /// Should not be called directly. This interface is used by service client stubs.
        /// </summary>
        public ByteString Invoke (string service, string procedure, IList<ByteString> arguments = null)
        {
            return Invoke (BuildRequest (service, procedure, arguments));
        }

        internal ByteString Invoke (Request request)
        {
            var response = new Response ();

            lock (rpcClient) {
                var outStream = new CodedOutputStream (rpcClient.GetStream ());
                outStream.WriteLength (request.CalculateSize ());
                request.WriteTo (outStream);
                outStream.Flush ();

                var inStream = new CodedInputStream (rpcClient.GetStream ());
                inStream.ReadMessage (response);
            }

            if (response.HasError)
                throw new RPCException (response.Error);
            return response.HasReturnValue ? response.ReturnValue : null;
        }

        internal Request BuildRequest (string service, string procedure, IList<ByteString> arguments = null)
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

        internal Request BuildRequest (LambdaExpression expression)
        {
            Expression body = expression.Body;
            if (body is MethodCallExpression)
                return BuildRequest (body as MethodCallExpression);
            else if (body is MemberExpression)
                return BuildRequest (body as MemberExpression);
            else
                throw new ArgumentException ("Invalid expression. Must consist of a method call or property accessor only.");
        }

        internal Request BuildRequest (MethodCallExpression expression)
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

        internal Request BuildRequest (MemberExpression expression)
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
    }
}
