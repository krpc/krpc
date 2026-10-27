using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using KRPC.Conformance;
using ConnectionResponse = KRPC.Schema.KRPC.ConnectionResponse;
using Disconnect = KRPC.Schema.KRPC.Disconnect;
using Error = KRPC.Conformance.Error;
using Exception = System.Exception;

namespace KRPC.Client.Conformance
{
    /// <summary>
    /// Runs the commands from the driver against its connections.
    /// </summary>
    sealed class Testee
    {
        readonly Writer output;
        // The connections, by the driver's name for each
        readonly Dictionary<string, Connection> connections = new Dictionary<string, Connection> ();
        // The name of the connection the command this thread runs is for
        [ThreadStatic]
        static string currentConnection;
        // The callbacks the driver added, by its id, each with the action that removes it
        readonly Dictionary<ulong, Action> callbacks = new Dictionary<ulong, Action> ();
        // The releases of blocked callbacks, by callback id
        readonly Dictionary<ulong, SemaphoreSlim> releases = new Dictionary<ulong, SemaphoreSlim> ();
        // The number of calls of each callback, by callback id
        readonly Dictionary<ulong, uint> calls = new Dictionary<ulong, uint> ();
        // The conditions this thread holds, the last taken on top
        readonly ThreadLocal<Stack<object>> held = new ThreadLocal<Stack<object>> (() => new Stack<object> ());

        public Testee (Writer output)
        {
            this.output = output;
        }

        public void Run (Command command)
        {
            output.Write (new Output { Reply = Reply (command) });
        }

        Reply Reply (Command command)
        {
            var reply = new Reply { Id = command.Id, Thread = ThreadName () };
            currentConnection = command.Connection;
            try {
                if (command.Condition != 0) {
                    lock (ConditionOf (command.Condition))
                        Dispatch (command, reply);
                } else {
                    Dispatch (command, reply);
                }
            } catch (Exception exn) {
                reply.Error = ToError (exn);
            }
            return reply;
        }

