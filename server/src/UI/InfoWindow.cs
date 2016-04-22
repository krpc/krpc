using System;
using UnityEngine;
using System.Collections.Generic;

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
        const string timePerRPCUpdateText = "Time per update";
        const string pollTimePerRPCUpdateText = "Poll time per update";
        const string execTimePerRPCUpdateText = "Exec time per update";

        const string streamInfoText = "Stream Execution Info";
        const string streamingRPCsText = "Current Streams";
        const string streamingRPCsExecutedText = "Stream RPCs Executed";
        const string streamingRPCRateText = "Stream RPC Rate";
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
        IDictionary<string, string> values = new Dictionary<string, string> ();

        void DrawInfo (string label, string value)
        {
            if (update || !values.ContainsKey (label)) {
                values [label] = value;
            }
            GUILayout.BeginHorizontal ();
            GUILayout.Label (label, nameLabelStyle);
            GUILayout.Label (values [label], valueLabelStyle);
            GUILayout.EndHorizontal ();
        }

        protected override void Draw ()
        {
            update = ((DateTime.Now - lastUpdate).TotalSeconds > updateTime);
            if (update)
                lastUpdate = DateTime.Now;

            GUILayout.BeginVertical ();

            GUILayout.Label (networkInfoText, labelStyle);
            DrawInfo (bytesReadText, BytesToString (KRPCCore.Instance.BytesRead));
            DrawInfo (bytesWrittenText, BytesToString (KRPCCore.Instance.BytesWritten));
            DrawInfo (bytesReadRateText, BytesToString ((ulong)KRPCCore.Instance.BytesReadRate) + "/s");
            DrawInfo (bytesWrittenRateText, BytesToString ((ulong)KRPCCore.Instance.BytesWrittenRate) + "/s");

            GUILayoutExtensions.Separator (separatorStyle);

            GUILayout.Label (rpcInfoText, labelStyle);
            DrawInfo (rpcsExecutedText, KRPCCore.Instance.RPCsExecuted.ToString ());
            DrawInfo (rpcRateText, Math.Round (KRPCCore.Instance.RPCRate) + " RPC/s");
            DrawInfo (rpcExecutionMode, KRPCCore.Instance.OneRPCPerUpdate ? singleRPCModeText : (KRPCCore.Instance.AdaptiveRateControl ? adaptiveModeText : staticModeText));
            DrawInfo (maxTimePerUpdateText, KRPCCore.Instance.OneRPCPerUpdate ? notApplicableText : KRPCCore.Instance.MaxTimePerUpdate + " ns");
            DrawInfo (rpcReceiveModeText, KRPCCore.Instance.BlockingRecv ? blockingModeText : nonBlockingModeText);
            DrawInfo (recvTimeoutText, KRPCCore.Instance.BlockingRecv ? KRPCCore.Instance.RecvTimeout + " ns" : notApplicableText);
            DrawInfo (timePerRPCUpdateText, String.Format ("{0:F5} s", KRPCCore.Instance.TimePerRPCUpdate));
            DrawInfo (pollTimePerRPCUpdateText, String.Format ("{0:F5} s", KRPCCore.Instance.PollTimePerRPCUpdate));
            DrawInfo (execTimePerRPCUpdateText, String.Format ("{0:F5} s", KRPCCore.Instance.ExecTimePerRPCUpdate));

            GUILayoutExtensions.Separator (separatorStyle);

            GUILayout.Label (streamInfoText, labelStyle);
            DrawInfo (streamingRPCsText, KRPCCore.Instance.StreamRPCs.ToString ());
            DrawInfo (streamingRPCsExecutedText, KRPCCore.Instance.StreamRPCsExecuted.ToString ());
            DrawInfo (streamingRPCRateText, Math.Round (KRPCCore.Instance.StreamRPCRate) + " RPC/s");
            DrawInfo (timePerStreamUpdateText, String.Format ("{0:F5} s", KRPCCore.Instance.TimePerStreamUpdate));

            GUILayoutExtensions.Separator (separatorStyle);

            GUILayout.BeginHorizontal ();
            if (GUILayout.Button (clearStatisticsText, buttonStyle)) {
                KRPCCore.Instance.ClearStats ();
            }
            GUILayout.EndHorizontal ();

            GUILayout.EndVertical ();
            GUI.DragWindow ();
        }

        static String BytesToString (ulong bytes)
        {
            string[] suffix = { "B", "KB", "MB", "GB", "TB", "PB", "EB" };
            if (bytes == 0)
                return "0" + suffix [0];
            int place = Convert.ToInt32 (Math.Floor (Math.Log (bytes, 1024)));
            double num = Math.Round (bytes / Math.Pow (1024, place), 1);
            return num + suffix [place];
        }
    }
}

