using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace KRPC.UI
{
    sealed class InfoWindow : Window
    {
        const float windowWidth = 300f;
        const float nameWidth = 140f;
        const float valueWidth = 150f;
        const float indentWidth = 12f;
        GUIStyle labelStyle, nameLabelStyle, indentedNameLabelStyle, valueLabelStyle, separatorStyle, buttonStyle;

        const string frameText = "Frame";
        const string frameRateText = "Frame rate";
        const string budgetText = "Budget";
        const string serverTimeText = "Server time";
        const string executingText = "Executing";
        const string waitingText = "Waiting";
        const string streamTimeText = "Streams";

        const string callsText = "Calls";
        const string callRateText = "Call rate";
        const string limitedByText = "Limited by";
        const string medianTurnaroundText = "Median turnaround";
        const string deferredCallsText = "Deferred calls";
        const string callsExecutedText = "Calls executed";

        const string streamsText = "Streams";
        const string openStreamsText = "Open streams";
        const string evaluationsText = "Evaluations";
        const string updatesSentText = "Updates sent";

        const string networkText = "Network";
        const string readText = "Read";
        const string writtenText = "Written";

        const string noValueText = "-";
        const string limitSlowGameText = "Slow game";
        const string limitBudgetText = "Budget";
        const string limitMaxCallWaitText = "Max. call wait";
        const string limitClientText = "Client";

        const string clearStatisticsText = "Clear statistics";

        // The share of frames the budget stops the server in, past which the budget is the limit
        const float budgetBoundShare = 0.5f;
        // The share of the target frame rate below which the game is slow
        const float slowFrameRateShare = 0.95f;

        protected override void Init ()
        {
            Title = "kRPC Server Info";

            Style.fixedWidth = windowWidth;

            var skin = Skin.DefaultSkin;

            labelStyle = new GUIStyle (skin.label);
            labelStyle.margin = new RectOffset (0, 0, 0, 0);

            nameLabelStyle = new GUIStyle (skin.label);
            nameLabelStyle.margin = new RectOffset (0, 0, 0, 0);
            nameLabelStyle.fixedWidth = nameWidth;

            indentedNameLabelStyle = new GUIStyle (nameLabelStyle);
            indentedNameLabelStyle.padding.left += (int)indentWidth;

            valueLabelStyle = new GUIStyle (skin.label);
            valueLabelStyle.margin = new RectOffset (0, 0, 0, 0);
            valueLabelStyle.fixedWidth = valueWidth;

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
        // The value shown in each row, by label, refreshed every updateTime seconds
        readonly IDictionary<string, string> values = new Dictionary<string, string> ();

        void DrawInfo (string label, string value, bool indented = false)
        {
            if (update || !values.ContainsKey (label))
                values [label] = value;
            GUILayout.BeginHorizontal ();
            GUILayout.Label (label, indented ? indentedNameLabelStyle : nameLabelStyle);
            GUILayout.Label (values [label], valueLabelStyle);
            GUILayout.EndHorizontal ();
        }

        void DrawSection (string title)
        {
            GUILayout.Label (title, labelStyle);
        }

        protected override void Draw (bool needRescale)
        {
            if (needRescale) {
                int scaledFontSize = Style.fontSize;
                Style.fixedWidth = windowWidth * GameSettings.UI_SCALE;
                labelStyle.fontSize = scaledFontSize;
                nameLabelStyle.fontSize = scaledFontSize;
                nameLabelStyle.fixedWidth = nameWidth * GameSettings.UI_SCALE;
                indentedNameLabelStyle.fontSize = scaledFontSize;
                indentedNameLabelStyle.fixedWidth = nameWidth * GameSettings.UI_SCALE;
                indentedNameLabelStyle.padding.left = nameLabelStyle.padding.left + (int)(indentWidth * GameSettings.UI_SCALE);
                valueLabelStyle.fontSize = scaledFontSize;
                valueLabelStyle.fixedWidth = valueWidth * GameSettings.UI_SCALE;
                buttonStyle.fontSize = scaledFontSize;

                // Force window to resize to height of content
                Position = new Rect (Position.x, Position.y, Position.width, 0f);
            }

            update = ((DateTime.Now - lastUpdate).TotalSeconds > updateTime);
            if (update)
                lastUpdate = DateTime.Now;
            var core = Core.Instance;
            var config = Configuration.Instance;

            GUILayout.BeginVertical ();

            DrawSection (frameText);
            DrawInfo (frameRateText, FrameRate (core, config));
            DrawInfo (budgetText, Milliseconds (core.BudgetPerFrame / 1e6f));
            DrawInfo (serverTimeText, Milliseconds (core.TimePerFrame));
            DrawInfo (executingText, Milliseconds (core.ExecTimePerFrame), true);
            DrawInfo (waitingText, Milliseconds (core.WaitTimePerFrame), true);
            DrawInfo (streamTimeText, Milliseconds (core.StreamTimePerFrame), true);

            GUILayoutExtensions.Separator (separatorStyle);

            DrawSection (callsText);
            DrawInfo (callRateText, Count (core.RPCRate) + " calls/s");
            DrawInfo (limitedByText, Limit (core, config));
            var turnaround = core.MedianTurnaround;
            DrawInfo (medianTurnaroundText, turnaround > 0 ? Milliseconds (turnaround) : noValueText);
            DrawInfo (deferredCallsText, Count (core.DeferredCalls));
            DrawInfo (callsExecutedText, Count (core.RPCsExecuted));

            GUILayoutExtensions.Separator (separatorStyle);

            DrawSection (streamsText);
            DrawInfo (openStreamsText, Count (core.Streams));
            DrawInfo (evaluationsText, Count (core.StreamEvaluationRate) + "/s (" + Count (core.StreamEvaluations) + ")");
            DrawInfo (updatesSentText, Count (core.StreamUpdatesSent));

            GUILayoutExtensions.Separator (separatorStyle);

            DrawSection (networkText);
            DrawInfo (readText, BytesToString ((ulong)core.BytesReadRate) + "/s (" + BytesToString (core.BytesRead) + ")");
            DrawInfo (writtenText, BytesToString ((ulong)core.BytesWrittenRate) + "/s (" + BytesToString (core.BytesWritten) + ")");

            GUILayoutExtensions.Separator (separatorStyle);

            if (GUILayout.Button (clearStatisticsText, buttonStyle))
                core.ClearStats ();

            GUILayout.EndVertical ();
            GUI.DragWindow ();
        }

        static string FrameRate (Core core, Configuration config)
        {
            var rate = Count (core.FrameRate) + " fps";
            if (!config.AdaptiveRateControl)
                return rate;
            return rate + " (target " + config.TargetFrameRate.ToString (CultureInfo.CurrentCulture) + ")";
        }

        // What holds the call rate down, by the causes the server performance guide lists
        static string Limit (Core core, Configuration config)
        {
            if (core.RPCRate < 1)
                return noValueText;
            if (config.AdaptiveRateControl && core.BudgetAtFloor &&
                core.FrameRate < config.TargetFrameRate * slowFrameRateShare)
                return limitSlowGameText;
            if (core.BudgetBoundShare > budgetBoundShare)
                return limitBudgetText;
            if (core.MedianTurnaround * 1e6f > config.MaxCallWait)
                return limitMaxCallWaitText;
            return limitClientText;
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
            string[] suffix = { "B", "KB", "MB", "GB", "TB", "PB", "EB" };
            if (bytes == 0)
                return "0 " + suffix [0];
            int place = Convert.ToInt32 (Math.Floor (Math.Log (bytes, 1024)));
            double num = Math.Round (bytes / Math.Pow (1024, place), 1);
            return num.ToString (CultureInfo.CurrentCulture) + " " + suffix [place];
        }
    }
}