        static string ThreadName ()
        {
            return Thread.CurrentThread.ManagedThreadId.ToString (System.Globalization.CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Whether an interrupt is pending on this thread, which takes it.
        /// </summary>
        static bool InterruptPending ()
        {
            try {
                Thread.Sleep (0);
                return false;
            } catch (ThreadInterruptedException) {
                return true;
            }
        }

        /// <summary>
        /// Throws for a behavior the language has no way to carry out.
        /// </summary>
        static void CheckBehavior (Behavior behavior)
        {
            if (behavior != null && behavior.ActionCase == Behavior.ActionOneofCase.FailFatally)
                throw new UnsupportedException ("C# has no kind of error apart from its ordinary exceptions");
        }

        static object ConditionOf (ulong handle)
        {
            var target = Handles.Get (handle);
            var evnt = target as Event;
            return evnt != null ? evnt.Condition : ((IStreamHandle)target).Condition;
        }

        /// <summary>
        /// Writes the notification for a call of a callback, then does what its behavior says.
        /// </summary>
        void CallBack (ulong callback, Behavior behavior, Action<Notification> fill = null)
        {
            var notification = new Notification { Callback = callback, Thread = ThreadName () };
            if (fill != null)
                fill (notification);
            output.Write (new Output { Notification = notification });
            if (behavior == null)
                return;
            uint count;
            lock (calls) {
                calls.TryGetValue (callback, out count);
                calls [callback] = ++count;
            }
            if (count <= behavior.Skip || (behavior.Only > 0 && count > behavior.Only))
                return;
            if (behavior.Sleep > 0)
                Thread.Sleep (TimeSpan.FromSeconds (behavior.Sleep));
            try {
                switch (behavior.ActionCase) {
                case Behavior.ActionOneofCase.Block:
                    Release (callback).Wait ();
                    break;
                case Behavior.ActionOneofCase.Run:
                    var result = Reply (behavior.Run);
                    output.Write (new Output { Notification = new Notification { Callback = callback, Thread = ThreadName (), Result = result } });
                    break;
                case Behavior.ActionOneofCase.Fail:
                    throw new InvalidOperationException (behavior.Fail);
                }
            } finally {
                if (behavior.ReportReturn)
                    output.Write (new Output { Notification = new Notification { Callback = callback, Thread = ThreadName (), Returned = true } });
            }
        }

        SemaphoreSlim Release (ulong callback)
        {
            lock (releases) {
                SemaphoreSlim release;
                if (!releases.TryGetValue (callback, out release)) {
                    release = new SemaphoreSlim (0);
                    releases [callback] = release;
                }
                return release;
            }
        }

        public void Close ()
        {
            lock (connections) {
                foreach (var connection in connections.Values)
                    connection.Dispose ();
            }
        }

        void Dispatch (Command command, Reply reply)
        {
            switch (command.CommandCase) {
            case Command.CommandOneofCase.Connect:
                Connect (command.Connect);
                reply.Value = Result.None ();
                break;
            case Command.CommandOneofCase.Close:
                if (command.Close.Interrupted) {
                    Thread.CurrentThread.Interrupt ();
                    Connected ().Dispose ();
                    reply.Value = Result.Bool (InterruptPending ());
                } else {
                    Connected ().Dispose ();
                    reply.Value = Result.None ();
                }
                break;
            case Command.CommandOneofCase.Invoke:
                if (command.Invoke.Through != null)
                    throw new UnsupportedException ("A static method is called through its class");
                reply.Value = Conformance.Dispatch.Invoke (Connected (), command.Invoke);
                break;
            case Command.CommandOneofCase.AddStream:
                if (command.AddStream.Scoped)
                    throw new UnsupportedException ("A stream has no scope idiom");
                reply.Stream = Handles.Add (Conformance.Dispatch.AddStream (Connected (), command.AddStream.Invoke));
                break;
            case Command.CommandOneofCase.Call:
                reply.Value = Functions.Call (Connected (), command.Call.Invoke);
                break;
            case Command.CommandOneofCase.AddFunctionStream:
                reply.Stream = Handles.Add (Functions.AddStream (Connected (), command.AddFunctionStream));
                break;
            case Command.CommandOneofCase.AddFunctionEvent:
                reply.Value = Functions.AddEvent (Connected (), command.AddFunctionEvent);
                break;
            case Command.CommandOneofCase.RunFunction:
                reply.Value = Functions.Run (Connected (), command.RunFunction);
                break;
            case Command.CommandOneofCase.WaitOnCondition:
                reply.Value = Result.Bool (Monitor.Wait (
                    ConditionOf (command.WaitOnCondition.Stream), TimeSpan.FromSeconds (command.WaitOnCondition.Timeout)));
                break;
            case Command.CommandOneofCase.NotifyCondition:
                var notified = ConditionOf (command.NotifyCondition.Stream);
                lock (notified)
                    Monitor.PulseAll (notified);
                reply.Value = Result.None ();
                break;
            case Command.CommandOneofCase.DropHandle:
                Handles.Remove (command.DropHandle.Stream);
                GC.Collect ();
                GC.WaitForPendingFinalizers ();
                reply.Value = Result.None ();
                break;
            case Command.CommandOneofCase.CopyStream:
                reply.Stream = Handles.Add (Handles.Get (command.CopyStream.Stream));
                break;
            case Command.CommandOneofCase.CompareHandles:
                reply.Value = Result.Bool (Equal (ClientObject (command.CompareHandles.A), ClientObject (command.CompareHandles.B)));
                break;
            case Command.CommandOneofCase.HashHandle:
                reply.Value = Result.Uint64 ((uint)ClientObject (command.HashHandle.Stream).GetHashCode ());
                break;
            case Command.CommandOneofCase.SetRate:
                Stream (command.SetRate.Stream).Rate = command.SetRate.Rate;
                reply.Value = Result.None ();
                break;
            case Command.CommandOneofCase.Rate:
                reply.Value = Result.Float (Stream (command.Rate.Stream).Rate);
                break;
            case Command.CommandOneofCase.Remove:
                var removed = Handles.Get (command.Remove.Stream);
                if (removed is Event)
                    ((Event)removed).Remove ();
                else
                    ((IStreamHandle)removed).Remove ();
                reply.Value = Result.None ();
                break;
            case Command.CommandOneofCase.WaitEvent:
                var evnt = GetEvent (command.WaitEvent.Event);
                var eventTimeout = command.WaitEvent.HasTimeout ? command.WaitEvent.Timeout : double.PositiveInfinity;
                lock (evnt.Condition)
                    reply.Value = Result.Bool (evnt.Wait (eventTimeout));
                break;
            case Command.CommandOneofCase.AddEventCallback:
                CheckBehavior (command.AddEventCallback.Behavior);
                AddEventCallback (command.AddEventCallback);
                reply.Value = Result.None ();
                break;
            case Command.CommandOneofCase.EventStream:
                reply.Stream = Handles.Add (Result.Stream (GetEvent (command.EventStream.Event).Stream, Result.Bool));
                break;
            case Command.CommandOneofCase.Get:
                reply.Value = Stream (command.Get.Stream).Get ();
                break;
            case Command.CommandOneofCase.Wait:
                var timeout = command.Wait.HasTimeout ? command.Wait.Timeout : double.PositiveInfinity;
                reply.Value = Result.Bool (Stream (command.Wait.Stream).Wait (timeout));
                break;
            case Command.CommandOneofCase.AddCallback:
                CheckBehavior (command.AddCallback.Behavior);
                AddCallback (command.AddCallback);
                reply.Value = Result.None ();
                break;
            case Command.CommandOneofCase.AddEndCallback:
                CheckBehavior (command.AddEndCallback.Behavior);
                AddEndCallback (command.AddEndCallback);
                reply.Value = Result.None ();
                break;
            case Command.CommandOneofCase.AddUpdateCallback:
                throw new UnsupportedException ("A connection has no stream update callback");
            case Command.CommandOneofCase.RemoveCallback:
                RemoveCallback (command.RemoveCallback.Callback);
                reply.Value = Result.None ();
                break;
            case Command.CommandOneofCase.RemoveEndCallback:
                RemoveCallback (command.RemoveEndCallback.Callback);
                reply.Value = Result.None ();
                break;
            case Command.CommandOneofCase.Release:
                Release (command.Release.Callback).Release ();
                reply.Value = Result.None ();
                break;
            case Command.CommandOneofCase.HoldCondition:
                if (command.HoldCondition.Stream == 0)
                    throw new UnsupportedException ("A connection has no stream update condition");
                var condition = ConditionOf (command.HoldCondition.Stream);
                Monitor.Enter (condition);
                held.Value.Push (condition);
                reply.Value = Result.None ();
                break;
            case Command.CommandOneofCase.ReleaseCondition:
                var taken = held.Value.Pop ();
                Monitor.Exit (command.ReleaseCondition.Stream == 0 ? taken : ConditionOf (command.ReleaseCondition.Stream));
                reply.Value = Result.None ();
                break;
            case Command.CommandOneofCase.WaitForUpdate:
                throw new UnsupportedException ("A connection has no wait for a stream update");
            case Command.CommandOneofCase.Hash:
                reply.Value = Result.Uint64 ((uint)Arg.Any (command.Hash.A).GetHashCode ());
                break;
            case Command.CommandOneofCase.Order:
                reply.Value = Result.Sint32 (Order (Arg.Any (command.Order.A), Arg.Any (command.Order.B)));
                break;
            case Command.CommandOneofCase.ObjectId:
                reply.Value = Result.Uint64 (RemoteObject.GetObjectId (Arg.Object<KRPC.Client.Services.TestService.TestClass> (command.ObjectId.A)));
                break;
            case Command.CommandOneofCase.Members:
                // The stubs are compiled from the service definitions
                reply.Value = new Value { Null = true };
                break;
            case Command.CommandOneofCase.Encode:
                reply.Value = Codec.Encode (command.Encode);
                break;
            case Command.CommandOneofCase.Decode:
                reply.Value = Codec.Decode (Connected (), command.Decode);
                break;
            case Command.CommandOneofCase.Compare:
                reply.Value = Result.Bool (Equal (Arg.Any (command.Compare.A), Arg.Any (command.Compare.B)));
                break;
            case Command.CommandOneofCase.Pause:
            case Command.CommandOneofCase.AwaitPause:
            case Command.CommandOneofCase.Resume:
                throw new InvalidOperationException ("The client has no race points");
            case Command.CommandOneofCase.FailWrite:
                Interlocked.Exchange (ref writeFault, command.FailWrite.Message);
                reply.Value = Result.None ();
                break;
            default:
                throw new NotSupportedException ("Unknown command " + command.CommandCase);
            }
        }

        void AddCallback (AddCallback command)
        {
            var callback = command.Callback;
            var stream = Stream (command.Stream);
            var behavior = command.Behavior;
            var tag = stream.AddCallback (value => CallBack (callback, behavior, x => x.Value = value));
            lock (callbacks)
                callbacks [callback] = () => stream.RemoveCallback (tag);
        }

        void AddEventCallback (AddEventCallback command)
        {
            var callback = command.Callback;
            var evnt = GetEvent (command.Event);
            var behavior = command.Behavior;
            var tag = evnt.AddCallback (() => CallBack (callback, behavior));
            lock (callbacks)
                callbacks [callback] = () => evnt.RemoveCallback (tag);
        }

        void AddEndCallback (AddEndCallback command)
        {
            var callback = command.Callback;
            var behavior = command.Behavior;
            Action<Exception> notify = exn => {
                var ending = new Ending ();
                if (exn != null)
                    ending.Error = ToError (exn);
                CallBack (callback, behavior, x => x.Ending = ending);
            };
            Action remove;
            var target = Handles.Get (command.Stream);
            if (target is Event) {
                var evnt = (Event)target;
                var tag = evnt.AddEndCallback (notify);
                remove = () => evnt.RemoveEndCallback (tag);
            } else {
                var stream = (IStreamHandle)target;
                var tag = stream.AddEndCallback (notify);
                remove = () => stream.RemoveEndCallback (tag);
            }
            lock (callbacks)
                callbacks [callback] = remove;
        }

        void RemoveCallback (ulong callback)
        {
            Action remove;
            lock (callbacks) {
                remove = callbacks [callback];
                callbacks.Remove (callback);
            }
            remove ();
        }

        void Connect (Connect command)
        {
            var name = currentConnection ?? string.Empty;
            var closed = new List<Connection> ();
            lock (connections) {
                Connection existing;
                if (name.Length > 0) {
                    if (connections.TryGetValue (name, out existing))
                        closed.Add (existing);
                    connections.Remove (name);
                } else {
                    closed.AddRange (connections.Values);
                    connections.Clear ();
                }
            }
            foreach (var existing in closed)
                existing.Dispose ();
            if (name.Length == 0) {
                Handles.Clear ();
                lock (callbacks)
                    callbacks.Clear ();
                lock (releases)
                    releases.Clear ();
            }
            Connection connection;
            TimeSpan? handshakeTimeout = null;
            TimeSpan? stallTimeout = null;
            if (command.HasStallTimeout)
                stallTimeout = ToTimeSpan (command.StallTimeout);
            if (command.HasHandshakeTimeout)
                handshakeTimeout = ToTimeSpan (command.HandshakeTimeout);
            if (command.Protocol == "localsocket") {
                connection = Connection.ConnectLocal (command.Name, command.Path, handshakeTimeout, stallTimeout);
            } else if (command.Protocol == "tcpip") {
                var address = command.Address.Length > 0 ? IPAddress.Parse (command.Address) : null;
                var port = command.Port > 0 ? (int)command.Port : 50000;
                connection = new Connection (command.Name, address, port, handshakeTimeout: handshakeTimeout, stallTimeout: stallTimeout);
            } else {
                throw new NotSupportedException ("Unknown protocol " + command.Protocol);
            }
            lock (connections)
                connections [name] = connection;
            if (name.Length == 0)
                Arg.Connection = connection;
        }

        Connection Connected ()
        {
            Connection connection;
            lock (connections) {
                if (!connections.TryGetValue (currentConnection ?? string.Empty, out connection))
                    throw new InvalidOperationException ("Not connected");
            }
            return connection;
        }

        static IStreamHandle Stream (ulong handle)
        {
            return (IStreamHandle)Handles.Get (handle);
        }

        /// <summary>
        /// The client's stream or event object for a handle.
        /// </summary>
        static object ClientObject (ulong handle)
        {
            var value = Handles.Get (handle);
            var stream = value as IStreamHandle;
            return stream != null ? stream.Stream : value;
        }

        /// <summary>
        /// Whether two values are equal, after checking that the type's equality operators
        /// agree, with the other value and with null.
        /// </summary>
        static bool Equal (object a, object b)
        {
            var equal = a == null ? b == null : a.Equals (b);
            if (a != null) {
                CheckOperator ("op_Equality", a, b, equal);
                CheckOperator ("op_Inequality", a, b, !equal);
                CheckOperator ("op_Equality", a, null, false);
                CheckOperator ("op_Inequality", a, null, true);
            }
            if (b != null) {
                CheckOperator ("op_Equality", null, b, false);
                CheckOperator ("op_Inequality", null, b, true);
            }
            return equal;
        }

        /// <summary>
        /// The sign of the order of two values, after checking that the type's comparison
        /// operators agree with it.
        /// </summary>
        static int Order (object a, object b)
        {
            var compareTo = a.GetType ().GetMethod ("CompareTo", new [] { a.GetType () });
            if (compareTo == null)
                throw new InvalidOperationException ("A " + a.GetType ().Name + " has no ordering");
            var sign = Math.Sign ((int)compareTo.Invoke (a, new [] { b }));
            CheckOperator ("op_LessThan", a, b, sign < 0);
            CheckOperator ("op_GreaterThan", a, b, sign > 0);
            CheckOperator ("op_LessThanOrEqual", a, b, sign <= 0);
            CheckOperator ("op_GreaterThanOrEqual", a, b, sign >= 0);
            CheckOperator ("op_Equality", a, b, sign == 0);
            CheckOperator ("op_Inequality", a, b, sign != 0);
            return sign;
        }

        /// <summary>
        /// Check the result of an operator the type of the operands defines, if it takes them.
        /// </summary>
        static void CheckOperator (string name, object a, object b, bool expected)
        {
            var type = (a ?? b).GetType ();
            var method = type.GetMethod (name, BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
            if (method == null)
                return;
            var parameters = method.GetParameters ();
            if (!Takes (parameters [0].ParameterType, a) || !Takes (parameters [1].ParameterType, b))
                return;
            if ((bool)method.Invoke (null, new [] { a, b }) != expected)
                throw new InvalidOperationException ("The " + name + " operator of " + type.Name + " disagrees");
        }

        static bool Takes (System.Type type, object value)
        {
            return value == null ? !type.IsValueType : type.IsInstanceOfType (value);
        }

        static Event GetEvent (ulong handle)
        {
            return (Event)Handles.Get (handle);
        }

        static TimeSpan ToTimeSpan (double seconds)
        {
            if (double.IsNaN (seconds))
                throw new UnsupportedException ("A TimeSpan cannot hold NaN");
            if (double.IsPositiveInfinity (seconds))
                return Timeout.InfiniteTimeSpan;
            return TimeSpan.FromSeconds (seconds);
        }

        // The message of the write fault armed for the client's next write
        static string writeFault;

        /// <summary>
        /// Sets the client's write fault hook, which only the testee has.
        /// </summary>
        internal static void InstallWriteFault ()
        {
            var type = typeof(Connection).Assembly.GetType ("KRPC.Client.WriteFault", true);
            Func<string> reach = () => Interlocked.Exchange (ref writeFault, null);
            type.GetField ("hook", BindingFlags.NonPublic | BindingFlags.Static).SetValue (null, reach);
        }

        static Error ToError (Exception exn)
        {
            var error = new Error { Message = exn.Message };
            for (var type = exn.GetType (); type != typeof (object); type = type.BaseType)
                error.NativeTypes.Add (type.FullName);
            var cause = exn.InnerException;
            if (cause != null)
                error.Cause = cause.GetType ().FullName + ": " + cause.Message;
            var connectionException = exn as ConnectionException;
            var rpcException = exn as RPCException;
            if (exn is UnsupportedException) {
                error.Kind = Error.Types.Kind.Unsupported;
            } else if (connectionException != null) {
                error.Kind = Error.Types.Kind.Connection;
                if (connectionException.Status.HasValue)
                    error.Status = EnumName (
                        ConnectionResponse.Descriptor.EnumTypes [0], (int)connectionException.Status.Value);
                if (connectionException.Reason.HasValue)
                    error.Reason = EnumName (
                        Disconnect.Descriptor.EnumTypes [0], (int)connectionException.Reason.Value);
            } else if (exn is ArgumentException) {
                // The client's argument error, which is also the type of a server
                // ArgumentException
                error.Kind = Error.Types.Kind.Argument;
            } else {
                error.Kind = Error.Types.Kind.Rpc;
                // A generated exception type is named after the server's, in a namespace named
                // after its service
                const string prefix = "KRPC.Client.Services.";
                var ns = exn.GetType ().Namespace ?? string.Empty;
                if (rpcException != null && ns.StartsWith (prefix, StringComparison.Ordinal)) {
                    error.Service = ns.Substring (prefix.Length);
                    error.Name = exn.GetType ().Name;
                }
            }
            return error;
        }

        // The name of an enum value, or its number for a value the schema does not name
        static string EnumName (Google.Protobuf.Reflection.EnumDescriptor type, int number)
        {
            var value = type.FindValueByNumber (number);
            return value == null ? number.ToString (System.Globalization.CultureInfo.InvariantCulture) : value.Name;
        }
    }

    /// <summary>
    /// The client offers no way to do what a command asks.
    /// </summary>
    sealed class UnsupportedException : Exception
    {
        public UnsupportedException (string message) : base (message)
        {
        }
    }
}
