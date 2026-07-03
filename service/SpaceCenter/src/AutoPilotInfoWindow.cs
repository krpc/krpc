#pragma warning disable 1591

using System;
using KRPC.UI;
using UnityEngine;

namespace KRPC.SpaceCenter
{
    /// <summary>
    /// An Apollo-era control-panel style window showing the live state of a vessel's auto-pilot:
    /// backlit annunciator lamps for status (dim when nominal, amber to flag a problem), and
    /// digital register readouts for the numeric target, attitude error, inner-loop PID gains and
    /// oscillation detector/gate/mitigation values. One instance per vessel, managed by
    /// <see cref="AutoPilotInfoAddon"/>.
    /// </summary>
    public class AutoPilotInfoWindow : Window
    {
        // Panel colours, matched to the stock navball's speed display: the register text and the
        // ENGAGED lamp use the navball's readout green, amber is the caution colour.
        static readonly Color green = new Color (0.00f, 1.00f, 0.00f);
        static readonly Color amber = new Color (1.00f, 0.72f, 0.10f);
        // Background of the digital registers, and the unlit lamp colour, so an off lamp matches a
        // register cell — the dark grey behind the navball's speed text (#3A3A3F).
        static readonly Color registerBackground = new Color (0.227f, 0.227f, 0.247f);
        // Text colour of an unlit status lamp (and of a data lamp that is not live): faint grey
        // "engraved" text on the dark lamp background.
        static readonly Color engravedGrey = new Color (0.55f, 0.55f, 0.55f);

        const float baseWidth = 250f;

        // Unscaled point sizes of the panel's monospace font: body text (registers, lamps, labels)
        // and the slightly smaller sub-labels (units, column headers).
        const int BodyFontSize = 12;
        const int SmallFontSize = 10;

        public Guid VesselId { get; set; }

        Texture2D lampTexture, registerTexture, tooltipTexture;
        GUIStyle lampStyle, registerStyle, registerCenterStyle, registerLabelStyle, headerStyle, unitStyle, columnHeadStyle, separatorStyle, tooltipStyle;
        Font monoFont;

        // Description of the row currently under the mouse (or null), and that row's rect, which
        // the tooltip is anchored to — a row's tooltip always appears in the same place, rather
        // than following the mouse around. Captured by RowTooltip as the rows are drawn each
        // Repaint pass and rendered by DrawTooltip at the end of the pass, so the tooltip box
        // draws on top of the panel.
        string activeTooltip;
        Rect activeTooltipRow;

        // The panel body is a grid of 4 equal-width columns filling the window: every cell is
        // fixed to a whole number of columns (spanOptions [n] sizes an n-column cell), so cells
        // line up vertically across rows regardless of how many cells each row has — a per-axis
        // row is label + 3 values, a PCH/YAW+RLL group row is label + a 2-column cell + 1, the
        // engagement lamp spans all 4. Fixed widths (rather than stretch weights) are needed
        // because IMGUI distributes surplus width in proportion to each cell's content-derived
        // size, which would let wider text steal width and break the vertical alignment.
        // Rebuilt by ComputeGrid on rescale.
        GUILayoutOption[][] spanOptions;

        // The horizontal gap IMGUI leaves between adjacent cells and at the row edges: the cells'
        // 2px side margins, adjacent margins collapsing to the larger of the two. Unscaled, like
        // the style margins it mirrors.
        const float cellSpacing = 2f;

        static Texture2D FlatTexture (Color color)
        {
            var texture = new Texture2D (1, 1);
            texture.SetPixel (0, 0, color);
            texture.Apply ();
            return texture;
        }

