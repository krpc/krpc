#pragma warning disable 1591

using System;
using KRPC.UI;
using UnityEngine;

namespace KRPC.SpaceCenter
{
    /// <summary>
    /// An Apollo-era control-panel style window showing the live state of a vessel's auto-pilot:
    /// backlit annunciator lamps for status, and green digital register readouts for the numeric
    /// attitude error, target, angular rate (measured vs target), inner-loop PID gains and
    /// oscillation suppression values. One instance per vessel, managed by
    /// <see cref="AutoPilotInfoAddon"/>.
    /// </summary>
    public class AutoPilotInfoWindow : Window
    {
        // Apollo caution-and-warning panel colours.
        static readonly Color green = new Color (0.30f, 1.00f, 0.45f);
        static readonly Color amber = new Color (1.00f, 0.72f, 0.10f);
        static readonly Color red = new Color (1.00f, 0.32f, 0.22f);
        // Background of the digital registers, and the unlit lamp colour, so an off lamp matches a
        // register cell (near-black with dim grey text).
        static readonly Color registerBackground = new Color (0.04f, 0.05f, 0.04f);

        const float baseWidth = 250f;

        // Unscaled point sizes of the panel's monospace font: body text (registers, lamps, labels)
        // and the slightly smaller sub-labels (units, column headers).
        const int BodyFontSize = 12;
        const int SmallFontSize = 10;

        public Guid VesselId { get; set; }

        Texture2D lampTexture, registerTexture;
        GUIStyle lampStyle, registerStyle, registerCenterStyle, registerLabelStyle, headerStyle, unitStyle, columnHeadStyle, separatorStyle;
        Font monoFont;

        // Layout options applied to every stretch column (value registers, lamps and column
        // headers) to make the row split into equal columns regardless of the text in each cell.
        // Two options are needed because IMGUI distributes a row's surplus width to each cell in
        // proportion to its (maxWidth - minWidth): pinning only the minimum to a tiny constant is
        // not enough, because the content-derived *maximum* still differs cell to cell, so a cell
        // with wider text (a populated "0.0°" next to a blank one, a "–" sign appearing on a rate,
        // "LPASS", "HOLD", an extra digit) absorbs a larger share of the surplus and ends up wider.
        // Pinning both the minimum to a tiny constant and the maximum to a large equal constant
        // removes the content's influence entirely, so the cells always divide the row evenly while
        // still stretching to fill the fixed-width window.
        GUILayoutOption[] equalColumn;

        static Texture2D FlatTexture (Color color)
        {
            var texture = new Texture2D (1, 1);
            texture.SetPixel (0, 0, color);
            texture.Apply ();
            return texture;
        }

