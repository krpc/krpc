using System;

namespace KRPC.Utils
{
    /// <summary>
    /// Extension methods for event handlers.
    /// </summary>
    public static class EventHandlerExtensions
    {
        /// <summary>
        /// Invoke an event handler, if it is non-null, otherwise do nothing.
        /// </summary>
        public static void Invoke (EventHandler handler, object sender)
        {
            if (handler != null)
                handler (sender, EventArgs.Empty);
        }

        /// <summary>
        /// Invoke an event handler, if it is non-null, otherwise do nothing.
        /// </summary>
        public static void Invoke<T> (EventHandler<T> handler, object sender, T args) where T : EventArgs
        {
            if (handler != null)
                handler (sender, args);
        }
    }
}
