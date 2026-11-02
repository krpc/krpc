using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace KRPC.UI
{
    sealed class InfoWindow : Window
    {
        const float windowWidth = 300f;
        GUIStyle labelStyle, nameLabelStyle, valueLabelStyle, separatorStyle, buttonStyle;

        const string networkInfoText = "Network Info";
        const string bytesReadText = "Data read";
        const string bytesWrittenText = "Data written";
        const string bytesReadRateText = "Data read rate";
        const string bytesWrittenRateText = "Data written rate";

        const string rpcInfoText = "RPC Execution Info";
        const string rpcsExecutedText = "RPCs Executed";
        const string rpcRateText = "RPC Rate";
        const string rpcExecutionMode = "Execution mode";
        const string singleRPCModeText = "One RPC per update";
        const string adaptiveModeText = "Adaptive";
        const string staticModeText = "Static";
        const string maxTimePerUpdateText = "Max. time per update";
        const string rpcReceiveModeText = "Receive mode";
        const string blockingModeText = "Blocking";
        const string nonBlockingModeText = "Non-blocking";
        const string recvTimeoutText = "Receive timeout";
        const string tickHoldTimeoutText = "Tick hold timeout";
        const string timePerRPCUpdateText = "Time per update";
        const string pollTimePerRPCUpdateText = "Poll time per update";
        const string execTimePerRPCUpdateText = "Exec time per update";
        const string deferredCallsText = "Deferred calls";

        const string streamInfoText = "Stream Execution Info";
        const string streamsText = "Current Streams";
        const string streamEvaluationsText = "Stream Evaluations";
        const string streamEvaluationRateText = "Stream Evaluation Rate";
        const string streamUpdatesSentText = "Stream Updates Sent";
        const string timePerStreamUpdateText = "Time per update";

        const string notApplicableText = "n/a";

        const string clearStatisticsText = "Clear Statistics";

        protected override void Init ()
        {
            Title = "kRPC Server Info";

            Style.fixedWidth = windowWidth;

            var skin = Skin.DefaultSkin;

            labelStyle = new GUIStyle (skin.label);
            labelStyle.margin = new RectOffset (0, 0, 0, 0);

            nameLabelStyle = new GUIStyle (skin.label);
            nameLabelStyle.margin = new RectOffset (0, 0, 0, 0);
            nameLabelStyle.fixedWidth = 160f;

            valueLabelStyle = new GUIStyle (skin.label);
            valueLabelStyle.margin = new RectOffset (0, 0, 0, 0);
            valueLabelStyle.fixedWidth = 120f;

            separatorStyle = GUILayoutExtensions.SeparatorStyle (new Color (0f, 0f, 0f, 0.25f));
            separatorStyle.fixedHeight = 2;
            separatorStyle.stretchWidth = true;
            separatorStyle.margin = new RectOffset (2, 2, 3, 3);

            buttonStyle = new GUIStyle (skin.button);
            buttonStyle.margin = new RectOffset (0, 0, 0, 0);
        }

        const float updateTime = 0.2f;
        DateTime lastUpdate = DateTime.Now;
        bool update = true;
        // The value shown in each row, by position, as two sections have a "Time per update" row
        readonly IList<string> values = new List<string> ();
        int row;

        void DrawInfo (string label, string value)
        {
            if (row == values.Count)
                values.Add (value);
            else if (update)
                values [row] = value;
            GUILayout.BeginHorizontal ();
            GUILayout.Label (label, nameLabelStyle);
            GUILayout.Label (values [row], valueLabelStyle);
            GUILayout.EndHorizontal ();
            row++;
        }

        protected override void Draw (bool needRescale)
        {
            if (needRescale) {
                int scaledFontSize = Style.fontSize;
                Style.fixedWidth = windowWidth * GameSettings.UI_SCALE;
                labelStyle.fontSize = scaledFontSize;
                nameLabelStyle.fontSize = scaledFontSize;
                nameLabelStyle.fixedWidth = 160f * GameSettings.UI_SCALE;
                valueLabelStyle.fontSize = scaledFontSize;
                valueLabelStyle.fixedWidth = 120f * GameSettings.UI_SCALE;
                buttonStyle.fontSize = scaledFontSize;

                // Force window to resize to height of content
                Position = new Rect (Position.x, Position.y, Position.width, 0f);
            }

            update = ((DateTime.Now - lastUpdate).TotalSeconds > updateTime);
            row = 0;
            if (update)
                lastUpdate = DateTime.Now;
            var core = Core.Instance;

            GUILayout.BeginVertical ();

            GUILayout.Label (networkInfoText, labelStyle);
            DrawInfo (bytesReadText, BytesToString (core.BytesRead));
            DrawInfo (bytesWrittenText, BytesToString (core.BytesWritten));
            DrawInfo (bytesReadRateText, BytesToString ((ulong)core.BytesReadRate) + "/s");
            DrawInfo (bytesWrittenRateText, BytesToString ((ulong)core.BytesWrittenRate) + "/s");

            GUILayoutExtensions.Separator (separatorStyle);

            GUILayout.Label (rpcInfoText, labelStyle);
            Configuration config = Configuration.Instance;
            DrawInfo (rpcsExecutedText, core.RPCsExecuted.ToString (CultureInfo.CurrentCulture));
            DrawInfo (rpcRateText, Math.Round (core.RPCRate) + " RPC/s");
            DrawInfo (rpcExecutionMode, config.OneRPCPerUpdate ? singleRPCModeText : (config.AdaptiveRateControl ? adaptiveModeText : staticModeText));
            DrawInfo (maxTimePerUpdateText, config.OneRPCPerUpdate ? notApplicableText : core.MaxTimePerUpdate + " us");
            DrawInfo (rpcReceiveModeText, config.BlockingRecv ? blockingModeText : nonBlockingModeText);
            DrawInfo (recvTimeoutText, config.BlockingRecv ? config.RecvTimeout + " us" : notApplicableText);
            DrawInfo (tickHoldTimeoutText, config.TickHoldTimeout + " us");
            DrawInfo (timePerRPCUpdateText, string.Format (CultureInfo.CurrentCulture, "{0:F5} s", core.TimePerRPCUpdate));
            DrawInfo (pollTimePerRPCUpdateText, string.Format (CultureInfo.CurrentCulture, "{0:F5} s", core.PollTimePerRPCUpdate));
            DrawInfo (execTimePerRPCUpdateText, string.Format (CultureInfo.CurrentCulture, "{0:F5} s", core.ExecTimePerRPCUpdate));
            DrawInfo (deferredCallsText, core.DeferredCalls.ToString (CultureInfo.CurrentCulture));

            GUILayoutExtensions.Separator (separatorStyle);

            GUILayout.Label (streamInfoText, labelStyle);
            DrawInfo (streamsText, core.Streams.ToString (CultureInfo.CurrentCulture));
            DrawInfo (streamEvaluationsText, core.StreamEvaluations.ToString (CultureInfo.CurrentCulture));
            DrawInfo (streamEvaluationRateText, Math.Round (core.StreamEvaluationRate) + "/s");
            DrawInfo (streamUpdatesSentText, core.StreamUpdatesSent.ToString (CultureInfo.CurrentCulture));
            DrawInfo (timePerStreamUpdateText, string.Format (CultureInfo.CurrentCulture, "{0:F5} s", core.TimePerStreamUpdate));

            GUILayoutExtensions.Separator (separatorStyle);

            GUILayout.BeginHorizontal ();
            if (GUILayout.Button (clearStatisticsText, buttonStyle)) {
                core.ClearStats ();
            }
            GUILayout.EndHorizontal ();

            GUILayout.EndVertical ();
            GUI.DragWindow ();
        }

        static string BytesToString (ulong bytes)
        {
            string[] suffix = { "B", "KB", "MB", "GB", "TB", "PB", "EB" };
            if (bytes == 0)
                return "0 " + suffix [0];
            int place = Convert.ToInt32 (Math.Floor (Math.Log (bytes, 1024)));
            double num = Math.Round (bytes / Math.Pow (1024, place), 1);
            return num + " " + suffix [place];
        }
    }
}
