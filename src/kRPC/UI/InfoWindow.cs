using System;
using UnityEngine;
using System.Collections.Generic;

namespace KRPC.UI
{
    sealed class InfoWindow : Window
    {
        const float windowWidth = 300f;
        GUIStyle labelStyle, nameLabelStyle, valueLabelStyle, separatorStyle;

        const string networkInfoText = "Network Info";
        const string bytesReadText = "Data read";
        const string bytesWrittenText = "Data written";
        const string bytesReadRateText = "Data read rate";
        const string bytesWrittenRateText = "Data written rate";

        const string rpcInfoText = "RPC Execution Info";
        const string rpcsExecutedText = "RPCs Executed";
        const string rpcRateText = "RPC Rate";
        const string rpcExecutionMode = "Execution mode";
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

        public KRPCServer Server { private get; set; }

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

            if (Server.Running) {
                GUILayout.Label (networkInfoText, labelStyle);
                DrawInfo (bytesReadText, BytesToString (Server.BytesRead));
                DrawInfo (bytesWrittenText, BytesToString (Server.BytesWritten));
                DrawInfo (bytesReadRateText, BytesToString ((long)Server.BytesReadRate) + "/s");
                DrawInfo (bytesWrittenRateText, BytesToString ((long)Server.BytesWrittenRate) + "/s");

                GUILayoutExtensions.Separator (separatorStyle);

                GUILayout.Label (rpcInfoText, labelStyle);
                DrawInfo (rpcsExecutedText, Server.RPCsExecuted.ToString ());
                DrawInfo (rpcRateText, Math.Round (Server.RPCRate) + " RPC/s");
                DrawInfo (rpcExecutionMode, Server.AdaptiveRateControl ? adaptiveModeText : staticModeText);
                DrawInfo (maxTimePerUpdateText, Server.MaxTimePerUpdate + " ns");
                DrawInfo (rpcReceiveModeText, Server.BlockingRecv ? blockingModeText : nonBlockingModeText);
                DrawInfo (recvTimeoutText, Server.BlockingRecv ? Server.RecvTimeout + " ns" : notApplicableText);
                DrawInfo (timePerRPCUpdateText, String.Format ("{0:F5} s", Server.TimePerRPCUpdate));
                DrawInfo (pollTimePerRPCUpdateText, String.Format ("{0:F5} s", Server.PollTimePerRPCUpdate));
                DrawInfo (execTimePerRPCUpdateText, String.Format ("{0:F5} s", Server.ExecTimePerRPCUpdate));

                GUILayoutExtensions.Separator (separatorStyle);

                GUILayout.Label (streamInfoText, labelStyle);
                DrawInfo (streamingRPCsText, Server.StreamRPCs.ToString ());
                DrawInfo (streamingRPCsExecutedText, Server.StreamRPCsExecuted.ToString ());
                DrawInfo (streamingRPCRateText, Math.Round (Server.StreamRPCRate) + " RPC/s");
                DrawInfo (timePerStreamUpdateText, String.Format ("{0:F5} s", Server.TimePerStreamUpdate));
            } else {
                GUILayout.Label ("Server not running");
            }
            GUILayout.EndVertical ();
            GUI.DragWindow ();
        }

        static String BytesToString (long byteCount)
        {
            string[] suf = { "B", "KB", "MB", "GB", "TB", "PB", "EB" }; //Longs run out around EB
            if (byteCount == 0)
                return "0" + suf [0];
            long bytes = Math.Abs (byteCount);
            int place = Convert.ToInt32 (Math.Floor (Math.Log (bytes, 1024)));
            double num = Math.Round (bytes / Math.Pow (1024, place), 1);
            return (Math.Sign (byteCount) * num) + suf [place];
        }
    }
}

