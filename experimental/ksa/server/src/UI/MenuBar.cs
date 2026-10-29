using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using Brutal.ImGuiApi;
using HarmonyLib;
using KSA;
using Logger = KRPC.Utils.Logger;

namespace KRPC.KSA.UI
{
    /// <summary>
    /// The kRPC menu in the game's menu bar.
    /// </summary>
    static class MenuBar
    {
        static Addon addon;

        /// <summary>
        /// Add the menu after the game's own menus.
        /// </summary>
        /// <remarks>
        /// <c>Program.DrawMenuBar</c> calls the empty <c>Program.DrawProgramMenusHook</c> after its
        /// last menu, before the version text at the right. We insert a call to the menu after
        /// it, as the JIT may inline a call to an empty method and skip a patch on it.
        /// </remarks>
        public static void Patch (Harmony harmony, Addon instance)
        {
            addon = instance;
            harmony.Patch (Addon.Required (typeof (Program), "DrawMenuBar"),
                transpiler: new HarmonyMethod (typeof (MenuBar), nameof (Transpiler)));
        }

        static IEnumerable<CodeInstruction> Transpiler (IEnumerable<CodeInstruction> instructions)
        {
            var code = instructions.ToList ();
            var hook = AccessTools.Method (typeof (Program), "DrawProgramMenusHook");
            var index = hook == null ? -1 : code.FindIndex (x => x.Calls (hook));
            if (index < 0)
                throw new InvalidOperationException ("Program.DrawMenuBar has no Program.DrawProgramMenusHook call");
            code.Insert (index + 1, new CodeInstruction (OpCodes.Call, AccessTools.Method (typeof (MenuBar), nameof (Draw))));
            return code;
        }

        static void Draw ()
        {
            try {
                if (addon.MainWindow == null || !ImGui.BeginMenu ("kRPC"))
                    return;
                if (ImGui.MenuItem ("Server", selected: addon.MainWindow.IsShown))
                    addon.MainWindow.Toggle ();
                if (ImGui.MenuItem ("Server info", selected: addon.InfoWindow.IsShown))
                    addon.InfoWindow.Toggle ();
                ImGui.Separator ();
                var running = addon.Core.AnyRunning;
                if (ImGui.MenuItem (running ? "Stop server" : "Start server")) {
                    if (running)
                        addon.StopAllServers ();
                    else
                        addon.StartAllServers ();
                }
                ImGui.EndMenu ();
            } catch (Exception e) {
                // An exception escaping the patch would take the game's frame down with it
                Logger.WriteLine ("kRPC menu failed: " + e, Logger.Severity.Error);
            }
        }
    }
}