        protected override void Init ()
        {
            Title = DisplayName () + " — AutoPilot";
            Style.fixedWidth = baseWidth;

            var skin = Skin.DefaultSkin;

            lampTexture = FlatTexture (Color.white);
            registerTexture = FlatTexture (registerBackground);

            lampStyle = new GUIStyle (skin.box) {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                margin = new RectOffset (2, 2, 1, 1),
                padding = new RectOffset (4, 4, 4, 4),
                stretchWidth = true
            };
            lampStyle.normal.background = lampTexture;

            registerStyle = new GUIStyle (skin.label) {
                alignment = TextAnchor.MiddleRight,
                fontStyle = FontStyle.Bold,
                margin = new RectOffset (2, 2, 1, 1),
                padding = new RectOffset (6, 6, 4, 4),
                stretchWidth = true
            };
            registerStyle.normal.background = registerTexture;
            registerStyle.normal.textColor = green;

            // Centre-aligned register, for cells whose content reads better centred than
            // right-aligned (the resolved tool name, the percentage readouts).
            registerCenterStyle = new GUIStyle (registerStyle) {
                alignment = TextAnchor.MiddleCenter
            };

            registerLabelStyle = new GUIStyle (skin.label) {
                alignment = TextAnchor.MiddleLeft,
                margin = new RectOffset (2, 0, 1, 1),
                padding = new RectOffset (2, 2, 2, 2),
                fixedWidth = 46f,
                wordWrap = false
            };

            headerStyle = new GUIStyle (skin.label) {
                fontStyle = FontStyle.Bold,
                margin = new RectOffset (0, 0, 4, 1)
            };

            unitStyle = new GUIStyle (skin.label) {
                alignment = TextAnchor.MiddleRight
            };

            // Sub-header that labels the two stretch columns of a register/lamp row. Centered so it
            // reads cleanly over both the right-aligned numeric registers and the centered lamps.
            columnHeadStyle = new GUIStyle (skin.label) {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                margin = new RectOffset (2, 2, 3, 3),
                padding = new RectOffset (4, 4, 0, 0),
                stretchWidth = true
            };

            separatorStyle = GUILayoutExtensions.SeparatorStyle (new Color (0f, 0f, 0f, 0.4f));
            separatorStyle.fixedHeight = 2;
            separatorStyle.stretchWidth = true;
            separatorStyle.margin = new RectOffset (2, 2, 3, 3);

            // The max is any large constant well above any realistic column width; only its being
            // equal across cells matters, so the surplus splits evenly (see equalColumn comment).
            equalColumn = new[] { GUILayout.MinWidth (1f), GUILayout.MaxWidth (10000f) };

            // Use a fixed-width (monospace) font across the whole panel so the digital readouts and
            // columns line up. Several common OS monospace fonts are listed so it resolves on both
            // Windows and Linux. The title bar is unaffected: it is drawn with the window Style, which
            // keeps the stock font.
            monoFont = Font.CreateDynamicFontFromOSFont (
                new[] { "Consolas", "Courier New", "DejaVu Sans Mono", "Liberation Mono", "monospace" },
                (int) (BodyFontSize * GameSettings.UI_SCALE));
            foreach (var style in new[] {
                lampStyle, registerStyle, registerCenterStyle, registerLabelStyle, headerStyle,
                unitStyle, columnHeadStyle })
                style.font = monoFont;
            ApplyFontSizes (GameSettings.UI_SCALE);
        }

        // Applies the scaled monospace point sizes to the panel's text styles.
        void ApplyFontSizes (float scale)
        {
            int body = (int) (BodyFontSize * scale);
            int small = (int) (SmallFontSize * scale);
            lampStyle.fontSize = body;
            registerStyle.fontSize = body;
            registerCenterStyle.fontSize = body;
            registerLabelStyle.fontSize = body;
            headerStyle.fontSize = body;
            unitStyle.fontSize = small;
            columnHeadStyle.fontSize = small;
        }

        string DisplayName ()
        {
            try {
                return FlightGlobalsExtensions.GetVesselById (VesselId).GetDisplayName ();
            } catch (ArgumentException) {
                return "Vessel";
            }
        }

