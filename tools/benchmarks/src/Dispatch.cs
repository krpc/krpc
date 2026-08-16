using System;
using System.Collections.Generic;
using KRPC.Server.ProtocolBuffers;
using KRPC.Service;
using KRPC.Service.Messages;
using KRPC.Service.Scanner;
// KRPC.Service.Messages has a Services message of its own, so name the one that dispatches.
using Services = KRPC.Service.Services;

namespace KRPC.Benchmarks
{
    /// <summary>
    /// The end-to-end server benchmark: run a procedure call through the path the server takes
    /// for every request it receives, in a loop, and report what one call cost.
    ///
    /// What the loop covers is argument decoding, procedure dispatch, the procedure itself, and
    /// encoding the result - everything the server does per call except reading the request off a
    /// socket and writing the response back to it. What a client pays on top of that is the
    /// client-side benchmarks' subject.
    ///
    /// Because the call is named by the client, any procedure of any service is measurable
    /// without a line of code here. Nothing in it names a game object, which is what lets the
    /// same assembly be loaded by TestServer and by the game.
    /// </summary>
    static class Dispatch
    {
        /// <summary>
        /// Run a procedure call the given number of times and return what one call cost.
        /// </summary>
        /// <remarks>
        /// A call whose result is an object adds that object to the object store as it is
        /// encoded, exactly as a real call does. The store deduplicates equal instances, so a
        /// loop over one call does not grow it without bound.
        /// </remarks>
        internal static IDictionary<string, double> MeasureCall (ProcedureCall call, uint iterations)
        {
            if (call == null)
                throw new ArgumentNullException (nameof (call));
            var procedure = Services.Instance.GetProcedureSignature (call);

            // Make the call once outside the timed loop. A call the server cannot make comes back
            // as a result carrying an error, which costs about what any other result costs, so
            // without this check a mistyped call would quietly report the price of failing.
            var probe = Execute (procedure, call);
            if (probe.HasError)
                throw new ArgumentException (
                    "Call to " + procedure.FullyQualifiedName + " failed: " + probe.Error.Description);

            return Timer.Measure (
                () => { Timer.ObjectSink = Execute (procedure, call).ToProtobufMessage (); },
                iterations);
        }

        static ProcedureResult Execute (ProcedureSignature procedure, ProcedureCall call)
        {
            try {
                // ExecuteCall decodes the arguments into buffers it holds statically, but reads
                // them back out before invoking the procedure, so a benchmark call running inside
                // the server's own call to this procedure does not disturb it.
                return Services.Instance.ExecuteCall (procedure, call);
            } catch (YieldException) {
                throw new ArgumentException (
                    "Procedure " + procedure.FullyQualifiedName +
                    " yields, so it spans several updates and cannot be benchmarked in a loop");
            }
        }
    }
}
