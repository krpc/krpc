using System;
using System.Threading;
using KRPC.Schema.KRPC;

namespace KRPC.Client
{
    /// <summary>
    /// Object representing a stream.
    /// </summary>
    public class Stream<TReturnType> : IEquatable<Stream<TReturnType>>
    {
        internal readonly StreamImpl stream;
        readonly Func<object, TReturnType> convert;
        readonly object convertKey;

        internal Stream (Connection connection, ulong id)
        {
            stream = connection.StreamManager.GetStream (TypeSpec.For (typeof(TReturnType)), id);
        }

        internal Stream (Connection connection, ProcedureCall call)
        {
            stream = connection.StreamManager.AddStream (TypeSpec.For (typeof(TReturnType)), call);
        }

        internal Stream (Connection connection, ProcedureCall call, TypeSpec returnType)
        {
            stream = connection.StreamManager.AddStream (returnType, call);
        }

        internal Stream (Connection connection, ProcedureCall call, TypeSpec rpcReturnType,
                         Func<object, TReturnType> convert, object convertKey)
        {
            stream = connection.StreamManager.AddStream (rpcReturnType, call);
            this.convert = convert;
            this.convertKey = convertKey;
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
            var typedObj = obj as Stream<TReturnType>;
            return typedObj != null && Equals (typedObj);
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public bool Equals (Stream<TReturnType> other)
        {
            return !ReferenceEquals (other, null) && stream.Id == other.stream.Id &&
                Equals (convertKey, other.convertKey);
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public static bool operator == (Stream<TReturnType> lhs, Stream<TReturnType> rhs)
        {
            if (ReferenceEquals (lhs, null) || ReferenceEquals (rhs, null))
                return ReferenceEquals (lhs, rhs);
            if (ReferenceEquals (lhs, rhs))
                return true;
            return lhs.Equals (rhs);
        }

        /// <summary>
        /// Returns true if the objects are not equal.
        /// </summary>
        public static bool operator != (Stream<TReturnType> lhs, Stream<TReturnType> rhs)
        {
            if (ReferenceEquals (lhs, null) || ReferenceEquals (rhs, null))
                return !ReferenceEquals (lhs, rhs);
            if (ReferenceEquals (lhs, rhs))
                return false;
            return !(lhs.Equals (rhs));
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            var hash = stream.GetHashCode ();
            if (convertKey != null)
                hash ^= convertKey.GetHashCode ();
            return hash;
        }

        /// <summary>
        /// The rate of the stream, in Hertz.
        /// </summary>
        public float Rate {
            get { return stream.Rate; }
            set { stream.Rate = value; }
        }

        /// <summary>
        /// The most recent value of the stream.
        /// </summary>
        /// <remarks>
        /// Blocks until the first value arrives. Throws the exception that ended the stream, if
        /// it has ended.
        /// </remarks>
        public TReturnType Get () {
            if (!stream.Updated) {
                var condition = stream.Condition;
                lock (condition) {
                    while (!stream.Updated && !stream.Ended)
                        Monitor.Wait (condition);
                }
            }
            return Convert (stream.Value);
        }

        TReturnType Convert (object value)
        {
            if (convert != null)
                return convert (value);
            return (TReturnType) value;
        }

        /// <summary>
        /// Condition variable that is notified when the streams value changes.
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
        /// </remarks>
        public bool Wait (double timeout = -1) {
            stream.ThrowIfEnded ();
            var version = stream.Version;
            return stream.Wait (() => stream.Version != version, timeout);
        }

        /// <summary>
        /// Add a callback that is invoked whenever the stream is updated.
        /// </summary>
        /// <remarks>
        /// The callback is invoked straight away with the value the stream already has, if any.
        /// An error ends the stream and does not invoke the callback.
        /// </remarks>
        public int AddCallback (Action<TReturnType> callback) {
            return stream.AddCallback ((object x) => callback (Convert (x)));
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
        /// Streams of the same procedure call are one stream, so removing one removes them all.
        /// </remarks>
        public void Remove ()
        {
            stream.Remove ();
        }
    }
}
