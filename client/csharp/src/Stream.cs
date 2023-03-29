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

        internal Stream (Connection connection, ulong id)
        {
            stream = connection.StreamManager.GetStream (typeof(TReturnType), id);
        }

        internal Stream (Connection connection, ProcedureCall call)
        {
            stream = connection.StreamManager.AddStream (typeof(TReturnType), call);
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
            return !ReferenceEquals (other, null) && stream.Id == other.stream.Id;
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
            return stream.GetHashCode ();
        }

        /// <summary>
        /// Start the stream.
        /// </summary>
        public void Start(bool wait = true) {
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
        public TReturnType Get () {
            if (!stream.Started)
                Start();
            var value = stream.Value;
            var exn = value as System.Exception;
            if (exn != null)
                throw exn;
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
        public void Wait (double timeout = -1) {
            if (!stream.Started)
                stream.Start();
            if (timeout >= 0)
                Monitor.Wait (stream.Condition, (int)(timeout*1000.0));
            else
                Monitor.Wait (stream.Condition);
        }

        /// <summary>
        /// Add a callback that is invoked whenever the stream is updated.
        /// </summary>
        public int AddCallback (Action<TReturnType> callback) {
            return stream.AddCallback ((object x) => callback((TReturnType)x));
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
        public void Remove ()
        {
            stream.Remove ();
        }
    }
}
