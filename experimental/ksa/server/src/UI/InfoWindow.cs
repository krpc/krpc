using System;
using System.Collections.Generic;
using System.Globalization;
using Brutal.Numerics;
using KSA;

namespace KRPC.KSA.UI
{
    /// <summary>
    /// The server statistics window.
    /// </summary>
    sealed class InfoWindow : ConsoleWindow
    {
        const string NoValueText = "-";
        // The share of frames the budget stops the server in, past which the budget is the limit
        const float BudgetBoundShare = 0.5f;
        // The share of the target frame rate below which the game is slow
        const float SlowFrameRateShare = 0.95f;
        // Seconds between updates of the values, so that they can be read
        const double UpdateInterval = 0.2;

        readonly Core core;
        readonly Dictionary<string, string> values = new Dictionary<string, string> ();
        DateTime lastUpdate = DateTime.MinValue;
        bool update;

        public InfoWindow (Core core, bool show, Tuple<float, float, float, float> rect)
            : base ("kRPC Server Info", "KRPC-INF", new float2 (340, 680), show, rect)
        {
            this.core = core;
        }

        protected override void DrawWindow (IViewport viewport)
        {
            var now = DateTime.UtcNow;
            update = (now - lastUpdate).TotalSeconds > UpdateInterval;
            if (update)
                lastUpdate = now;
            var config = Configuration.Instance;

            ConsoleWidgets.RegionHeader ("Frame");
            DrawInfo ("Frame rate", FrameRate (config));
            DrawInfo ("Budget", Milliseconds (core.BudgetPerFrame / 1e6f));
            DrawInfo ("Server time", Milliseconds (core.TimePerFrame));
            DrawInfo ("  Executing", Milliseconds (core.ExecTimePerFrame));
            DrawInfo ("  Waiting", Milliseconds (core.WaitTimePerFrame));
            DrawInfo ("  Streams", Milliseconds (core.StreamTimePerFrame));

            ConsoleWidgets.RegionHeader ("Calls");
            DrawInfo ("Call rate", Count (core.RPCRate) + " calls/s");
            DrawInfo ("Limited by", Limit (config));
            var turnaround = core.MedianTurnaround;
            DrawInfo ("Median turnaround", turnaround > 0 ? Milliseconds (turnaround) : NoValueText);
            DrawInfo ("Deferred calls", Count (core.DeferredCalls));
            DrawInfo ("Calls executed", Count (core.RPCsExecuted));

            ConsoleWidgets.RegionHeader ("Streams");
            DrawInfo ("Open streams", Count (core.Streams));
            DrawInfo ("Evaluations", Count (core.StreamEvaluationRate) + "/s (" + Count (core.StreamEvaluations) + ")");
            DrawInfo ("Updates sent", Count (core.StreamUpdatesSent));

            ConsoleWidgets.RegionHeader ("Network");
            DrawInfo ("Read", BytesToString ((ulong)core.BytesReadRate) + "/s (" + BytesToString (core.BytesRead) + ")");
            DrawInfo ("Written", BytesToString ((ulong)core.BytesWrittenRate) + "/s (" + BytesToString (core.BytesWritten) + ")");

            ConsoleWidgets.Rule ();
            if (ConsoleWidgets.Button ("Clear statistics"))
                core.ClearStats ();
        }

        void DrawInfo (string label, string value)
        {
            if (update || !values.ContainsKey (label))
                values [label] = value;
            ConsoleWidgets.Readout (label, values [label]);
        }

        string FrameRate (Configuration config)
        {
            var rate = Count (core.FrameRate) + " fps";
            if (!config.AdaptiveRateControl)
                return rate;
            return rate + " (target " + config.TargetFrameRate.ToString (CultureInfo.CurrentCulture) + ")";
        }

        // What holds the call rate down, by the causes the server performance guide lists
        string Limit (Configuration config)
        {
            if (core.RPCRate < 1)
                return NoValueText;
            if (config.AdaptiveRateControl && core.BudgetAtFloor &&
                core.FrameRate < config.TargetFrameRate * SlowFrameRateShare)
                return "Slow game";
            if (core.BudgetBoundShare > BudgetBoundShare)
                return "Budget";
            if (core.MedianTurnaround * 1e6f > config.MaxCallWait)
                return "Max. call wait";
            return "Client";
        }

        // A time in seconds, shown in milliseconds
        static string Milliseconds (float seconds)
        {
            return string.Format (CultureInfo.CurrentCulture, "{0:F2} ms", seconds * 1000);
        }

        static string Count (double value)
        {
            return Math.Round (value).ToString ("N0", CultureInfo.CurrentCulture);
        }

        static string BytesToString (ulong bytes)
        {
            string [] suffix = { "B", "KB", "MB", "GB", "TB", "PB", "EB" };
            if (bytes == 0)
                return "0 " + suffix [0];
            var place = Convert.ToInt32 (Math.Floor (Math.Log (bytes, 1024)));
            var num = Math.Round (bytes / Math.Pow (1024, place), 1);
            return num.ToString (CultureInfo.CurrentCulture) + " " + suffix [place];
        }
    }
}