        // A 3×3 texture with a 1px border colour around a fill colour. Drawn with a style whose
        // border is 1px, the edge pixels become a crisp 1px outline at any box size (point
        // filtering stops the border bleeding into the fill when stretched).
        static Texture2D BorderedTexture (Color border, Color fill)
        {
            var texture = new Texture2D (3, 3);
            for (int x = 0; x < 3; x++)
                for (int y = 0; y < 3; y++)
                    texture.SetPixel (x, y, x == 1 && y == 1 ? fill : border);
            texture.filterMode = FilterMode.Point;
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
            // Darker than the registers so the floating tooltip reads as a layer above them, with
            // a grey outline to separate it from whatever it happens to overlap.
            tooltipTexture = BorderedTexture (new Color (0.55f, 0.55f, 0.55f), new Color (0.10f, 0.10f, 0.12f));

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
            // Registers carry data, not status, so they read in plain white — status is the
            // lamps' job.
            registerStyle.normal.textColor = Color.white;

            // Centre-aligned register, for cells whose content reads better centred than
            // right-aligned (the resolved tool name, the percentage readouts).
            registerCenterStyle = new GUIStyle (registerStyle) {
                alignment = TextAnchor.MiddleCenter
            };

            registerLabelStyle = new GUIStyle (skin.label) {
                alignment = TextAnchor.MiddleLeft,
                margin = new RectOffset (2, 0, 1, 1),
                padding = new RectOffset (2, 2, 2, 2),
                // fixedWidth (one grid column) is set by ComputeGrid.
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

            // The tooltip keeps the skin's default (proportional) font: it is prose, not a
            // register readout, and the proportional face also visually separates it from the
            // panel it floats over.
            tooltipStyle = new GUIStyle (skin.label) {
                wordWrap = true,
                border = new RectOffset (1, 1, 1, 1),
                padding = new RectOffset (6, 6, 4, 4)
            };
            tooltipStyle.normal.background = tooltipTexture;
            tooltipStyle.normal.textColor = Color.white;

            ComputeGrid ();

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

        // Recomputes the 4-column grid from the window's current fixed width: the column width is
        // what makes 4 columns, the 3 gaps between them and the 2 edge gaps exactly fill the
        // window's content area. The row-label style is pinned to one column so plain
        // registerLabelStyle labels land on the grid too.
        void ComputeGrid ()
        {
            float contentWidth = Style.fixedWidth - Style.padding.left - Style.padding.right;
            float column = (contentWidth - 5f * cellSpacing) / 4f;
            spanOptions = new GUILayoutOption[5][];
            for (int n = 1; n <= 4; n++)
                spanOptions [n] = new[] { GUILayout.Width (column * n + cellSpacing * (n - 1)) };
            registerLabelStyle.fixedWidth = column;
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
            tooltipStyle.fontSize = body;
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
                ComputeGrid ();
                ApplyFontSizes (scale);
                // Force the window to shrink back to its content height.
                Position = new Rect (Position.x, Position.y, Position.width, 0f);
            }

            var ap = Services.AutoPilot.GetEngaged (VesselId);
            bool engaged = ap != null;

            activeTooltip = null;
            GUILayout.BeginVertical ();

            // Engagement annunciator lamp: green when engaged and controlling, amber HELD while
            // the engaged auto-pilot is held inert on the launch clamps (PRELAUNCH).
            var held = engaged && ap.Held;
            GUILayout.BeginHorizontal ();
            Lamp (held ? "HELD" : "ENGAGED", engaged, held ? amber : green, 4);
            GUILayout.EndHorizontal ();
            RowTooltip ("Auto-pilot engagement. HELD: engaged but held inert on the launch clamps.");

            // Target: axis column headers, then the current target (CURRENT, what the auto-pilot
            // is tracking right now) and below it the commanded target (COMMAND, what was
            // requested). COMMAND is shown only while smoothing is active (TargetSmoothingTime
            // > 0); without smoothing CURRENT always equals the command, so the whole COMMAND
            // row is blanked. RLL is blank when no
            // specific roll is held. Values carry a degree symbol, so the section needs no unit label.
            bool showCmd = engaged && ap.TargetSmoothingTime > 0;
            Header ("TARGET", null);
            AxisColumnHeader ("PCH", "HDG", "RLL");
            GUILayout.BeginHorizontal ();
            GUILayout.Label ("CURRENT", registerLabelStyle);
            Register (engaged ? Deg (ap.CurrentTargetPitch) : Blank);
            Register (engaged ? Deg (ap.CurrentTargetHeading) : Blank);
            Register (engaged ? DegOrBlank (ap.CurrentTargetRoll) : Blank);
            GUILayout.EndHorizontal ();
            RowTooltip ("Target the auto-pilot is tracking right now (slewed toward COMMAND when target smoothing is on).");
            GUILayout.BeginHorizontal ();
            GUILayout.Label ("COMMAND", registerLabelStyle);
            Register (showCmd ? Deg (ap.TargetPitch) : Blank);
            Register (showCmd ? Deg (ap.TargetHeading) : Blank);
            Register (showCmd ? DegOrBlank (ap.TargetRoll) : Blank);
            GUILayout.EndHorizontal ();
            RowTooltip ("Commanded (requested) target; shown while target smoothing is still slewing CURRENT toward it.");

            // Attitude error: the four axis column headers on one row, error lamps on the next, each
            // dim within the AutoPilot.Wait() stopping angle threshold and amber outside it. These
            // are errors to the CURRENT (tracked) target, not the commanded one, so they stay small
            // while a smoothed change is being slewed in (the craft is tracking CURRENT, not COMMAND).
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
            HeadingErrorLamp (engaged, engaged ? ap.CurrentHeadingError : 0f, errorThreshold, engaged ? ap.CurrentPitch : 0f);
            ErrorLamp (rollShown, rollShown ? ap.CurrentRollError : 0f, errorThreshold);
            GUILayout.EndHorizontal ();
            RowTooltip ("Attitude error to the current target, total and per axis. Amber when outside the stopping angle threshold.");

            // Inner-loop PID gains (autotuned each tick when AutoTune is on): axis column headers,
            // then a Kp and a Ki row.
            var gainP = engaged ? ap.PitchPIDGains : null;
            var gainR = engaged ? ap.RollPIDGains : null;
            var gainY = engaged ? ap.YawPIDGains : null;
            Header ("PID GAIN", null);
            AxisColumnHeader ("PCH", "YAW", "RLL");
            GUILayout.BeginHorizontal ();
            GUILayout.Label ("KP", registerLabelStyle);
            Register (gainP == null ? Blank : Gain (gainP.Item1));
            Register (gainY == null ? Blank : Gain (gainY.Item1));
            Register (gainR == null ? Blank : Gain (gainR.Item1));
            GUILayout.EndHorizontal ();
            RowTooltip ("Inner rate-loop proportional gain (set by the autotuner each tick when auto-tune is on).");
            GUILayout.BeginHorizontal ();
            GUILayout.Label ("KI", registerLabelStyle);
            Register (gainP == null ? Blank : Gain (gainP.Item2));
            Register (gainY == null ? Blank : Gain (gainY.Item2));
            Register (gainR == null ? Blank : Gain (gainR.Item2));
            GUILayout.EndHorizontal ();
            RowTooltip ("Inner rate-loop integral gain (set by the autotuner each tick when auto-tune is on).");

            // Oscillation handling, mirroring the controller's detector → gates → mitigations
            // structure. DETECTOR: what the runtime observes (per-axis structural level, the
            // post-floor loop bandwidth, the estimated mode frequency and the control-output
            // envelope). GATES: the [0,1] weights that decide how strongly the mitigations
            // engage. MITIGATIONS: one combined mode+engagement lamp per individually
            // controllable mitigation — dim whenever the mitigation is not acting (manually Off
            // or automatically idle), amber when engaged.
            Header ("OSCILLATION", null);
            var level = engaged ? ap.OscillationLevel : null;
            // Beyond reach of a level of 0 when disengaged, so the lamp stays green while not engaged.
            double latchThreshold = engaged ? ap.OscillationLatchThreshold : double.PositiveInfinity;
            AxisColumnHeader ("PCH", "YAW", "RLL");
            // Structural-wobble level the detector reads off the measured angular rate, per axis.
            GUILayout.BeginHorizontal ();
            GUILayout.Label ("STRUC", registerLabelStyle);
            LevelLamp (engaged, level == null ? 0 : level.Item1, engaged && ap.PitchYawOscillationLatched, latchThreshold);
            LevelLamp (engaged, level == null ? 0 : level.Item3, engaged && ap.PitchYawOscillationLatched, latchThreshold);
            LevelLamp (engaged, level == null ? 0 : level.Item2, engaged && ap.RollOscillationLatched, latchThreshold);
            GUILayout.EndHorizontal ();
            RowTooltip ("Structural oscillation measurement: bending-mode chatter detected in the measured rate, per axis. Amber: at the latch threshold, or latched.");

            TwoColumnHeader ("PCH/YAW", "RLL");

            // Control-output oscillation envelope per group: the about-mean amplitude of the delivered
            // command that drives the back-off gate, lit amber at/above the engage threshold.
            var oscThreshold = engaged ? ap.OscillationControlThreshold : double.PositiveInfinity;
            GUILayout.BeginHorizontal ();
            GUILayout.Label ("CTRL", registerLabelStyle);
            OscCell (engaged, engaged ? ap.PitchYawControlOscillation : 0.0, oscThreshold, 2);
            OscCell (engaged, engaged ? ap.RollControlOscillation : 0.0, oscThreshold);
            GUILayout.EndHorizontal ();
            RowTooltip ("Oscillation envelope of the delivered control output. Amber at the level that engages BACKOFF.");

            // Estimated structural mode frequency per group (held estimator value).
            GUILayout.BeginHorizontal ();
            GUILayout.Label ("FREQ", registerLabelStyle);
            RegisterCentered (engaged ? FreqText (ap.PitchYawOscillationDetectedFrequency) : Blank, 2);
            RegisterCentered (engaged ? FreqText (ap.RollOscillationDetectedFrequency) : Blank);
            GUILayout.EndHorizontal ();
            RowTooltip ("Estimated structural mode frequency; holds the last acquired lock. Routes the rate filter (FILTER).");

            // The gates: HOLD (the pointing-error hold factor, global), LATCH (the detector's
            // persistent flexible-craft verdict), RAMP (the eased latch weight), BACKOFF (the
            // limit-cycle back-off) and GATE (the net hold-gated mitigation level,
            // ramp · max(hold, bko), which drives the floor and the feedforward cut).
            Header ("GATES", null);
            var ramp = engaged ? ap.SuppressionRamp : null;
            var backoff = engaged ? ap.OscillationBackoff : null;
            var mitigation = engaged ? ap.MitigationLevel : null;
            TwoColumnHeader ("PCH/YAW", "RLL");
            GUILayout.BeginHorizontal ();
            GUILayout.Label ("HOLD", registerLabelStyle);
            RegisterCentered (engaged ? Percent (ap.PitchYawHoldFactor) : Blank, 2);
            RegisterCentered (engaged ? Percent (ap.RollHoldFactor) : Blank);
            GUILayout.EndHorizontal ();
            RowTooltip ("Hold factor: 100% holding attitude, 0% slewing. Gates the latched mitigations via GATE.");
            GUILayout.BeginHorizontal ();
            GUILayout.Label ("LATCH", registerLabelStyle);
            LatchLamp (engaged && ap.PitchYawOscillationLatched, 2);
            LatchLamp (engaged && ap.RollOscillationLatched, 1);
            GUILayout.EndHorizontal ();
            RowTooltip ("The detector's persistent verdict that the craft is flexible; lasts for the rest of the engagement.");
            GUILayout.BeginHorizontal ();
            GUILayout.Label ("RAMP", registerLabelStyle);
            GateLamp (engaged, GroupMax (ramp), 2);
            GateLamp (engaged, ramp == null ? 0.0 : ramp.Item2);
            GUILayout.EndHorizontal ();
            RowTooltip ("Eased weight of the latched mitigations, ramped in/out so the control does not step.");
            GUILayout.BeginHorizontal ();
            GUILayout.Label ("BACKOFF", registerLabelStyle);
            GateLamp (engaged, GroupMax (backoff), 2);
            GateLamp (engaged, backoff == null ? 0.0 : backoff.Item2);
            GUILayout.EndHorizontal ();
            RowTooltip ("Limit-cycle back-off: keeps the mitigations engaged while CTRL shows a sustained oscillation during a slew.");
            GUILayout.BeginHorizontal ();
            GUILayout.Label ("GATE", registerLabelStyle);
            GateLamp (engaged, GroupMax (mitigation), 2);
            GateLamp (engaged, mitigation == null ? 0.0 : mitigation.Item2);
            GUILayout.EndHorizontal ();
            RowTooltip ("Net mitigation level, RAMP × max(HOLD, BACKOFF); drives FLOOR and FFCUT.");

            // The mitigations, one combined mode+engagement lamp each. FILTER is the rate filter
            // (per group, showing the tool Automatic routed to, or the forced tool); FLOOR the
            // bandwidth-floor engagement; FFCUT the applied feedforward-cut fraction; SMOOTH the
            // output-smoothing blend weight.
            Header ("MITIGATIONS", null);
            var ffCut = engaged ? ap.FeedforwardCut : null;
            var outWeight = engaged ? ap.OutputFilterWeight : null;
            TwoColumnHeader ("PCH/YAW", "RLL");
            GUILayout.BeginHorizontal ();
            GUILayout.Label ("FILTER", registerLabelStyle);
            RateFilterLamp (engaged, engaged ? ap.PitchYawRateFilterMode : default (Services.RateFilterMode),
                engaged ? ap.ActiveSuppressionTool (0) : 0, 2);
            RateFilterLamp (engaged, engaged ? ap.RollRateFilterMode : default (Services.RateFilterMode),
                engaged ? ap.ActiveSuppressionTool (1) : 0);
            GUILayout.EndHorizontal ();
            RowTooltip ("Rate filter suppressing the structural mode in the measured rate: a notch or low-pass, routed by FREQ.");
            var floorMode = engaged ? ap.OscillationBandwidthFloorMode : default (Services.MitigationMode);
            GUILayout.BeginHorizontal ();
            GUILayout.Label ("FLOOR", registerLabelStyle);
            MitigationLamp (engaged, floorMode, GroupMax (mitigation), 2);
            MitigationLamp (engaged, floorMode, mitigation == null ? 0.0 : mitigation.Item2);
            GUILayout.EndHorizontal ();
            RowTooltip ("Bandwidth floor: pulls the inner-loop bandwidth down on a latched axis while holding.");
            var ffMode = engaged ? ap.OscillationFeedforwardMode : default (Services.MitigationMode);
            GUILayout.BeginHorizontal ();
            GUILayout.Label ("FFCUT", registerLabelStyle);
            MitigationLamp (engaged, ffMode, GroupMax (ffCut), 2);
            MitigationLamp (engaged, ffMode, ffCut == null ? 0.0 : ffCut.Item2);
            GUILayout.EndHorizontal ();
            RowTooltip ("Feedforward cut: fraction of the feedforward removed on a latched axis while holding.");
            var outMode = engaged ? ap.OscillationOutputFilterMode : default (Services.MitigationMode);
            GUILayout.BeginHorizontal ();
            GUILayout.Label ("SMOOTH", registerLabelStyle);
            MitigationLamp (engaged, outMode, GroupMax (outWeight), 2);
            MitigationLamp (engaged, outMode, outWeight == null ? 0.0 : outWeight.Item2);
            GUILayout.EndHorizontal ();
            RowTooltip ("Output smoothing: low-pass blend applied to the delivered control output.");

            GUILayout.EndVertical ();

            DrawTooltip ();
            GUI.DragWindow ();
        }

        // Marks the row group just closed by GUILayout.EndHorizontal as a tooltip hover target:
        // when the mouse is over it, the description and the row's rect are captured and drawn as
        // a floating tooltip box by DrawTooltip at the end of the pass. GetLastRect returns the
        // closed group's rect only during Repaint, which is also the only pass that draws, so the
        // capture is gated on it.
        void RowTooltip (string text)
        {
            if (Event.current.type != EventType.Repaint)
                return;
            var row = GUILayoutUtility.GetLastRect ();
            // Grow the hit area past the 1px margins between rows, so a slow vertical sweep of
            // the mouse does not drop the tooltip on the seam where neither row is hit. Adjacent
            // grown rects overlap slightly; the later (lower) row wins, since it overwrites.
            row.yMin -= 2f;
            row.yMax += 2f;
            if (row.Contains (Event.current.mousePosition)) {
                activeTooltip = text;
                activeTooltipRow = row;
            }
        }

        // Draws the captured tooltip anchored to the hovered row: just below it, or just above it
        // when too close to the window's bottom edge. Anchoring to the row (never the mouse)
        // keeps the tooltip still while the mouse moves around within the row. The tooltip is
        // drawn with GUI (not GUILayout) so it overlays the panel without affecting the window's
        // auto-layout height, and stays inside the window rect because GUI.Window clips its
        // contents.
        void DrawTooltip ()
        {
            if (activeTooltip == null || Event.current.type != EventType.Repaint)
                return;
            float scale = GameSettings.UI_SCALE;
            var content = new GUIContent (activeTooltip);
            float width = Position.width - 16f * scale;
            float height = tooltipStyle.CalcHeight (content, width);
            float x = (Position.width - width) / 2f;
            float y = activeTooltipRow.yMax + 2f;
            if (y + height > Position.height - 4f * scale)
                y = activeTooltipRow.yMin - height - 2f;
            GUI.Label (new Rect (x, y, width, height), content, tooltipStyle);
        }

        // Draws one square backlit annunciator lamp spanning the given number of grid columns.
        // When unlit, the lamp shows the register background with faint grey "engraved" text;
        // when lit, the full colour with black text. The lamp is purely informational (drawn as
        // a box, never interactive).
        void Lamp (string label, bool lit, Color colour, int span = 1)
        {
            Lamp (label, lit, colour, engravedGrey, span);
        }

        // Lamp variant for cells that carry a data readout as well as a status colour (attitude
        // error, STRUC, CTRL): the unlit text stays plain white — the value must remain readable
        // when the status is nominal — rather than the engraved grey of a pure status lamp.
        // Callers blank the label when there is no live value (auto-pilot disengaged), so white
        // never shows a stale or placeholder reading.
        void DataLamp (string label, bool lit, Color colour, int span = 1)
        {
            Lamp (label, lit, colour, Color.white, span);
        }

        void Lamp (string label, bool lit, Color colour, Color unlitText, int span)
        {
            var prevBg = GUI.backgroundColor;
            GUI.backgroundColor = lit ? colour : registerBackground;
            lampStyle.normal.textColor = lit ? Color.black : unlitText;
            // A box with no measurable content picks up a slightly different line height, so a
            // blank lamp (e.g. RLL when no roll is held) would render taller than its lit
            // neighbours. A plain space does not fix this: IMGUI trims trailing whitespace when
            // measuring, so it still measures as empty. A non-breaking space is measured as a
            // real glyph, keeping the box geometry identical to a populated cell while still
            // reading as blank.
            GUILayout.Box (string.IsNullOrEmpty (label) ? NonBreakingSpace : label, lampStyle, spanOptions [span]);
            GUI.backgroundColor = prevBg;
        }

        // One control-oscillation envelope lamp (matching the structural LevelLamp): lit amber
        // at/above the engage threshold, green below, dim when disengaged.
        void OscCell (bool engaged, double level, double threshold, int span = 1)
        {
            DataLamp (engaged ? Percent (level) : Blank, engaged && level >= threshold, amber, span);
        }

        // Attitude-error lamp: green when the axis error is within the AutoPilot.Wait() stopping
        // angle threshold, amber when outside it. Dim/blank when not shown (disengaged, or roll on an
        // axis whose roll is not held).
        void ErrorLamp (bool show, float error, float threshold)
        {
            DataLamp (show ? Deg (error) : Blank, show && Math.Abs (error) > threshold, amber);
        }

        // Heading-error lamp. Heading is a coordinate singularity at the poles: near vertical
        // (pitch → ±90°) swinging the heading barely moves where the craft actually points, so a
        // negligible pointing error shows up as a huge heading-angle error and would pin the lamp
        // permanently lit. Gate on the heading error's true contribution to the pointing error,
        // error·cos(pitch), rather than the raw error — this widens the allowed heading error as
        // the craft nears vertical and drops it to zero exactly at the pole. The displayed value
        // is still the raw heading error.
        void HeadingErrorLamp (bool show, float error, float threshold, float pitch)
        {
            double effective = Math.Abs (error) * Math.Abs (Math.Cos (pitch * Math.PI / 180.0));
            DataLamp (show ? Deg (error) : Blank, show && effective > threshold, amber);
        }

        // Frequency lamp: green showing the detected structural frequency once the tracker has a
        // lock, amber "NO LOCK" until then (and dim when disengaged).
        static string FreqText (double freq)
        {
            return double.IsNaN (freq) ? "NO LOCK" : string.Format ("{0:F1} Hz", freq);
        }

        // The axis is identified by the PCH/YAW/RLL column header, so the lamp shows only the level value.
        void LevelLamp (bool engaged, double level, bool latched, double latchThreshold)
        {
            // Amber once the level reaches the controller's latch threshold (so it is in the range
            // that triggers the latch), or the axis has already latched; dim below.
            DataLamp (engaged ? Percent (level) : Blank, engaged && (latched || level >= latchThreshold), amber);
        }

        // Latch lamp: amber ENGAGED once the axis group has latched as flexible; blank (not
        // engraved) when unlatched or disengaged, so the word only ever appears lit.
        void LatchLamp (bool latched, int span)
        {
            Lamp (latched ? "ENGAGED" : Blank, latched, amber, span);
        }

        // Gate readout lamp: dim while the gate is inactive (at zero), amber once it is engaged.
        void GateLamp (bool engaged, double level, int span = 1)
        {
            Lamp (engaged ? Percent (level) : Blank, engaged && level > 0.01, amber, span);
        }

        // Combined mode + engagement lamp for one mitigation cell: dim whenever the mitigation
        // is not acting (manually Off, or automatically idle at ~0), amber when engaged. The
        // label carries the mode (OFF) or the engagement level. Forced pins the mitigation fully
        // on, so its lamp reads 100% regardless of the gate.
        void MitigationLamp (bool engaged, Services.MitigationMode mode, double level, int span = 1)
        {
            if (engaged && mode == Services.MitigationMode.Off) {
                Lamp ("OFF", false, amber, span);
                return;
            }
            if (engaged && mode == Services.MitigationMode.Forced)
                level = 1.0;
            Lamp (engaged ? Percent (level) : Blank, engaged && level > 0.01, amber, span);
        }

        // Combined mode + engagement lamp for the rate filter on one axis group: dim when Off or
        // while Automatic has no tool engaged, amber with the tool name whenever a filter is
        // actually running (automatic-routed or forced).
        void RateFilterLamp (bool engaged, Services.RateFilterMode mode, int tool, int span = 1)
        {
            if (engaged && mode == Services.RateFilterMode.Off) {
                Lamp ("OFF", false, amber, span);
                return;
            }
            if (tool != 0) {
                Lamp (ToolName (tool), engaged, amber, span);
                return;
            }
            Lamp (engaged ? "AUTO" : Blank, false, amber, span);
        }

        // The pitch/yaw group cell for a per-axis (pitch, roll, yaw) tuple: the two transverse
        // axes latch together, so the group shows the larger of the two.
        static double GroupMax (Tuple<double, double, double> t)
        {
            return t == null ? 0.0 : Math.Max (t.Item1, t.Item3);
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

        // Draws one digital-register value cell spanning the given number of grid columns.
        void Register (string value, int span = 1)
        {
            GUILayout.Label (value, registerStyle, spanOptions [span]);
        }

        // As Register, but the content is centred rather than right-aligned.
        void RegisterCentered (string value, int span = 1)
        {
            GUILayout.Label (value, registerCenterStyle, spanOptions [span]);
        }

        // Draws one centered column-header cell on the grid, matching the value/lamp cells drawn
        // below it.
        void ColumnHead (string text, int span = 1)
        {
            GUILayout.Label (text, columnHeadStyle, spanOptions [span]);
        }

        // Empty spacer matching the row-label column width, but drawn with the small column-header
        // style so it does not inflate the header row to body-font height (which would leave an
        // oversized gap below the small header text). The width matches registerLabelStyle so the
        // axis columns still line up with the value rows below.
        void ColumnSpacer ()
        {
            GUILayout.Label (string.Empty, columnHeadStyle, GUILayout.Width (registerLabelStyle.fixedWidth));
        }

        // A sub-header labelling the PCH/YAW group (two grid columns) and RLL (one) over a
        // leading row-label column.
        void TwoColumnHeader (string left, string right)
        {
            GUILayout.BeginHorizontal ();
            ColumnSpacer ();
            ColumnHead (left, 2);
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
            if (tooltipTexture != null)
                Destroy (tooltipTexture);
            if (monoFont != null)
                Destroy (monoFont);
        }
    }
}
