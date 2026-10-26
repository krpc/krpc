using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using Google.Protobuf;
using KRPC.Client.Attributes;
using KRPC.Schema.KRPC;

namespace KRPC.Client
{
    /// <summary>
    /// A connection to the kRPC server. All interaction with kRPC is performed via an instance of this class.
    /// </summary>
    public class Connection : IConnection, IDisposable
    {
        const uint PROTOCOL_VERSION = 2;

        // The wait for the server to accept a connection when none is given
        static readonly TimeSpan DefaultHandshakeTimeout = TimeSpan.FromSeconds (5);

        readonly Session session;

        internal Session Session {
            get { return session; }
        }

        // Building the KRPC.Type object that names a type costs a round trip. Every
        // function compiled on this connection shares the objects built for it
        readonly FunctionCompiler.TypeCache expressionRemoteTypes = new FunctionCompiler.TypeCache ();

        // The type of the values each server side function evaluates to, by the function's
        // object id, and null for one that evaluates to no value. Introspecting a type costs a
        // round trip per property, and a function's return type does not change
        internal readonly ConcurrentDictionary<ulong, global::KRPC.Schema.KRPC.Type> functionReturnTypes =
            new ConcurrentDictionary<ulong, global::KRPC.Schema.KRPC.Type> ();

        // The types of each server side function's parameters, likewise
        readonly ConcurrentDictionary<ulong, IList<global::KRPC.Schema.KRPC.Type>> functionParameterTypes =
            new ConcurrentDictionary<ulong, IList<global::KRPC.Schema.KRPC.Type>> ();

        internal StreamManager StreamManager {
            get;
            private set;
        }

        /// <summary>
        /// Connect to a kRPC server over TCP/IP.
        /// </summary>
        /// <param name="name">A name for the client, up to 32 bytes of UTF-8 encoded text.</param>
        /// <param name="address">The address of the server, the loopback address if null.</param>
        /// <param name="port">The port of the server.</param>
        /// <param name="timeout">How long to wait for a connection, or indefinitely if zero.</param>
        /// <param name="handshakeTimeout">How long to wait for the server to accept the
        /// connection, 5 seconds if null, or indefinitely if
        /// <see cref="Timeout.InfiniteTimeSpan"/>, such as for a server that asks the player to
        /// approve each connection.</param>
        /// <param name="stallTimeout">How long a message from the server may go without a new byte
        /// once it has started to arrive, 30 seconds if null, or indefinitely if
        /// <see cref="Timeout.InfiniteTimeSpan"/>.</param>
        public Connection (string name = "", IPAddress address = null, int port = 50000,
                           TimeSpan timeout = default (TimeSpan), TimeSpan? handshakeTimeout = null,
                           TimeSpan? stallTimeout = null)
            : this (name, Connect (address ?? IPAddress.Loopback, port, timeout, handshakeTimeout, stallTimeout),
                    handshakeTimeout, stallTimeout)
        {
        }

        /// <summary>
        /// Connect to a kRPC server on the same machine, over a unix domain socket named by the
        /// given path rather than over TCP. An empty path stands for the one the server uses
        /// unless it was configured with another. The connection behaves identically once
        /// established. Unix domain sockets are available on Linux, macOS, and Windows 10 1803
        /// and later. See the constructor for the handshake and stall timeouts.
        /// </summary>
        public static Connection ConnectLocal (string name = "", string path = "", TimeSpan? handshakeTimeout = null,
                                               TimeSpan? stallTimeout = null)
        {
            CheckTimeouts (handshakeTimeout, stallTimeout);
            return new Connection (name, ConnectToPath (PathOrDefault (path)), handshakeTimeout, stallTimeout);
        }

        /// <summary>
        /// The path to connect to: the one asked for, or the server's default where none was.
        /// </summary>
        static string PathOrDefault (string path)
        {
            return string.IsNullOrEmpty (path) ? DefaultPath () : path;
        }

        /// <summary>
        /// Throw ArgumentOutOfRangeException for a handshake or stall timeout that is neither
        /// positive nor infinite.
        /// </summary>
        static void CheckTimeouts (TimeSpan? handshakeTimeout, TimeSpan? stallTimeout)
        {
            CheckTimeout (handshakeTimeout, nameof (handshakeTimeout));
            CheckTimeout (stallTimeout, nameof (stallTimeout));
        }

        static void CheckTimeout (TimeSpan? timeout, string name)
        {
            if (timeout.HasValue && timeout.Value <= TimeSpan.Zero && timeout.Value != Timeout.InfiniteTimeSpan)
                throw new ArgumentOutOfRangeException (name, timeout.Value, "The timeout must be positive or infinite");
        }

        static Socket Connect (IPAddress address, int port, TimeSpan timeout,
                               TimeSpan? handshakeTimeout, TimeSpan? stallTimeout)
        {
            CheckTimeouts (handshakeTimeout, stallTimeout);
            try {
                return Connect (address, port, timeout);
            } catch (SocketException exn) {
                throw new ConnectionException ("Failed to connect to the server: " + exn.Message, exn);
            }
        }

