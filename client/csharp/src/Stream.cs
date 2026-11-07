using System;
using System.Threading;
using KRPC.Schema.KRPC;

namespace KRPC.Client
{
    /// <summary>
    /// Object representing a stream. A stream of a call or function with no value runs on each
    /// update for its effects.
    /// </summary>
    public class Stream : IEquatable<Stream>, IDisposable
    {
        internal static readonly TypeSpec NoValue = TypeSpec.For (typeof (void));

        internal readonly StreamImpl stream;

        internal Stream (StreamImpl stream)
        {
            this.stream = stream;
        }

        internal Stream (Connection connection, ulong id)
            : this (connection.StreamManager.GetStream (NoValue, id))
        {
        }

        internal Stream (Connection connection, ProcedureCall call)
            : this (connection.StreamManager.AddStream (NoValue, call))
        {
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (object obj)
        {
            if (ReferenceEquals (this, obj))
                return true;
            if (ReferenceEquals (obj, null))
                return false;
            var typedObj = obj as Stream;
            return typedObj != null && Equals (typedObj);
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public bool Equals (Stream other)
        {
            return !ReferenceEquals (other, null) && stream.Id == other.stream.Id;
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public static bool operator == (Stream lhs, Stream rhs)
        {
            if (ReferenceEquals (lhs, null) || ReferenceEquals (rhs, null))
                return ReferenceEquals (lhs, rhs);
            return ReferenceEquals (lhs, rhs) || lhs.Equals (rhs);
        }

        /// <summary>
        /// Returns true if the objects are not equal.
        /// </summary>
        public static bool operator != (Stream lhs, Stream rhs)
        {
            return !(lhs == rhs);
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            return stream.GetHashCode ();
        }

        /// <summary>
        /// The rate of the stream, in Hertz.
        /// </summary>
        public float Rate {
            get { return stream.Rate; }
            set { stream.Rate = value; }
        }

        /// <summary>
        /// Wait for the stream's first update.
        /// </summary>
        /// <remarks>
        /// Throws the exception that ended the stream, if it has ended.
        /// </remarks>
        public void Get () {
            WaitForFirstUpdate ();
            // Throws the exception that ended the stream
            _ = stream.Value;
        }

        internal void WaitForFirstUpdate ()
        {
            if (stream.Updated)
                return;
            var condition = stream.Condition;
            lock (condition) {
                while (!stream.Updated && !stream.Ended)
                    Monitor.Wait (condition);
            }
        }

        /// <summary>
        /// Condition variable that is notified when the stream updates.
        /// </summary>
        public object Condition {
            get { return stream.Condition; }
        }

        /// <summary>
        /// Wait until the next stream update or a timeout in seconds. Returns true if the stream
        /// updated, and false on a timeout.
        /// </summary>
        /// <remarks>
        /// Throws the exception that ended the stream, if it ends before or during the wait.
        /// Throws an exception if the timeout is negative or NaN.
        /// </remarks>
        public bool Wait (double timeout = double.PositiveInfinity) {
            StreamImpl.CheckTimeout (timeout);
            stream.ThrowIfEnded ();
            // An ending that arrived while this thread held the condition is read as an update
            var pendingEnding = stream.Ended;
            // Past the value this thread last read, which can be one not yet announced
            var version = Math.Max (stream.AnnouncedVersion, stream.SeenVersion);
            return stream.Wait (
                () => stream.AnnouncedVersion > version || (pendingEnding && stream.EndingAnnounced), timeout);
        }

        /// <summary>
        /// Add a callback, with no arguments, that is invoked whenever the stream updates.
        /// </summary>
        /// <remarks>
        /// A stream with no value updates once, after its first run. The callback is invoked
        /// straight away if the stream has updated. An error ends the stream and does not invoke
        /// the callback.
        /// </remarks>
        public int AddCallback (Action callback) {
            return stream.AddCallback ((object x) => callback ());
        }

        /// <summary>
        /// Remove a callback, based on its tag.
        /// </summary>
        public void RemoveCallback (int tag) {
            stream.RemoveCallback (tag);
        }

        /// <summary>
        /// Add a callback that is invoked once when the stream ends, with the exception that
        /// ended it.
        /// </summary>
        /// <remarks>
        /// The callback is invoked straight away if the stream has already ended.
        /// </remarks>
        public int AddEndCallback (Action<System.Exception> callback) {
            return stream.AddEndCallback (callback);
        }

        /// <summary>
        /// Remove an end callback, based on its tag.
        /// </summary>
        public void RemoveEndCallback (int tag) {
            stream.RemoveEndCallback (tag);
        }

        /// <summary>
        /// Remove the stream from the server.
        /// </summary>
        /// <remarks>
        /// Each add creates a separate stream, so this leaves the other streams of the same call.
        /// </remarks>
        public void Remove ()
        {
            stream.Remove ();
        }

        /// <summary>
        /// Remove the stream from the server, as <see cref="Remove"/> does, at the end of a
        /// <c>using</c> block. A failed connection throws no exception.
        /// </summary>
        public void Dispose ()
        {
            try {
                Remove ();
            } catch (System.Exception exn) when (exn is ConnectionException || exn is ObjectDisposedException) {
                // The server removes the stream with the connection, and an exception here would
                // replace the one leaving the block
            }
        }
    }

    /// <summary>
    /// Object representing a stream with a value.
    /// </summary>
    public class Stream<TReturnType> : Stream
    {
        readonly Func<object, TReturnType> convert;

        internal Stream (Connection connection, ulong id)
            : base (connection.StreamManager.GetStream (TypeSpec.For (typeof(TReturnType)), id))
        {
        }

        internal Stream (Connection connection, ulong id, TypeSpec returnType)
            : base (connection.StreamManager.GetStream (returnType, id))
        {
        }

        internal Stream (Connection connection, ProcedureCall call, TypeSpec returnType)
            : base (connection.StreamManager.AddStream (returnType, call))
        {
        }

        /// <summary>
        /// A stream of a call, whose values are converted on the client.
        /// </summary>
        internal Stream (
            Connection connection, ProcedureCall call, TypeSpec returnType,
            Func<object, TReturnType> convert)
            : base (connection.StreamManager.AddStream (returnType, call))
        {
            this.convert = convert;
        }

        /// <summary>
        /// The most recent value of the stream.
        /// </summary>
        /// <remarks>
        /// Blocks until the first value arrives. Throws the exception that ended the stream, if
        /// it has ended.
        /// </remarks>
        public new TReturnType Get () {
            WaitForFirstUpdate ();
            return Convert (stream.Value);
        }

        TReturnType Convert (object value)
        {
            if (convert != null)
                return convert (value);
            if (value == null && typeof (TReturnType).IsValueType &&
                System.Nullable.GetUnderlyingType (typeof (TReturnType)) == null)
                throw new System.InvalidOperationException (
                    "The stream's value is null, which " + typeof (TReturnType) +
                    " cannot hold. Use a nullable type for the stream.");
            return (TReturnType) value;
        }

        /// <summary>
        /// Add a callback that is invoked with each new value of the stream.
        /// </summary>
        /// <remarks>
        /// The callback is invoked straight away with the value the stream already has, if any.
        /// An error ends the stream and does not invoke the callback.
        /// </remarks>
        public int AddCallback (Action<TReturnType> callback) {
            return stream.AddCallback ((object x) => callback (Convert (x)));
        }
    }
}
