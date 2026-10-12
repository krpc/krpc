using System;
using System.Collections.Generic;
using System.Threading;
using KRPC.Schema.KRPC;

namespace KRPC.Client
{
    /// <summary>
    /// Object representing an event.
    /// </summary>
    public class Event : IEquatable<Event>
    {
        readonly Stream<bool> stream;
        long seen;

        internal Event (Connection connection, KRPC.Schema.KRPC.Event evnt)
        {
            stream = new Stream<bool> (connection, evnt.Stream.Id);
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
            var typedObj = obj as Event;
            return typedObj != null && Equals (typedObj);
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public bool Equals (Event other)
        {
            return !ReferenceEquals (other, null) && stream == other.stream;
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public static bool operator == (Event lhs, Event rhs)
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
        public static bool operator != (Event lhs, Event rhs)
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
        /// Start the event.
        /// </summary>
        /// <remarks>
        /// Throws the exception that ended the event, if it has ended.
        /// </remarks>
        public void Start() {
            stream.Start(false);
        }

        /// <summary>
        /// Condition variable that is notified when the event occurs.
        /// </summary>
        public object Condition {
            get { return stream.Condition; }
        }

        /// <summary>
        /// Wait until the event occurs or a timeout in seconds. Returns true if the event
        /// occurred, and false on a timeout.
        /// </summary>
        /// <remarks>
        /// Returns straight away if the event has occurred since the previous wait on this
        /// event object. Throws the exception that ended the event, if it ends before or during
        /// the wait.
        /// </remarks>
        public bool Wait (double timeout = -1) {
            var impl = stream.stream;
            if (!impl.Started) {
                impl.ThrowIfEnded ();
                impl.Start ();
            }
            // A firing not yet seen is returned before the ending
            var last = seen;
            if (!impl.Wait (() => impl.Fired > last, timeout))
                return false;
            seen = impl.Fired;
            return true;
        }

        /// <summary>
        /// Add a callback that is invoked whenever the stream is updated.
        /// </summary>
        public int AddCallback (Action callback) {
            return stream.AddCallback ((bool x) => { if (x) callback(); });
        }

        /// <summary>
        /// Remove a callback, based on its key.
        /// </summary>
        public void RemoveCallback (int key) {
            stream.RemoveCallback (key);
        }

        /// <summary>
        /// Add a callback that is invoked once when the event ends, with the exception that
        /// ended it.
        /// </summary>
        /// <remarks>
        /// The callback is invoked straight away if the event has already ended.
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
        /// The underlyling stream for the event.
        /// </summary>
        public Stream<bool> Stream {
            get { return stream; }
        }

        /// <summary>
        /// Remove the event from the server.
        /// </summary>
        public void Remove ()
        {
            stream.Remove ();
        }
    }
}
