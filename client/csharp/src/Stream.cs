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
        /// Start the stream.
        /// </summary>
        /// <remarks>
        /// Throws an exception if the stream has been removed.
        /// </remarks>
        public void Start(bool wait = true) {
            CheckNotRemoved ();
            if (stream.Started)
                return;
            if (!wait) {
                stream.Start();
            } else {
                var condition = stream.Condition;
                lock (condition) {
                    stream.Start();
                    Monitor.Wait (condition);
                }
                CheckNotRemoved ();
            }
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
        /// Throws an exception if the stream has been removed.
        /// </remarks>
        public TReturnType Get () {
            CheckNotRemoved ();
            if (!stream.Started)
                Start();
            var value = stream.Value;
            var exn = value as System.Exception;
            if (exn != null)
                throw exn;
            return Convert (value);
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
        /// Wait until the next stream update.
        /// </summary>
        /// <remarks>
        /// Throws an exception if the stream has been removed.
        /// </remarks>
        public void Wait (double timeout = -1) {
            CheckNotRemoved ();
            if (!stream.Started)
                stream.Start();
            if (timeout >= 0)
                Monitor.Wait (stream.Condition, (int)(timeout*1000.0));
            else
                Monitor.Wait (stream.Condition);
            CheckNotRemoved ();
        }

        internal void CheckNotRemoved ()
        {
            if (stream.Removed)
                throw new System.InvalidOperationException ("Stream does not exist");
        }

        /// <summary>
        /// Add a callback that is invoked whenever the stream is updated.
        /// </summary>
        /// <remarks>
        /// An update that is an error is not passed to the callback. <see cref="Get"/> throws it.
        /// </remarks>
        public int AddCallback (Action<TReturnType> callback) {
            return stream.AddCallback ((object x) => {
                if (!(x is System.Exception))
                    callback (Convert (x));
            });
        }

        /// <summary>
        /// Remove a callback, based on its tag.
        /// </summary>
        public void RemoveCallback (int tag) {
          stream.RemoveCallback (tag);
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
