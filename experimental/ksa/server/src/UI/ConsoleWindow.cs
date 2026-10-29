using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Brutal.ImGuiApi;
using Brutal.Numerics;
using HarmonyLib;
using KSA;
using Logger = KRPC.Utils.Logger;

namespace KRPC.KSA.UI
{
    /// <summary>
    /// A window in the game's console style, opened at its saved position and size, or at
    /// its own default size.
    /// </summary>
    abstract class ConsoleWindow : ImGuiWindow, IStaticWindow
    {
        static Func<ImGuiWindow, float2> computeSafeWindowSize;
        static Func<float2, float2> computeCenteredWindowPosition;
        static FieldInfo windowPos;
        static FieldInfo windowSize;
        readonly Tuple<float, float, float, float> savedRect;

        /// <summary>
        /// Create the window. A saved rectangle is <c>(x, y, width, height)</c> in pixels, with
        /// a zero width for none.
        /// </summary>
        protected ConsoleWindow (string title, string signature, float2 size, bool show, Tuple<float, float, float, float> rect)
            : base (size, false, show)
        {
            SetWindowTitle (title);
            UseConsoleChrome = true;
            ConsoleSignature = signature;
            savedRect = rect;
            Rect = rect;
        }

        /// <summary>
        /// The position and size of the window when it was last drawn, as <c>(x, y, width, height)</c>.
        /// </summary>
        public Tuple<float, float, float, float> Rect { get; private set; }

        public sealed override void DrawContent (IViewport viewport)
        {
            // The game's console style records the window it is drawing
            if (windowPos != null) {
                var pos = (float2)windowPos.GetValue (null);
                var size = (float2)windowSize.GetValue (null);
                Rect = Tuple.Create (MathF.Round (pos.X), MathF.Round (pos.Y), MathF.Round (size.X), MathF.Round (size.Y));
            }
            DrawWindow (viewport);
        }

        protected abstract void DrawWindow (IViewport viewport);

        /// <summary>
        /// Open our windows at their saved position and size, or default size.
        /// </summary>
        /// <remarks>
        /// The game opens a console window centered, and at no less than
        /// <c>ImGuiHelper.LargeResizableScaled</c>. We replace the calls in
        /// <c>ImGuiWindow.OnDrawUiConsole</c> that compute these, and call the originals for
        /// the game's windows.
        /// </remarks>
        public static void Patch (Harmony harmony)
        {
            var compute = AccessTools.Method (typeof (ImGuiWindow), "ComputeSafeWindowSize");
            var center = AccessTools.Method (typeof (ImGuiWindow), "ComputeCenteredWindowPosition");
            var draw = AccessTools.Method (typeof (ImGuiWindow), "OnDrawUiConsole");
            if (compute == null || center == null || draw == null) {
                Logger.WriteLine ("ImGuiWindow.OnDrawUiConsole not as expected, kRPC windows open at the game's size", Logger.Severity.Error);
                return;
            }
            computeSafeWindowSize = AccessTools.MethodDelegate<Func<ImGuiWindow, float2>> (compute);
            computeCenteredWindowPosition = AccessTools.MethodDelegate<Func<float2, float2>> (center);
            windowPos = AccessTools.Field (typeof (ConsoleStyle), "_windowPos");
            windowSize = AccessTools.Field (typeof (ConsoleStyle), "_windowSize");
            if (windowPos == null || windowSize == null) {
                Logger.WriteLine ("ConsoleStyle window fields not found, kRPC window positions are not saved", Logger.Severity.Error);
                windowPos = null;
            }
            harmony.Patch (draw, transpiler: new HarmonyMethod (typeof (ConsoleWindow), nameof (Transpiler)));
        }

        static IEnumerable<CodeInstruction> Transpiler (IEnumerable<CodeInstruction> instructions)
        {
            var compute = AccessTools.Method (typeof (ImGuiWindow), "ComputeSafeWindowSize");
            var center = AccessTools.Method (typeof (ImGuiWindow), "ComputeCenteredWindowPosition");
            foreach (var instruction in instructions) {
                if (instruction.Calls (compute)) {
                    instruction.opcode = OpCodes.Call;
                    instruction.operand = AccessTools.Method (typeof (ConsoleWindow), nameof (ComputeSafeWindowSize));
                } else if (instruction.Calls (center)) {
                    // The size is on the stack, and the window is passed after it
                    yield return new CodeInstruction (OpCodes.Ldarg_0);
                    instruction.operand = AccessTools.Method (typeof (ConsoleWindow), nameof (ComputeWindowPosition));
                }
                yield return instruction;
            }
        }

        /// <summary>
        /// Draw a labeled text field that takes every edit, and marks a value that cannot be
        /// saved. <c>ConsoleWidgets.TextLine</c> takes an edit only when Enter is pressed.
        /// </summary>
        internal static bool DrawTextRow (string label, string id, ref string text, bool invalid, bool number = false)
        {
            ConsoleWidgets.BeginRow (label);
            if (invalid)
                ImGui.PushStyleColor (ImGuiCol.Text, ConsoleStyle.Pending);
            ConsoleStyle.PushValueFont ();
            if (number) {
                // Wide enough for a time in microseconds, at the right of the row
                var width = ImGui.CalcTextSize ("00000000").X + 2 * ConsoleStyle.ScaledFramePaddingPx.X;
                ImGui.SetCursorPosX (ImGui.GetCursorPosX () + Math.Max (0, ImGui.GetContentRegionAvail ().X - width));
                ImGui.SetNextItemWidth (width);
            } else {
                ImGui.SetNextItemWidth (-1);
            }
            // ImGui holds the text being edited, and copies it into the buffer on each edit
            var buffer = new ImInputString (256, text);
            var changed = ImGui.InputText ("##" + id, buffer);
            if (changed)
                text = buffer.ToString ();
            ConsoleStyle.PopFont ();
            if (invalid)
                ImGui.PopStyleColor ();
            ConsoleWidgets.EndRow ();
            return changed;
        }

        static float2 ComputeSafeWindowSize (ImGuiWindow window)
        {
            if (window is not ConsoleWindow consoleWindow)
                return computeSafeWindowSize (window);
            var rect = consoleWindow.savedRect;
            var size = rect.Item3 > 0 ? new float2 (rect.Item3, rect.Item4) : consoleWindow._initialSize * ImGuiHelper.InterfaceScale;
            var screen = ImGui.GetMainViewport ().Size;
            return new float2 (Math.Min (size.X, screen.X - 32), Math.Min (size.Y, screen.Y - 32));
        }

        static float2 ComputeWindowPosition (float2 size, ImGuiWindow window)
        {
            if (window is not ConsoleWindow consoleWindow || consoleWindow.savedRect.Item3 <= 0)
                return computeCenteredWindowPosition (size);
            // Kept in the game's work area, as the game keeps it each frame after
            var rect = consoleWindow.savedRect;
            var viewport = ImGui.GetMainViewport ();
            var min = viewport.WorkPos;
            var max = min + viewport.WorkSize - size;
            return new float2 (
                Math.Clamp (rect.Item1, min.X, Math.Max (min.X, max.X)),
                Math.Clamp (rect.Item2, min.Y, Math.Max (min.Y, max.Y)));
        }
    }
}
