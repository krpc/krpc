using System;
using System.Diagnostics.CodeAnalysis;

namespace KRPC.Utils
{
    [SuppressMessage ("Gendarme.Rules.Smells", "AvoidSpeculativeGeneralityRule")]
    public static class EventHandlerExtensions
    {
        public static void Invoke (EventHandler handler, object sender)
        {
            if (handler != null)
                handler (sender, EventArgs.Empty);
        }

        public static void Invoke<T> (EventHandler<T> handler, object sender, T args) where T : EventArgs
        {
            if (handler != null)
                handler (sender, args);
        }
    }
}
