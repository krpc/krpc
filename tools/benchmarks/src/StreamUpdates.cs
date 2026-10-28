using System;
using System.Collections.Generic;
using KRPC.Service;
using KRPC.Service.Messages;

namespace KRPC.Benchmarks
{
    /// <summary>
    /// The stream benchmark: update a stream of a procedure call in a loop, as the server does
    /// once per fixed update, and report what one update cost.
    ///
    /// An update runs the procedure and finds out whether its value changed since the last
    /// update sent. A loop over one stream measures the steady state of a stream whose value
    /// holds, which is the common case: each update ends as if it had been sent.
    /// </summary>
    static class StreamUpdates
    {
        internal static IDictionary<string, double> Measure (ProcedureCall call, uint iterations)
        {
            if (call == null)
                throw new ArgumentNullException (nameof (call));
            var stream = new ProcedureCallStream (call);

            // Update once outside the timed loop, so the loop measures a stream that has a
            // value, and a stream that fails reports the error rather than the price of it
            stream.Update ();
            var result = stream.StreamResult.Result;
            if (result.HasError)
                throw new ArgumentException ("Stream failed: " + result.Error.Description);
            stream.Sent ();

            return Timer.Measure (
                () => {
                    stream.Update ();
                    stream.Sent ();
                },
                iterations);
        }
    }
}