        static Socket Connect (IPAddress address, int port, TimeSpan timeout)
        {
            var socket = new Socket (address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            if (timeout == TimeSpan.Zero) {
                socket.Connect (address, port);
            } else {
                // A network that drops a connection attempt instead of refusing it leaves the
                // client waiting, so bound the wait where one was asked for.
                var pending = socket.BeginConnect (address, port, null, null);
                if (!pending.AsyncWaitHandle.WaitOne (timeout)) {
                    socket.Close ();
                    throw new SocketException ((int)SocketError.TimedOut);
                }
                socket.EndConnect (pending);
            }
            // A message is sent as soon as it is written, so waiting for more data to coalesce
            // with can only add latency
            socket.NoDelay = true;
            return socket;
        }

        static Socket ConnectToPath (string path)
        {
            var socket = new Socket (AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
            try {
                socket.Connect (new UnixEndPoint (path));
            } catch (SocketException exn) {
                socket.Close ();
                throw new ConnectionException ("Failed to connect to the server: " + exn.Message, exn);
            }
            return socket;
        }

        /// <summary>
        /// The default socket path, matching the one the server uses unless it was configured
        /// with another. The fallback names a fixed directory rather than asking for the
        /// temporary one, which TMPDIR moves for the client and not the server.
        /// </summary>
        static string DefaultPath ()
        {
            var windows = Environment.OSVersion.Platform == PlatformID.Win32NT;
            var directory = Environment.GetEnvironmentVariable (
                windows ? "LOCALAPPDATA" : "XDG_RUNTIME_DIR");
            if (!string.IsNullOrEmpty (directory))
                return System.IO.Path.Combine (directory, "krpc.sock");
            var temporary = windows ? System.IO.Path.GetTempPath () : "/tmp";
            return System.IO.Path.Combine (temporary, "krpc-" + Environment.UserName + ".sock");
        }

        /// <summary>
        /// Perform the connection handshake over an already opened socket. The handshake is the
        /// same whatever carries it.
        /// </summary>
        Connection (string name, Socket socket, TimeSpan? handshakeTimeout, TimeSpan? stallTimeout)
        {
            var stream = new NetworkStream (socket);
            var reader = new MessageReader (stream, socket);
            var stall = stallTimeout ?? Session.DefaultStallTimeout;
            try {
                Handshake (name, stream, reader, handshakeTimeout ?? DefaultHandshakeTimeout);
            } catch (System.Exception) {
                socket.Close ();
                throw;
            }
            session = new Session (socket, stream, reader, RemoveStreamRequest, stall);
            StreamManager = new StreamManager (this, session.Dispatch);
            session.Start (StreamManager);
            Services.KRPC.Service.AddExceptionTypes (this);
        }

        /// <summary>
        /// Send the connection request, and check the server's response to it.
        /// </summary>
        static void Handshake (string name, NetworkStream stream, MessageReader reader, TimeSpan timeout)
        {
            var output = new CodedOutputStream (stream, true);
            var request = new Message {
                ConnectionRequest = new ConnectionRequest { ClientName = name ?? string.Empty, ProtocolVersion = PROTOCOL_VERSION }
            };
            output.WriteLength (request.CalculateSize ());
            request.WriteTo (output);
            output.Flush ();
            Message message;
            try {
                // The stall timeout applies from the next message
                reader.Read (timeout, Timeout.InfiniteTimeSpan);
                message = Message.Parser.ParseFrom (reader.Buffer, reader.Offset, reader.Size);
            } catch (TimeoutException exn) {
                throw new ConnectionException (
                    "The server did not answer the connection request within " +
                    timeout.TotalSeconds.ToString (CultureInfo.InvariantCulture) + " seconds", exn);
            } catch (InvalidProtocolBufferException exn) {
                throw new ConnectionException ("Failed to parse the server's response to the connection request", exn);
            } catch (System.IO.InvalidDataException exn) {
                throw new ConnectionException (
                    "Failed to read the server's response to the connection request: " + exn.Message, exn);
            } catch (System.Exception exn) when (exn is System.IO.IOException || exn is SocketException) {
                throw new ConnectionException ("Connection failed: " + exn.Message, exn);
            }
            if (message.ContentCase == Message.ContentOneofCase.Disconnect) {
                var text = "The server ended the connection (" + message.Disconnect.Reason + ")";
                if (message.Disconnect.Message.Length > 0)
                    text += ": " + message.Disconnect.Message;
                throw new ConnectionException (text) { Reason = message.Disconnect.Reason };
            }
            if (message.ContentCase != Message.ContentOneofCase.ConnectionResponse)
                throw new ConnectionException (
                    "Expected a response to the connection request, got " +
                    (message.ContentCase == Message.ContentOneofCase.None
                     ? "a message of an unknown kind" : message.ContentCase.ToString ()));
            var response = message.ConnectionResponse;
            if (response.Status != ConnectionResponse.Types.Status.Ok) {
                var text = "The server refused the connection (" + response.Status + ")";
                if (response.Message.Length > 0)
                    text += ": " + response.Message;
                throw new ConnectionException (text) { Status = response.Status };
            }
            if (response.ProtocolVersion != PROTOCOL_VERSION)
                throw new ConnectionException (
                    "The server does not support protocol version " +
                    PROTOCOL_VERSION.ToString (CultureInfo.InvariantCulture) +
                    ". Use a client from the same kRPC release as the server.");
        }

        /// <summary>
        /// A request to remove a stream on the server.
        /// </summary>
        Request RemoveStreamRequest (ulong id)
        {
            var request = new Request ();
            request.Calls.Add (GetCall ("KRPC", "RemoveStream", new [] { Encoder.Encode (id, TypeSpec.For (typeof (ulong))) }));
            return request;
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
                    Session.DeferPendingInterrupt ();
                    try {
                        // The dispatcher thread stops trying to take a condition this thread can
                        // hold
                        StreamManager.RequestClose ();
                        session.Close ();
                        // Streams end before the join, so a thread waiting on one is woken
                        StreamManager.NotifyClosed (session.FailureCause);
                        // Callbacks run on the dispatcher thread, so a connection disposed from
                        // one does not join it. A callback can also wait for a condition this
                        // thread holds, so the dispatcher thread then ends once this thread
                        // releases it.
                        session.Join (!StreamManager.HoldsACondition ());
                    } finally {
                        Session.RaiseDeferredInterrupt ();
                    }
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
        /// Create a stream from a lambda expression.
        /// </summary>
        /// <remarks>
        /// A single remote procedure call with fixed arguments is streamed as that call. Any
        /// other lambda is translated as <see cref="Compile{TResult}(Expression{Func{TResult}})"/>
        /// translates it. Throws an exception if the type parameter cannot hold the lambda's
        /// value, or if the lambda cannot be compiled.
        /// </remarks>
        public Stream<TResult> AddStream<TResult> (LambdaExpression expression)
        {
            CheckDisposed ();
            if (ReferenceEquals (expression, null))
                throw new ArgumentNullException (nameof (expression));
            if (expression.Parameters.Count > 0)
                throw new FunctionCompilationException (
                    "A function that is streamed, or checked for an event, cannot take parameters");
            if (!typeof (TResult).IsAssignableFrom (expression.ReturnType))
                throw new ArgumentException (
                    "The lambda evaluates to a " + expression.ReturnType +
                    ", which is not a " + typeof (TResult));
            Expression rpc;
            // A call is streamed as that call when its arguments are fixed. An argument
            // that makes a remote call is evaluated on each update, in a function
            if (IsASingleCall (expression, out rpc) && !FunctionCompiler.HasRemoteArguments (rpc))
                return new Stream<TResult> (
                    this, GetCall (expression, out rpc),
                    ProcedureSpecs.ReturnSpec (GetRPCAttribute (rpc), rpc.Type));
            // A function cannot call the KRPC service, so a conversion of its value is made on
            // the client
            Func<object, TResult> convert;
            if (IsAConvertedKRPCCall<TResult> (expression, out rpc, out convert))
                return new Stream<TResult> (
                    this, GetCall (rpc), ProcedureSpecs.ReturnSpec (GetRPCAttribute (rpc), rpc.Type),
                    convert);
            var spec = CompiledResultSpec<TResult> (expression);
            var compiled = FunctionCompiler.Compile (this, expression, expressionRemoteTypes, true);
            return AddCheckedStream<TResult> (compiled, DescribedSpec (spec, compiled));
        }

        /// <summary>
        /// See <see cref="AddStream{TResult}(LambdaExpression)"/>.
        /// </summary>
        public Stream<TResult> AddStream<TResult> (Expression<Func<TResult>> expression)
        {
            return AddStream<TResult> ((LambdaExpression)expression);
        }

        /// <summary>
        /// Compile a lambda expression into a server side function.
        /// </summary>
        /// <remarks>
        /// The function makes the lambda's remote procedure calls on each evaluation, and the
        /// lambda's other values are captured when it is compiled. Compiling makes a remote
        /// procedure call per node, so compile a function once and reuse it. The server
        /// compiles the function too, so its first run is as fast as later ones. Throws an
        /// exception if the lambda cannot be compiled.
        /// </remarks>
        public Services.KRPC.Expression Compile<TResult> (Expression<Func<TResult>> expression)
        {
            CheckDisposed ();
            return CompiledOnServer (FunctionCompiler.Compile (this, expression, expressionRemoteTypes));
        }

        /// <summary>
        /// Compile a lambda with no value.
        /// </summary>
        /// <remarks>
        /// See <see cref="Compile{TResult}(Expression{Func{TResult}})"/>.
        /// </remarks>
        public Services.KRPC.Expression Compile (Expression<Action> expression)
        {
            CheckDisposed ();
            return CompiledOnServer (FunctionCompiler.Compile (this, expression, expressionRemoteTypes));
        }

        /// <summary>
        /// Compile a lambda with a parameter.
        /// </summary>
        /// <remarks>
        /// <see cref="Run{TResult}(Services.KRPC.Expression, object[])"/> passes its argument to
        /// the parameter. See <see cref="Compile{TResult}(Expression{Func{TResult}})"/>.
        /// </remarks>
        public Services.KRPC.Expression Compile<T1, TResult> (Expression<Func<T1, TResult>> expression)
        {
            return CompileWithParameters (expression);
        }

        /// <summary>
        /// Compile a lambda with two parameters.
        /// </summary>
        /// <remarks>
        /// See <see cref="Compile{T1, TResult}(Expression{Func{T1, TResult}})"/>.
        /// </remarks>
        public Services.KRPC.Expression Compile<T1, T2, TResult> (Expression<Func<T1, T2, TResult>> expression)
        {
            return CompileWithParameters (expression);
        }

        /// <summary>
        /// Compile a lambda with three parameters.
        /// </summary>
        /// <remarks>
        /// See <see cref="Compile{T1, TResult}(Expression{Func{T1, TResult}})"/>.
        /// </remarks>
        public Services.KRPC.Expression Compile<T1, T2, T3, TResult> (
            Expression<Func<T1, T2, T3, TResult>> expression)
        {
            return CompileWithParameters (expression);
        }

        /// <summary>
        /// Compile a lambda with four parameters.
        /// </summary>
        /// <remarks>
        /// See <see cref="Compile{T1, TResult}(Expression{Func{T1, TResult}})"/>.
        /// </remarks>
        public Services.KRPC.Expression Compile<T1, T2, T3, T4, TResult> (
            Expression<Func<T1, T2, T3, T4, TResult>> expression)
        {
            return CompileWithParameters (expression);
        }

        /// <summary>
        /// Compile a lambda with a parameter and no value.
        /// </summary>
        /// <remarks>
        /// See <see cref="Compile{T1, TResult}(Expression{Func{T1, TResult}})"/>.
        /// </remarks>
        public Services.KRPC.Expression Compile<T1> (Expression<Action<T1>> expression)
        {
            return CompileWithParameters (expression);
        }

        /// <summary>
        /// Compile a lambda with two parameters and no value.
        /// </summary>
        /// <remarks>
        /// See <see cref="Compile{T1, TResult}(Expression{Func{T1, TResult}})"/>.
        /// </remarks>
        public Services.KRPC.Expression Compile<T1, T2> (Expression<Action<T1, T2>> expression)
        {
            return CompileWithParameters (expression);
        }

        /// <summary>
        /// Compile a lambda with three parameters and no value.
        /// </summary>
        /// <remarks>
        /// See <see cref="Compile{T1, TResult}(Expression{Func{T1, TResult}})"/>.
        /// </remarks>
        public Services.KRPC.Expression Compile<T1, T2, T3> (Expression<Action<T1, T2, T3>> expression)
        {
            return CompileWithParameters (expression);
        }

        /// <summary>
        /// Compile a lambda with four parameters and no value.
        /// </summary>
        /// <remarks>
        /// See <see cref="Compile{T1, TResult}(Expression{Func{T1, TResult}})"/>.
        /// </remarks>
        public Services.KRPC.Expression Compile<T1, T2, T3, T4> (Expression<Action<T1, T2, T3, T4>> expression)
        {
            return CompileWithParameters (expression);
        }

        Services.KRPC.Expression CompileWithParameters (LambdaExpression expression)
        {
            CheckDisposed ();
            if (ReferenceEquals (expression, null))
                throw new ArgumentNullException (nameof (expression));
            return CompiledOnServer (FunctionCompiler.Compile (this, expression, expressionRemoteTypes));
        }

        Services.KRPC.Expression CompiledOnServer (Services.KRPC.Expression function)
        {
            Services.KRPC.ExtensionMethods.KRPC (this).CompileFunction (function);
            return function;
        }

        /// <summary>
        /// Create an event from a lambda with a boolean value.
        /// </summary>
        /// <remarks>
        /// The lambda is translated as <see cref="Compile{TResult}(Expression{Func{TResult}})"/>
        /// translates it.
        /// </remarks>
        public Event AddEvent (Expression<Func<bool>> expression)
        {
            CheckDisposed ();
            var compiled = FunctionCompiler.Compile (this, expression, expressionRemoteTypes, true);
            return Services.KRPC.ExtensionMethods.KRPC (this).AddEvent (compiled);
        }

        /// <summary>
        /// Create an event from a server side function with a boolean value.
        /// </summary>
        public Event AddEvent (Services.KRPC.Expression function)
        {
            CheckDisposed ();
            if (ReferenceEquals (function, null))
                throw new ArgumentNullException (nameof (function));
            return Services.KRPC.ExtensionMethods.KRPC (this).AddEvent (function);
        }

        /// <summary>
        /// Run a server side function, and return its value.
        /// </summary>
        /// <remarks>
        /// The function runs within a single physics tick, with the arguments passed to its
        /// parameters. Throws an exception if the function has no value, if the type parameter
        /// cannot hold its value, or if a parameter cannot take its argument.
        /// </remarks>
        public TResult Run<TResult> (Services.KRPC.Expression function, params object[] args)
        {
            CheckDisposed ();
            if (ReferenceEquals (function, null))
                throw new ArgumentNullException (nameof (function));
            var spec = FunctionSpec<TResult> (function);
            return RunCheckedFunction<TResult> (function, spec, EncodeArguments (function, args));
        }

        TResult RunCheckedFunction<TResult> (
            Services.KRPC.Expression function, TypeSpec spec, IList<byte[]> arguments = null)
        {
            var data = Services.KRPC.ExtensionMethods.KRPC (this).RunFunction (function, arguments);
            // A null value is signaled out of band by is_null, which the stub reports as
            // no data at all
            if (data == null) {
                if (typeof (TResult).IsValueType &&
                    System.Nullable.GetUnderlyingType (typeof (TResult)) == null)
                    throw new InvalidOperationException (
                        "The function produced a null value, which " + typeof (TResult) +
                        " cannot hold. Use a nullable type for the result.");
                return default (TResult);
            }
            return (TResult)Encoder.Decode (ByteString.CopyFrom (data), spec, this);
        }

        /// <summary>
        /// Run a lambda on the server, and return its value.
        /// </summary>
        /// <remarks>
        /// The lambda runs within a single physics tick. It is translated as
        /// <see cref="Compile{TResult}(Expression{Func{TResult}})"/> translates it.
        /// </remarks>
        public TResult Run<TResult> (Expression<Func<TResult>> expression)
        {
            CheckDisposed ();
            if (ReferenceEquals (expression, null))
                throw new ArgumentNullException (nameof (expression));
            var spec = CompiledResultSpec<TResult> (expression);
            var compiled = FunctionCompiler.Compile (this, expression, expressionRemoteTypes);
            return RunCheckedFunction<TResult> (compiled, DescribedSpec (spec, compiled));
        }

        /// <summary>
        /// Run a lambda with no value on the server.
        /// </summary>
        /// <remarks>
        /// The lambda runs within a single physics tick. It is translated as
        /// <see cref="Compile(Expression{Action})"/> translates it.
        /// </remarks>
        public void Run (Expression<Action> expression)
        {
            CheckDisposed ();
            var compiled = FunctionCompiler.Compile (this, expression, expressionRemoteTypes);
            Services.KRPC.ExtensionMethods.KRPC (this).RunFunction (compiled);
        }

        /// <summary>
        /// Run a server side function with no value.
        /// </summary>
        /// <remarks>
        /// The function runs within a single physics tick, with the arguments passed to its
        /// parameters. Throws an exception if a parameter cannot take its argument.
        /// </remarks>
        public void Run (Services.KRPC.Expression function, params object[] args)
        {
            CheckDisposed ();
            if (ReferenceEquals (function, null))
                throw new ArgumentNullException (nameof (function));
            Services.KRPC.ExtensionMethods.KRPC (this).RunFunction (function, EncodeArguments (function, args));
        }

        /// <summary>
        /// The encoded arguments of a call to a server side function, one per parameter.
        /// </summary>
        /// <remarks>
        /// A single null passed for the arguments is a null argument. Throws an exception if
        /// the number of arguments differs from the number of parameters, or a parameter
        /// cannot take its argument.
        /// </remarks>
        IList<byte[]> EncodeArguments (Services.KRPC.Expression function, object[] args)
        {
            if (args == null)
                args = new object [] { null };
            var functionId = RemoteObject.GetObjectId (function);
            IList<global::KRPC.Schema.KRPC.Type> types;
            if (!functionParameterTypes.TryGetValue (functionId, out types)) {
                types = function.ParameterTypes.Select (BuildType).ToList ();
                functionParameterTypes [functionId] = types;
            }
            // An array of another element type passed as the only argument reaches params as
            // the arguments array itself, by array covariance
            if (types.Count == 1 && args.GetType () != typeof(object[]))
                args = new object [] { args };
            if (args.Length != types.Count)
                throw new ArgumentException (
                    "The function takes " + types.Count + (types.Count == 1 ? " argument" : " arguments") +
                    ", got " + args.Length);
            if (args.Length == 0)
                return null;
            var encoded = new List<byte[]> ();
            for (int i = 0; i < args.Length; i++)
                encoded.Add (EncodeArgument (args [i], types [i], i));
            return encoded;
        }

        /// <summary>
        /// The implicit conversions from one numeric type to the numeric types a parameter can
        /// have, as C# makes them.
        /// </summary>
        static readonly Dictionary<System.Type, System.Type[]> numericWidenings =
            new Dictionary<System.Type, System.Type[]> {
                { typeof (sbyte), new [] { typeof (int), typeof (long), typeof (float), typeof (double) } },
                { typeof (byte), new [] {
                        typeof (int), typeof (uint), typeof (long), typeof (ulong), typeof (float), typeof (double) } },
                { typeof (short), new [] { typeof (int), typeof (long), typeof (float), typeof (double) } },
                { typeof (ushort), new [] {
                        typeof (int), typeof (uint), typeof (long), typeof (ulong), typeof (float), typeof (double) } },
                { typeof (char), new [] {
                        typeof (int), typeof (uint), typeof (long), typeof (ulong), typeof (float), typeof (double) } },
                { typeof (int), new [] { typeof (long), typeof (float), typeof (double) } },
                { typeof (uint), new [] { typeof (long), typeof (ulong), typeof (float), typeof (double) } },
                { typeof (long), new [] { typeof (float), typeof (double) } },
                { typeof (ulong), new [] { typeof (float), typeof (double) } },
                { typeof (float), new [] { typeof (double) } }
            };

        // A signed integer that is not negative converts to an unsigned type at least as wide
        static bool IsUnsignedConstant (object value, System.Type parameterType)
        {
            if (value is sbyte || value is short || value is int)
                return Convert.ToInt32 (value, System.Globalization.CultureInfo.InvariantCulture) >= 0 &&
                    (parameterType == typeof(uint) || parameterType == typeof(ulong));
            if (value is long)
                return (long)value >= 0 && parameterType == typeof(ulong);
            return false;
        }

        static byte[] EncodeArgument (object value, global::KRPC.Schema.KRPC.Type type, int position)
        {
            if (value == null)
                return null;
            var parameterType = FunctionCompiler.NaturalType (type);
            if (parameterType != null && !parameterType.IsInstanceOfType (value)) {
                System.Type[] widenings;
                if ((numericWidenings.TryGetValue (value.GetType (), out widenings) &&
                     Array.IndexOf (widenings, parameterType) >= 0) ||
                    IsUnsignedConstant (value, parameterType)) {
                    // Convert has no conversion from a char to a floating point type
                    if (value is char)
                        value = (int)(char)value;
                    value = Convert.ChangeType (value, parameterType, System.Globalization.CultureInfo.InvariantCulture);
                }
            }
            if (parameterType == null || !parameterType.IsInstanceOfType (value))
                throw new ArgumentException (
                    "Argument " + (position + 1) + " is a " + value.GetType () + ", which the " +
                    "function's parameter of type " + (parameterType != null ? parameterType.ToString () : "unknown") +
                    " cannot take");
            try {
                return Encoder.Encode (value, TypeSpec.FromDescribed (parameterType, type)).ToByteArray ();
            } catch (ArgumentException exception) {
                // Such as a null element of a collection that cannot hold one
                throw new ArgumentException (
                    "Argument " + (position + 1) + " cannot be encoded: " + exception.Message, exception);
            }
        }

        /// <summary>
        /// Create a stream of a server side function's value.
        /// </summary>
        /// <remarks>
        /// Throws an exception if the type parameter cannot hold the function's value.
        /// </remarks>
        public Stream<TResult> AddStream<TResult> (Services.KRPC.Expression function)
        {
            CheckDisposed ();
            if (ReferenceEquals (function, null))
                throw new ArgumentNullException (nameof (function));
            return AddCheckedStream<TResult> (function, FunctionSpec<TResult> (function, true));
        }

        Stream<TResult> AddCheckedStream<TResult> (Services.KRPC.Expression function, TypeSpec spec)
        {
            var stream = Services.KRPC.ExtensionMethods.KRPC (this).AddFunctionStream (function);
            return new Stream<TResult> (this, stream.Id, spec);
        }

        /// <summary>
        /// The spec a compiled lambda's values are decoded at, from the type of its body.
        /// </summary>
        /// <remarks>
        /// A lazily evaluated sequence is decoded as a list. Throws an exception if the type
        /// cannot be decoded, before the function runs.
        /// </remarks>
        static TypeSpec CompiledResultSpec<TResult> (LambdaExpression expression)
        {
            var resultType = typeof (TResult);
            if (resultType.IsGenericType &&
                resultType.GetGenericTypeDefinition () == typeof (System.Linq.IOrderedEnumerable<>))
                throw new FunctionCompilationException (
                    "The function evaluates to an ordered sequence, which is sent as a list. " +
                    "Call ToList() to evaluate to the list.");
            var type = FunctionCompiler.ResultBody (expression).Type;
            if (FunctionCompiler.IsALazySequence (type))
                type = typeof (IList<>).MakeGenericType (type.GetGenericArguments ());
            var spec = TypeSpec.For (type);
            try {
                Encoder.BuildDecoders (spec);
            } catch (ArgumentException exception) {
                throw new FunctionCompilationException (
                    "The function evaluates to a " + type + ", which cannot be received from " +
                    "the server", exception);
            }
            return spec;
        }

        /// <summary>
        /// The spec a lambda compiled for a single use is decoded at, with the nullability the
        /// server gives the reference types inside a collection or a tuple.
        /// </summary>
        /// <remarks>
        /// The function is not used again, so its type is not cached.
        /// </remarks>
        TypeSpec DescribedSpec (TypeSpec spec, Services.KRPC.Expression function)
        {
            if (!TypeSpec.HoldsAReference (spec.Type))
                return spec;
            var described = TypeSpec.FromDescribed (spec.Type, IntrospectReturnType (function));
            Encoder.BuildDecoders (described);
            return described;
        }

        /// <summary>
        /// The type of the values a function evaluates to, or null for a function with no value.
        /// </summary>
        global::KRPC.Schema.KRPC.Type FunctionReturnType (Services.KRPC.Expression function)
        {
            var functionId = RemoteObject.GetObjectId (function);
            global::KRPC.Schema.KRPC.Type type;
            if (!functionReturnTypes.TryGetValue (functionId, out type)) {
                type = IntrospectReturnType (function);
                functionReturnTypes [functionId] = type;
            }
            return type;
        }

        static global::KRPC.Schema.KRPC.Type IntrospectReturnType (Services.KRPC.Expression function)
        {
            return function.HasReturnType ? BuildType (function.ReturnType) : null;
        }

        /// <summary>
        /// The spec a server side function's values are decoded at, for a type parameter.
        /// </summary>
        /// <remarks>
        /// Throws an exception if the type parameter cannot hold the function's value.
        /// </remarks>
        TypeSpec FunctionSpec<TResult> (Services.KRPC.Expression function, bool streamed = false)
        {
            var type = FunctionReturnType (function);
            if (type == null)
                throw new InvalidOperationException (streamed
                    ? "The function does not evaluate to a value, so it cannot be streamed."
                    : "The function does not evaluate to a value. Call Run " +
                      "without a type parameter.");
            var decoded = FunctionCompiler.DecodedType (typeof (TResult), type);
            if (decoded == null)
                throw new InvalidOperationException (
                    "The function evaluates to a value that " + typeof (TResult) + " cannot hold");
            return TypeSpec.FromDescribed (decoded, type);
        }

        /// <summary>
        /// The protocol buffer message describing a type, built by introspecting it on the server.
        /// </summary>
        static global::KRPC.Schema.KRPC.Type BuildType (Services.KRPC.Type remote)
        {
            var type = new global::KRPC.Schema.KRPC.Type {
                Code = (global::KRPC.Schema.KRPC.Type.Types.TypeCode)(int)remote.Code
            };
            var service = remote.Service;
            if (service.Length > 0) {
                type.Service = service;
                type.Name = remote.Name;
            }
            foreach (var element in remote.Types)
                type.Types_.Add (BuildType (element));
            type.Nullable = remote.Nullable;
            return type;
        }

        /// <summary>
        /// Invoke a remote procedure.
        /// Should not be called directly. This interface is used by service client stubs.
        /// </summary>
        public ProcedureResult Invoke (string service, string procedure, IList<ByteString> arguments = null)
        {
            return Invoke (service, procedure, arguments, null);
        }

        /// <summary>
        /// Invoke a remote procedure that returns a value of the given type.
        /// Should not be called directly. This interface is used by service client stubs.
        /// </summary>
        public ProcedureResult Invoke (string service, string procedure, IList<ByteString> arguments, System.Type returnType)
        {
            CheckDisposed ();
            var request = new Request ();
            request.Calls.Add (GetCall (service, procedure, arguments));
            // The reader registers a stream or an event the call adds before it reads the
            // stream's first update
            var streamType = returnType == typeof (global::KRPC.Schema.KRPC.Stream) || returnType == typeof (Event)
                ? returnType : null;
            var response = session.Call (request, streamType);
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
            Expression rpc;
            return GetCall (expression, out rpc);
        }

        static ProcedureCall GetCall (LambdaExpression expression, out Expression rpc)
        {
            if (ReferenceEquals (expression, null))
                throw new ArgumentNullException (nameof (expression));

            if (!IsASingleCall (expression, out rpc))
                throw new ArgumentException ("Invalid expression. Must consist of a method call or property accessor only.");

            var methodCallExpression = rpc as MethodCallExpression;
            if (methodCallExpression != null)
                return GetCall (methodCallExpression);
            return GetCall ((MemberExpression)rpc);
        }

        /// <summary>
        /// Whether a lambda is a single method call or property access, which is streamed
        /// as that remote procedure call.
        /// </summary>
        static bool IsASingleCall (LambdaExpression expression, out Expression rpc)
        {
            return ExpressionUtils.TryFindStreamedRpc (expression.Body, out rpc) && rpc != null &&
                ExpressionUtils.IsIdentityWrapper (expression.Body, rpc) &&
                (rpc is MethodCallExpression || rpc is MemberExpression);
        }

        /// <summary>
        /// Whether a lambda is a call of a KRPC service procedure with fixed arguments, wrapped
        /// in conversions, with a function that makes the conversions.
        /// </summary>
        static bool IsAConvertedKRPCCall<TResult> (
            LambdaExpression expression, out Expression rpc, out Func<object, TResult> convert)
        {
            var conversions = new List<UnaryExpression> ();
            rpc = expression.Body;
            while (rpc.NodeType == ExpressionType.Convert || rpc.NodeType == ExpressionType.ConvertChecked) {
                conversions.Insert (0, (UnaryExpression)rpc);
                rpc = ((UnaryExpression)rpc).Operand;
            }
            convert = null;
            var member = rpc is MethodCallExpression
                ? ((MethodCallExpression)rpc).Method : (rpc as MemberExpression)?.Member;
            if (member == null || conversions.Count == 0 || FunctionCompiler.HasRemoteArguments (rpc))
                return false;
            var attributes = member.GetCustomAttributes (typeof(RPCAttribute), false);
            if (attributes.Length != 1 || ((RPCAttribute)attributes [0]).Service != "KRPC")
                return false;
            var value = Expression.Parameter (typeof(object), "value");
            Expression converted = Expression.Convert (value, rpc.Type);
            foreach (var conversion in conversions)
                converted = Expression.MakeUnary (
                    conversion.NodeType, converted, conversion.Type, conversion.Method);
            if (converted.Type != typeof(TResult))
                converted = Expression.Convert (converted, typeof(TResult));
            convert = Expression.Lambda<Func<object, TResult>> (converted, value).Compile ();
            return true;
        }

        static ProcedureCall GetCall (Expression rpc)
        {
            var methodCall = rpc as MethodCallExpression;
            return methodCall != null ? GetCall (methodCall) : GetCall ((MemberExpression)rpc);
        }

        /// <summary>
        /// The RPC attribute the stub in the expression carries, which names the procedure and
        /// the class holding the specs of its values.
        /// </summary>
        static RPCAttribute GetRPCAttribute (Expression rpc)
        {
            var methodCall = rpc as MethodCallExpression;
            if (methodCall != null)
                return GetRPCAttribute (methodCall.Method, "Method called");
            var member = rpc as MemberExpression;
            if (member != null)
                return GetRPCAttribute (member.Member, "Property accessed");
            throw new ArgumentException (
                "Invalid expression. Must consist of a method call or property accessor only.");
        }

        static RPCAttribute GetRPCAttribute (MemberInfo member, string description)
        {
            object[] attributes = member.GetCustomAttributes (typeof(RPCAttribute), false);
            if (attributes.Length != 1)
                throw new ArgumentException (
                    "Invalid expression. " + description + " must be backed by a RPC.");
            return (RPCAttribute)attributes [0];
        }

        internal static ProcedureCall GetCall (MethodCallExpression expression)
        {
            var method = expression.Method;
            var attribute = GetRPCAttribute (method, "Method called");

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
                arguments.Add (Encoder.Encode (
                    instanceValue,
                    ProcedureSpecs.ParameterSpec (attribute, arguments.Count, instanceType)));
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
                // The arguments are built in the order the procedure declares its parameters,
                // so the one being added is at the position the count has reached
                var spec = ProcedureSpecs.ParameterSpec (attribute, arguments.Count, type);
                arguments.Add (Encoder.Encode (value, spec));
                position++;
            }

            return GetCall (attribute.Service, attribute.Procedure, arguments);
        }

        internal static ProcedureCall GetCall (MemberExpression expression)
        {
            var member = expression.Member;
            var attribute = GetRPCAttribute (member, "Property accessed");

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
                arguments.Add (Encoder.Encode (
                    instanceValue,
                    ProcedureSpecs.ParameterSpec (attribute, arguments.Count, instanceType)));
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

        // Read by the reader while a service registers its exception types
        readonly ConcurrentDictionary<string, System.Type> exceptionTypes =
            new ConcurrentDictionary<string, System.Type> ();

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
            if (error.StackTrace.Length > 0)
                message += "\nServer stack trace:\n" + error.StackTrace;
            if (error.Service.Length > 0 && error.Name.Length > 0) {
                var key = error.Service + "." + error.Name;
                if (key == "KRPC.InvalidOperationException")
                    return new InvalidOperationException (message);
                if (key == "KRPC.ArgumentException")
                    return new ArgumentException (message);
                if (key == "KRPC.ArgumentNullException")
                    return new ArgumentNullException (string.Empty, message);
                if (key == "KRPC.ArgumentOutOfRangeException")
                    return new ArgumentOutOfRangeException (string.Empty, message);
                if (key == "KRPC.NullReferenceException")
                    return new NullReferenceException (message);
                if (key == "KRPC.KeyNotFoundException")
                    return new KeyNotFoundException (message);
                if (key == "KRPC.IndexOutOfRangeException")
                    return new IndexOutOfRangeException (message);
                if (key == "KRPC.DivideByZeroException")
                    return new DivideByZeroException (message);
                if (key == "KRPC.NotSupportedException")
                    return new NotSupportedException (message);
                if (key == "KRPC.TimeoutException")
                    return new TimeoutException (message);
                if (key == "KRPC.OverflowException")
                    return new OverflowException (message);
                System.Type exnType;
                if (!exceptionTypes.TryGetValue (key, out exnType)) {
                    // The type is unknown here if the service it belongs to was never
                    // registered. Report the error itself, named by its type on the server, so
                    // that the failure to build an exception for it does not hide it.
                    return new RPCException (key + ": " + message);
                }
                return (System.Exception)Activator.CreateInstance (exnType, new [] { message });
            }
            return new RPCException (message);
        }
    }
}