        protected override void Draw (bool needRescale)
        {
            if (needRescale) {
                float scale = GameSettings.UI_SCALE;
                Style.fixedWidth = baseWidth * scale;
                registerLabelStyle.fixedWidth = 46f * scale;
                ApplyFontSizes (scale);
                // Force the window to shrink back to its content height.
                Position = new Rect (Position.x, Position.y, Position.width, 0f);
            }

            var ap = Services.AutoPilot.GetEngaged (VesselId);
            bool engaged = ap != null;

            GUILayout.BeginVertical ();

            // Engagement annunciator lamp.
            GUILayout.BeginHorizontal ();
            Lamp ("ENGAGED", engaged, green);
            GUILayout.EndHorizontal ();

            // Target: axis column headers, then the current target (CUR, what the auto-pilot is
            // tracking right now) and below it the commanded target (CMD, what was requested). CMD is
            // shown only while smoothing is active (TargetSmoothingTime > 0); without smoothing CUR
            // always equals the command, so the whole CMD row is blanked. RLL is blank when no
            // specific roll is held. Values carry a degree symbol, so the section needs no unit label.
            bool showCmd = engaged && ap.TargetSmoothingTime > 0;
            Header ("TARGET", null);
            AxisColumnHeader ("PCH", "HDG", "RLL");
            GUILayout.BeginHorizontal ();
            GUILayout.Label ("CUR", registerLabelStyle);
            Register (engaged ? Deg (ap.CurrentTargetPitch) : Blank);
            Register (engaged ? Deg (ap.CurrentTargetHeading) : Blank);
            Register (engaged ? DegOrBlank (ap.CurrentTargetRoll) : Blank);
            GUILayout.EndHorizontal ();
            GUILayout.BeginHorizontal ();
            GUILayout.Label ("CMD", registerLabelStyle);
            Register (showCmd ? Deg (ap.TargetPitch) : Blank);
            Register (showCmd ? Deg (ap.TargetHeading) : Blank);
            Register (showCmd ? DegOrBlank (ap.TargetRoll) : Blank);
            GUILayout.EndHorizontal ();

            // Target angular rate: axis column headers, then the current (measured) and target rate
            // rows. CUR is the raw (roll-invariant frame) rate; both are rad/s in the controller's
            // internal sign convention, so a settled axis shows the CUR row tracking the TGT row.
            var rateMea = engaged ? ap.MeasuredAngularVelocity : null;
            var rateTgt = engaged ? ap.TargetAngularVelocity : null;
            Header ("TARGET RATE", "RAD/S");
            AxisColumnHeader ("PCH", "YAW", "RLL");
            GUILayout.BeginHorizontal ();
            GUILayout.Label ("CUR", registerLabelStyle);
            Register (rateMea == null ? Blank : Rate (rateMea.Item1));
            Register (rateMea == null ? Blank : Rate (rateMea.Item3));
            Register (rateMea == null ? Blank : Rate (rateMea.Item2));
            GUILayout.EndHorizontal ();
            GUILayout.BeginHorizontal ();
            GUILayout.Label ("TGT", registerLabelStyle);
            Register (rateTgt == null ? Blank : Rate (rateTgt.Item1));
            Register (rateTgt == null ? Blank : Rate (rateTgt.Item3));
            Register (rateTgt == null ? Blank : Rate (rateTgt.Item2));
            GUILayout.EndHorizontal ();

            // Attitude error: the four axis column headers on one row, error lamps on the next, each
            // green within the AutoPilot.Wait() stopping angle threshold and amber outside it. These
            // are errors to the CURRENT (tracked) target, not the commanded one, so they stay small
            // while a smoothed change is being slewed in (the craft is tracking CUR, not CMD).
            Header ("ATTITUDE ERROR", null);
            GUILayout.BeginHorizontal ();
            ColumnHead ("TOT");
            ColumnHead ("PCH");
            ColumnHead ("HDG");
            ColumnHead ("RLL");
            GUILayout.EndHorizontal ();
            // CurrentRollError throws when roll is not held, so only read it when it is (mirrors the
            // CurrentTargetRoll NaN guard the other roll readouts use).
            float errorThreshold = engaged ? ap.StoppingAngleThreshold : 0f;
            bool rollShown = engaged && !float.IsNaN (ap.CurrentTargetRoll);
            GUILayout.BeginHorizontal ();
            ErrorLamp (engaged, engaged ? ap.CurrentError : 0f, errorThreshold);
            ErrorLamp (engaged, engaged ? ap.CurrentPitchError : 0f, errorThreshold);
            ErrorLamp (engaged, engaged ? ap.CurrentHeadingError : 0f, errorThreshold);
            ErrorLamp (rollShown, rollShown ? ap.CurrentRollError : 0f, errorThreshold);
            GUILayout.EndHorizontal ();

            // Inner-loop PID gains (autotuned each tick when AutoTune is on): axis column headers,
            // then a Kp and a Ki row.
            var gainP = engaged ? ap.PitchPIDGains : null;
            var gainR = engaged ? ap.RollPIDGains : null;
            var gainY = engaged ? ap.YawPIDGains : null;
            Header ("PID GAIN", null);
            AxisColumnHeader ("PCH", "YAW", "RLL");
            GUILayout.BeginHorizontal ();
            GUILayout.Label ("Kp", registerLabelStyle);
            Register (gainP == null ? Blank : Gain (gainP.Item1));
            Register (gainY == null ? Blank : Gain (gainY.Item1));
            Register (gainR == null ? Blank : Gain (gainR.Item1));
            GUILayout.EndHorizontal ();
            GUILayout.BeginHorizontal ();
            GUILayout.Label ("Ki", registerLabelStyle);
            Register (gainP == null ? Blank : Gain (gainP.Item2));
            Register (gainY == null ? Blank : Gain (gainY.Item2));
            Register (gainR == null ? Blank : Gain (gainR.Item2));
            GUILayout.EndHorizontal ();

            // Oscillation suppression. Two distinct oscillation measurements head the section: STRC
            // is the structural-wobble level the detector reads off the measured angular rate
            // (per-axis: PCH/YAW/RLL column header), CTRL is the limit-cycle amplitude measured on the
            // control output (per axis-group: PCH/YAW and RLL column header, like the rows below it).
            Header ("OSCILLATION", null);
            var level = engaged ? ap.OscillationLevel : null;
            // Beyond reach of a level of 0 when disengaged, so the lamp stays green while not engaged.
            double latchThreshold = engaged ? ap.OscillationLatchThreshold : double.PositiveInfinity;
            AxisColumnHeader ("PCH", "YAW", "RLL");
            GUILayout.BeginHorizontal ();
            GUILayout.Label ("STRC", registerLabelStyle);
            LevelLamp (engaged, level == null ? 0 : level.Item1, engaged && ap.PitchYawOscillationLatched, latchThreshold);
            LevelLamp (engaged, level == null ? 0 : level.Item3, engaged && ap.PitchYawOscillationLatched, latchThreshold);
            LevelLamp (engaged, level == null ? 0 : level.Item2, engaged && ap.RollOscillationLatched, latchThreshold);
            GUILayout.EndHorizontal ();

            TwoColumnHeader ("PCH/YAW", "RLL");

            // Control-output oscillation envelope per group: the about-mean amplitude of the delivered
            // command that drives the automatic mitigation, drawn as a lamp matching STRC and lit amber
            // at/above the engage threshold.
            var oscThreshold = engaged ? ap.OscillationControlThreshold : double.PositiveInfinity;
            GUILayout.BeginHorizontal ();
            GUILayout.Label ("CTRL", registerLabelStyle);
            OscCell (engaged, engaged ? ap.PitchYawControlOscillation : 0.0, oscThreshold);
            OscCell (engaged, engaged ? ap.RollControlOscillation : 0.0, oscThreshold);
            GUILayout.EndHorizontal ();

            // Hold-gate level: how fully the latched flexible-craft hold mitigation (bandwidth floor
            // + feedforward cut) is engaged on the pitch/yaw group, 0 while slewing to 1 once settled.
            // Only the pitch/yaw group is shown; the RLL cell is intentionally left blank.
            var mitigation = engaged ? ap.MitigationLevel : null;
            GUILayout.BeginHorizontal ();
            GUILayout.Label ("HOLD", registerLabelStyle);
            RegisterCentered (mitigation == null ? Blank : Percent (mitigation.Item1));
            RegisterCentered (Blank);
            GUILayout.EndHorizontal ();

            GUILayout.BeginHorizontal ();
            GUILayout.Label ("FREQ", registerLabelStyle);
            FreqLamp (engaged, engaged ? ap.PitchYawOscillationDetectedFrequency : double.NaN);
            FreqLamp (engaged, engaged ? ap.RollOscillationDetectedFrequency : double.NaN);
            GUILayout.EndHorizontal ();

            // Oscillation-detected annunciators (whether each group has latched as confirmed
            // structurally flexible): a "LATCH" lamp, lit amber once latched and dim otherwise.
            GUILayout.BeginHorizontal ();
            GUILayout.Label ("OSC", registerLabelStyle);
            Lamp ("LATCH", engaged && ap.PitchYawOscillationLatched, amber);
            Lamp ("LATCH", engaged && ap.RollOscillationLatched, amber);
            GUILayout.EndHorizontal ();

            GUILayout.BeginHorizontal ();
            GUILayout.Label ("MODE", registerLabelStyle);
            ModeLamp (engaged, engaged ? ap.PitchYawOscillationControl : default (Services.OscillationControl));
            ModeLamp (engaged, engaged ? ap.RollOscillationControl : default (Services.OscillationControl));
            GUILayout.EndHorizontal ();

            // Resolved suppression tool: what Automatic actually selected (the MODE lamp above shows
            // the commanded mode, not the tool it routed to). Pitch (0) and yaw (2) share the group.
            GUILayout.BeginHorizontal ();
            GUILayout.Label ("FILT", registerLabelStyle);
            RegisterCentered (engaged ? ToolName (ap.ActiveSuppressionTool (0)) : Blank);
            RegisterCentered (engaged ? ToolName (ap.ActiveSuppressionTool (1)) : Blank);
            GUILayout.EndHorizontal ();

            GUILayout.EndVertical ();

            GUI.DragWindow ();
        }

