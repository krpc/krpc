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
        const string NotApplicableText = "n/a";
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

            ConsoleWidgets.RegionHeader ("Network");
            DrawInfo ("Data read", BytesToString (core.BytesRead));
            DrawInfo ("Data written", BytesToString (core.BytesWritten));
            DrawInfo ("Data read rate", BytesToString ((ulong)core.BytesReadRate) + "/s");
            DrawInfo ("Data written rate", BytesToString ((ulong)core.BytesWrittenRate) + "/s");

            ConsoleWidgets.RegionHeader ("RPC Execution");
            DrawInfo ("RPCs executed", core.RPCsExecuted.ToString (CultureInfo.CurrentCulture));
            DrawInfo ("RPC rate", Math.Round (core.RPCRate).ToString (CultureInfo.CurrentCulture) + " RPC/s");
            DrawInfo ("Execution mode", config.OneRPCPerUpdate ? "One RPC per update" : (config.AdaptiveRateControl ? "Adaptive" : "Static"));
            DrawInfo ("Max. time per update", config.OneRPCPerUpdate ? NotApplicableText : Microseconds (config.MaxTimePerUpdate));
            DrawInfo ("Receive mode", config.BlockingRecv ? "Blocking" : "Non-blocking");
            DrawInfo ("Receive timeout", config.BlockingRecv ? Microseconds (config.RecvTimeout) : NotApplicableText);
            DrawInfo ("Time per update", Seconds (core.TimePerRPCUpdate));
            DrawInfo ("Poll time per update", Seconds (core.PollTimePerRPCUpdate));
            DrawInfo ("Exec time per update", Seconds (core.ExecTimePerRPCUpdate));

            ConsoleWidgets.RegionHeader ("Stream Execution");
            DrawInfo ("Current streams", core.StreamRPCs.ToString (CultureInfo.CurrentCulture));
            DrawInfo ("Stream RPCs executed", core.StreamRPCsExecuted.ToString (CultureInfo.CurrentCulture));
            DrawInfo ("Stream RPC rate", Math.Round (core.StreamRPCRate).ToString (CultureInfo.CurrentCulture) + " RPC/s");
            DrawInfo ("Time per update", Seconds (core.TimePerStreamUpdate), "stream");

            ConsoleWidgets.Rule ();
            if (ConsoleWidgets.Button ("Clear statistics"))
                core.ClearStats ();
        }

        void DrawInfo (string label, string value, string key = null)
        {
            key = key == null ? label : key + label;
            if (update || !values.ContainsKey (key))
                values [key] = value;
            ConsoleWidgets.Readout (label, values [key]);
        }

        static string Microseconds (uint value)
        {
            return value.ToString (CultureInfo.CurrentCulture) + " us";
        }

        static string Seconds (float value)
        {
            return value.ToString ("F5", CultureInfo.CurrentCulture) + " s";
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
