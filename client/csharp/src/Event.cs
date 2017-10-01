using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
        /// Start the event
        /// </summary>
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
        /// Wait until the event occurs.
        /// </summary>
        public void Wait (double timeout = -1) {
            if (!stream.stream.Started)
                Start();
            stream.stream.Value = false;
            while (!stream.Get ()) {
                var origValue = stream.Get ();
                stream.Wait (timeout);
                if (timeout >= 0 && stream.Get () == origValue)
                    // Value did not change, must have timed out
                    return;
            }
        }

        /// <summary>
        /// Add a callback that is invoked whenever the stream is updated.
        /// </summary>
        public void AddCallback (Action callback) {
            stream.AddCallback ((bool x) => { if (x) callback(); });
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