        // Draws one square backlit annunciator lamp. When unlit, the lamp shows the near-black
        // register background with faint grey "engraved" text; when lit, the full colour with black
        // text. The lamp is purely informational (drawn as a box, never interactive).
        void Lamp (string label, bool lit, Color colour)
        {
            var prevBg = GUI.backgroundColor;
            GUI.backgroundColor = lit ? colour : registerBackground;
            lampStyle.normal.textColor = lit ? Color.black : new Color (0.55f, 0.55f, 0.55f);
            // A box with no measurable content does not stretch to fill its column and picks up a
            // slightly different line height, so a blank lamp (e.g. RLL when no roll is held) would
            // render narrower and taller than its lit neighbours. A plain space does not fix this:
            // IMGUI ignores trailing whitespace when measuring width, so it still measures as empty.
            // A non-breaking space is measured as a real glyph, keeping the box geometry identical to
            // a populated cell while still reading as blank.
            GUILayout.Box (string.IsNullOrEmpty (label) ? NonBreakingSpace : label, lampStyle, equalColumn);
            GUI.backgroundColor = prevBg;
        }

        // One control-oscillation envelope lamp (matching the structural LevelLamp): lit amber
        // at/above the engage threshold, green below, dim when disengaged.
        void OscCell (bool engaged, double level, double threshold)
        {
            Color colour = engaged && level >= threshold ? amber : green;
            Lamp (Percent (level), engaged, colour);
        }

