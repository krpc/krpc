using System;
using System.Collections.Generic;
using KRPC.Utils;

namespace KRPC
{
    /// <summary>
    /// Whether, and how long, the server waits for a client's next call. It keeps the client's
    /// recent turnarounds, each the time from a response being sent to the client's next
    /// request arriving. The server waits while the frame time a wait costs, for each call
    /// the wait gains this tick, is at most the longest call wait.
    /// </summary>
    sealed class CallWait
    {
        /// <summary>
        /// The number of turnarounds kept.
        /// </summary>
        internal const int WindowSize = 16;

        // The turnaround of a client with none measured yet, and the shortest wait. A wait
        // ends when the call arrives, so a long one costs only when the call misses it
        static readonly long priorTicks = StopwatchExtensions.MicrosecondsToTicks (1000);

        // Added to a wait, on top of a quarter of the longest turnaround, for a client's jitter
        static readonly long marginTicks = StopwatchExtensions.MicrosecondsToTicks (100);

        readonly long[] window = new long [WindowSize];
        int count;
        int next;

        // When the client's last response was sent, or zero while a call of the client's is
        // outstanding or the client has made none
        long sent;

        // Whether the wait for the client's next request has been decided, and the timestamp
        // it ends at, zero for no wait
        bool decided;
        long deadline;

        /// <summary>
        /// Record a response sent to the client, at the given timestamp.
        /// </summary>
        public void ResponseSent (long now)
        {
            sent = now;
            decided = false;
            deadline = 0;
        }

        /// <summary>
        /// Record a request from the client, which arrived at the given timestamp. A request
        /// sent ahead of the previous response, as a client that pipelines does, measures
        /// nothing about the client's turnaround, and is not recorded.
        /// </summary>
        public void RequestArrived (long arrived)
        {
            if (sent == 0)
                return;
            if (arrived >= sent) {
                window [next] = arrived - sent;
                next = (next + 1) % WindowSize;
                if (count < WindowSize)
                    count++;
            }
            sent = 0;
            decided = false;
            deadline = 0;
        }

        /// <summary>
        /// Add the client's turnarounds, in ticks, to the list.
        /// </summary>
        public void CopyTurnarounds (List<long> list)
        {
            for (int i = 0; i < count; i++)
                list.Add (window [i]);
        }

        /// <summary>
        /// The timestamp to wait until for the client's next request, or zero for no wait.
        /// The wait is decided once per response, with what was left of
        /// <paramref name="budget"/> ticks at the time. Sets <paramref name="limited"/> on the
        /// call that decides, when the server would have waited, given the budget to.
        /// </summary>
        public long Deadline (long now, long budget, long maxCallWait, out bool limited)
        {
            limited = false;
            if (sent == 0)
                return 0;
            if (!decided) {
                decided = true;
                var elapsed = now - sent;
                var wait = Wait (elapsed, Math.Min (budget, maxCallWait), maxCallWait);
                deadline = wait > 0 ? now + wait : 0;
                limited = wait == 0 && budget < maxCallWait &&
                    Wait (elapsed, maxCallWait, maxCallWait) > 0;
            }
            return deadline > now ? deadline : 0;
        }

        /// <summary>
        /// How long to wait for a request, in ticks, given that <paramref name="elapsed"/>
        /// ticks have passed since the response with no request, and that the wait may last
        /// up to <paramref name="limit"/> ticks. Zero for no wait.
        /// </summary>
        long Wait (long elapsed, long limit, long maxCallWait)
        {
            if (limit <= 0)
                return 0;
            // The turnarounds still possible, and those a wait could see out
            int possible = 0;
            int reachable = 0;
            long sum = 0;
            long longest = 0;
            var samples = count > 0 ? count : 1;
            for (int i = 0; i < samples; i++) {
                var remaining = (count > 0 ? window [i] : priorTicks) - elapsed;
                if (remaining <= 0)
                    continue;
                possible++;
                if (remaining > limit)
                    continue;
                reachable++;
                sum += remaining;
                longest = Math.Max (longest, remaining);
            }
            if (reachable == 0)
                return 0;
            var wait = Math.Min (limit, Math.Max (priorTicks, longest + longest / 4 + marginTicks));
            // A wait costs the turnaround when the request comes within it, and the whole wait
            // when it does not. Compare that cost per call gained against the longest call wait,
            // with both sides multiplied by the possible turnarounds
            var cost = sum + (possible - reachable) * wait;
            return cost <= reachable * maxCallWait ? wait : 0;
        }
    }
}
