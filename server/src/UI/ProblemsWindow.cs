using System.Collections.Generic;
using KRPC.Service;
using UnityEngine;

namespace KRPC.UI
{
    /// <summary>
    /// The problems found when loading the services, in categories that expand when clicked.
    /// </summary>
    sealed class ProblemsWindow : Window
    {
        const float windowWidth = 500f;
        const float listHeight = 300f;
        const float indentWidth = 20f;

        const string logText = "More detail in ";
        const string closeText = "Close";
        const string hideText = "Don't show again (until problems change)";

        GUIStyle labelStyle, categoryStyle, lineStyle, buttonStyle;
        Vector2 scroll;
        readonly HashSet<string> expanded = new HashSet<string> ();
        ConfigurationFile config;

        protected override void Init ()
        {
            Title = "kRPC Problems";
            config = ConfigurationFile.Instance;
            Style.fixedWidth = windowWidth;
            // The skin's window background is translucent, which makes a long list hard to read
            var background = new Texture2D (1, 1);
            background.SetPixel (0, 0, new Color (0.12f, 0.13f, 0.14f, 1f));
            background.Apply ();
            Style.normal.background = background;
            Style.onNormal.background = background;
            Position = new Rect ((Screen.width - windowWidth) / 2, Screen.height / 4, windowWidth, 0f);

            var skin = Skin.DefaultSkin;
            labelStyle = new GUIStyle (skin.label);
            labelStyle.margin = new RectOffset (0, 0, 0, 0);
            labelStyle.wordWrap = true;

            categoryStyle = new GUIStyle (skin.button);
            categoryStyle.margin = new RectOffset (0, 0, 2, 2);
            categoryStyle.alignment = TextAnchor.MiddleLeft;

            lineStyle = new GUIStyle (labelStyle);
            lineStyle.margin = new RectOffset ((int)indentWidth, 0, 0, 2);

            buttonStyle = new GUIStyle (skin.button);
            buttonStyle.margin = new RectOffset (0, 4, 4, 0);

            // A single category opens expanded
            var problems = ServiceProblems.Categories;
            if (problems.Count == 1)
                expanded.Add (problems [0].Title);
        }

        protected override void Draw (bool needRescale)
        {
            if (needRescale) {
                int scaledFontSize = Style.fontSize;
                Style.fixedWidth = windowWidth * GameSettings.UI_SCALE;
                labelStyle.fontSize = scaledFontSize;
                categoryStyle.fontSize = scaledFontSize;
                lineStyle.fontSize = scaledFontSize;
                lineStyle.margin.left = (int)(indentWidth * GameSettings.UI_SCALE);
                buttonStyle.fontSize = scaledFontSize;
                Position = new Rect (Position.x, Position.y, Position.width, 0f);
            }

            GUILayout.BeginVertical ();
            scroll = GUILayout.BeginScrollView (scroll, GUILayout.Height (listHeight * GameSettings.UI_SCALE));
            foreach (var category in ServiceProblems.Categories) {
                var open = expanded.Contains (category.Title);
                var label = (open ? "- " : "+ ") + category.Title + " (" + category.Lines.Count + ")";
                if (GUILayout.Button (label, categoryStyle)) {
                    if (open)
                        expanded.Remove (category.Title);
                    else
                        expanded.Add (category.Title);
                }
                if (open) {
                    foreach (var line in category.Lines)
                        GUILayout.Label (line, lineStyle);
                }
            }
            GUILayout.EndScrollView ();
            GUILayout.Label (logText + System.IO.Path.GetFullPath (KSPUtil.ApplicationRootPath + "KSP.log"), labelStyle);
            DrawButtons ();
            GUILayout.EndVertical ();
            GUI.DragWindow ();
        }

        void DrawButtons ()
        {
            GUILayout.BeginHorizontal ();
            // Close is on the right, where a player who clicks without reading clicks
            if (GUILayout.Button (hideText, buttonStyle)) {
                config.Load ();
                config.Configuration.HiddenServiceProblems = ServiceProblems.Digest ?? string.Empty;
                config.Save ();
                Visible = false;
            }
            if (GUILayout.Button (closeText, buttonStyle))
                Visible = false;
            GUILayout.EndHorizontal ();
        }
    }
}