        // Attitude-error lamp: green when the axis error is within the AutoPilot.Wait() stopping
        // angle threshold, amber when outside it. Dim/blank when not shown (disengaged, or roll on an
        // axis whose roll is not held).
        void ErrorLamp (bool show, float error, float threshold)
        {
            Lamp (show ? Deg (error) : Blank, show, Math.Abs (error) <= threshold ? green : amber);
        }

        // Frequency lamp: green showing the detected structural frequency once the tracker has a
        // lock, amber "NO LOCK" until then (and dim when disengaged).
        void FreqLamp (bool engaged, double freq)
        {
            bool locked = !double.IsNaN (freq);
            Lamp (locked ? string.Format ("{0:F2} Hz", freq) : "NO LOCK", engaged, locked ? green : amber);
        }

        // The axis is identified by the PCH/YAW/RLL column header, so the lamp shows only the level value.
        void LevelLamp (bool engaged, double level, bool latched, double latchThreshold)
        {
            // Amber once the level reaches the controller's latch threshold (so it is in the range
            // that triggers the latch), or the axis has already latched; green below. Lamps stay lit
            // (status colour) whenever engaged and dim when disengaged.
            Color colour = latched || level >= latchThreshold ? amber : green;
            Lamp (Percent (level), engaged, colour);
        }

        // Mode lamp. The axis group is identified by the PCH/YAW and RLL column header, so the lamp
        // shows only the mode name.
        void ModeLamp (bool engaged, Services.OscillationControl mode)
        {
            Color colour = mode == Services.OscillationControl.Off ? red
                : mode == Services.OscillationControl.Automatic ? green
                : amber;
            Lamp (ModeName (mode), engaged, colour);
        }

        static string ModeName (Services.OscillationControl mode)
        {
            switch (mode) {
            case Services.OscillationControl.Automatic:
                return "AUTO";
            case Services.OscillationControl.Off:
                return "OFF";
            case Services.OscillationControl.Notch:
                return "NOTCH";
            case Services.OscillationControl.LowPass:
                return "LOWPASS";
            default:
                return mode.ToString ().ToUpperInvariant ();
            }
        }

        void Header (string text, string unit)
        {
            GUILayoutExtensions.Separator (separatorStyle);
            if (unit == null) {
                GUILayout.Label (text, headerStyle);
            } else {
                GUILayout.BeginHorizontal ();
                GUILayout.Label (text, headerStyle);
                GUILayout.Label (unit, unitStyle);
                GUILayout.EndHorizontal ();
            }
        }

        void RegisterRow (string label, string value)
        {
            GUILayout.BeginHorizontal ();
            GUILayout.Label (label, registerLabelStyle);
            Register (value);
            GUILayout.EndHorizontal ();
        }

        // Draws one green digital-register value cell as an equal-width stretch column.
        void Register (string value)
        {
            GUILayout.Label (value, registerStyle, equalColumn);
        }

        // As Register, but the content is centred rather than right-aligned.
        void RegisterCentered (string value)
        {
            GUILayout.Label (value, registerCenterStyle, equalColumn);
        }

        // Draws one centered column-header cell as an equal-width stretch column, matching the
        // value/lamp columns drawn below it.
        void ColumnHead (string text)
        {
            GUILayout.Label (text, columnHeadStyle, equalColumn);
        }

        // Empty spacer matching the row-label column width, but drawn with the small column-header
        // style so it does not inflate the header row to body-font height (which would leave an
        // oversized gap below the small header text). The width matches registerLabelStyle so the
        // axis columns still line up with the value rows below.
        void ColumnSpacer ()
        {
            GUILayout.Label (string.Empty, columnHeadStyle, GUILayout.Width (registerLabelStyle.fixedWidth));
        }

        // A sub-header labelling two stretch columns (e.g. Kp/Ki, or the PCH/YAW and RLL axis groups).
        void TwoColumnHeader (string left, string right)
        {
            GUILayout.BeginHorizontal ();
            ColumnSpacer ();
            ColumnHead (left);
            ColumnHead (right);
            GUILayout.EndHorizontal ();
        }

        // A sub-header labelling three axis columns over a leading row-label column.
        void AxisColumnHeader (string a, string b, string c)
        {
            GUILayout.BeginHorizontal ();
            ColumnSpacer ();
            ColumnHead (a);
            ColumnHead (b);
            ColumnHead (c);
            GUILayout.EndHorizontal ();
        }

        // Rounds to the displayed number of decimals and normalises negative zero to positive zero,
        // so a value dithering either side of zero settles on a single displayed form (e.g. "+0.000",
        // "0.0°") instead of flickering its sign between frames.
        static double Stable (double value, int decimals)
        {
            double rounded = Math.Round (value, decimals);
            return rounded == 0.0 ? 0.0 : rounded;
        }

        // Always carries an explicit leading sign (positive and zero show "+", negative "-") so the
        // sign never appears or disappears between frames.
        static string Rate (double value)
        {
            return string.Format ("{0:+0.000;-0.000;+0.000}", Stable (value, 3));
        }

        static string Gain (double value)
        {
            return string.Format ("{0:0.0}", Stable (value, 1));
        }

        // A 0–1 fraction (oscillation level, control-output envelope, hold-mitigation weight) shown
        // as a whole-number percentage. The number is left-padded to three figure-spaces (U+2007,
        // digit-width) so the trailing "%" stays in a fixed position as the value gains or loses a
        // digit (e.g. 10% -> 9%) even when the cell is centre-aligned.
        static string Percent (double value)
        {
            return ((int) Math.Round (value * 100.0)).ToString ().PadLeft (3, ' ') + "%";
        }

        static string ToolName (int tool)
        {
            switch (tool) {
            case 1:
                return "NOTCH";
            case 2:
                return "LOWPASS";
            default:
                return Blank;
            }
        }

        // Empty cells are drawn blank (no placeholder glyphs) — less visually distracting than a
        // dashed filler in every disengaged/unset readout.
        const string Blank = "";

        // U+00A0 non-breaking space. Used as the content of a blank lamp box: a plain space is
        // ignored by IMGUI's width measurement (trailing whitespace is trimmed), so the box would
        // not stretch to fill its column, but a non-breaking space measures as a real glyph.
        const string NonBreakingSpace = " ";

        // An angle readout that is blank for an unset (NaN) roll target, otherwise the degrees.
        static string DegOrBlank (float value)
        {
            return float.IsNaN (value) ? Blank : Deg (value);
        }

        static string Deg (float value)
        {
            return string.Format ("{0:F1}°", Stable (value, 1));
        }

        public void OnDestroy ()
        {
            if (lampTexture != null)
                Destroy (lampTexture);
            if (registerTexture != null)
                Destroy (registerTexture);
            if (monoFont != null)
                Destroy (monoFont);
        }
    }
}
