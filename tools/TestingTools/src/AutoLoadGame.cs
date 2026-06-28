/*
 * This code is adapted from the AutoLoadGame plugin
 * https://github.com/allista/AutoLoadGame
 * 
 * The MIT License (MIT)
 *
 * Copyright (c) 2016 Allis Tauri
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

using System;
using System.IO;
using SaveUpgradePipeline;
using UnityEngine;

namespace TestingTools
{
    /// <summary>
    /// Addon that automatically loads a save when the game starts
    /// </summary>
    [KSPAddon(KSPAddon.Startup.MainMenu, false)]
    public sealed class AutoLoadGame : MonoBehaviour
    {
        /// <summary>
        /// Name of the game to load.
        /// </summary>
        public static string Game {
            get { return Options.Game; }
        }

        /// <summary>
        /// Name of the save file to load.
        /// </summary>
        public static string Save {
            get { return Options.Save; }
        }

        /// <summary>
        /// Whether the game has been loaded.
        /// </summary>
        public static bool Loaded { get; private set; }

        static TestingToolsOptions Options {
            get { return TestingToolsOptions.Instance; }
        }

        /// <summary>
        /// Initialize the addon.
        /// </summary>
        public void Awake()
        {
            GameEvents.onLevelWasLoadedGUIReady.Add(OnGUIReady);
        }

        /// <summary>
        /// Destroy the addon.
        /// </summary>
        public void OnDestroy()
        {
            GameEvents.onLevelWasLoadedGUIReady.Remove(OnGUIReady);
        }

        // Named OnGUIReady rather than OnLevelWasLoaded to avoid colliding with
        // Unity's deprecated OnLevelWasLoaded(int) magic message. The collision is
        // harmless (the handler is invoked via the GameEvents delegate, not Unity's
        // message dispatch) but logs a spurious "message parameter has to be of
        // type: int" error on every run.
        void OnGUIReady(GameScenes scene)
        {
            if (scene == GameScenes.MAINMENU)
                StartCoroutine(CallbackUtil.DelayedCallback(15, LoadGame));
        }

        static void LoadGame()
        {
            if (Loaded)
                return;
            Loaded = true;

            Debug.Log("[kRPC testing tools]: Loading game \"" + Game + "\" save \"" + Save + "\"");
            var gameObj = GamePersistence.LoadSFSFile(Save, Game);
            if (gameObj == null) {
                Debug.LogWarning("[kRPC testing tools]: Failed to load game, got null when loading sfs file");
                return;
            }
            KSPUpgradePipeline.Process(gameObj, Game, LoadContext.SFS, OnLoadDialogPipelineFinished, OnLoadDialogPipelineError);
        }

        static void OnLoadDialogPipelineError(KSPUpgradePipeline.UpgradeFailOption opt, ConfigNode node) {
            Debug.LogError("[kRPC testing tools]: KSPUpgradePipeline failed " + opt.ToString() + " " + node);
        }

        static void OnLoadDialogPipelineFinished(ConfigNode node)
        {
            // Load game cfg
            HighLogic.CurrentGame = GamePersistence.LoadGameCfg(node, Game, true, false);
            if (HighLogic.CurrentGame == null) {
                Debug.LogWarning("[kRPC testing tools]: Failed to load game, got null when loading game cfg");
                return;
            }
            // Ensure the loaded save has every scenario module the current install
            // expects. Loading a clean save into a modded install commonly adds
            // modules and makes this return true; that is normal, not a failure, so
            // the return value is ignored, matching KSP's own MainMenu load flow.
            // (Treating true as an error here left the game stuck at the main menu.)
            GamePersistence.UpdateScenarioModules(HighLogic.CurrentGame);

            // Load the game
            HighLogic.CurrentGame.startScene = GameScenes.SPACECENTER;
            HighLogic.SaveFolder = Game;

            if (Options.HasCraft) {
                // Still start into the Space Center if staging fails, rather than
                // leaving KSP stuck at the main menu. PrepareCraftLaunch has already
                // logged why; loading the save anyway lets a test client connect and
                // fail with a real assertion instead of a connection timeout.
                if (PrepareCraftLaunch(Options))
                    AutoSwitchVessel.SetCraftLaunch(
                        Options.Craft, Options.CraftDirectory, Options.LaunchSite);
                else
                    Debug.LogWarning(
                        "[kRPC testing tools]: Loading into the Space Center without " +
                        "launching a craft");
            } else {
                var vesselIdx = FindVesselToSwitchTo(Options);
                if (vesselIdx < 0) {
                    Debug.LogWarning("[kRPC testing tools]: Failed to find vessel to switch to");
                    return;
                }
                AutoSwitchVessel.Vessel = vesselIdx;
            }

            HighLogic.CurrentGame.Start();
        }

        static int FindVesselToSwitchTo(TestingToolsOptions options)
        {
            var vessels = HighLogic.CurrentGame.flightState.protoVessels;
            if (options.Vessel.HasValue) {
                var vesselIdx = options.Vessel.Value;
                if (vesselIdx >= 0 && vesselIdx < vessels.Count) {
                    Debug.Log("[kRPC testing tools]: Switching to vessel index " + vesselIdx);
                    return vesselIdx;
                }
                Debug.LogWarning(
                    "[kRPC testing tools]: Vessel index " + vesselIdx +
                    " is out of range; falling back to the first non-space-object vessel");
            }

            for (int i = 0; i < vessels.Count; i++) {
                if (vessels [i].vesselType != VesselType.SpaceObject)
                    return i;
            }
            return -1;
        }

        static bool PrepareCraftLaunch(TestingToolsOptions options)
        {
            var craftName = options.Craft;
            var targetDirectory = Path.Combine(
                KSPUtil.ApplicationRootPath, "saves", Game, "Ships", options.CraftDirectory);
            var targetCraft = Path.Combine(targetDirectory, craftName + ".craft");
            var sourceCraft = FindCraftSource(options);

            if (sourceCraft == null) {
                if (File.Exists(targetCraft)) {
                    Debug.Log(
                        "[kRPC testing tools]: Launching already-staged " +
                        options.CraftDirectory + " craft \"" + craftName + "\"");
                    return true;
                }
                Debug.LogWarning(
                    "[kRPC testing tools]: Failed to find craft \"" + craftName +
                    "\" in save Ships/" + options.CraftDirectory +
                    " (pass --krpc-auto-load-craft-fixture-dir to stage one from elsewhere)");
                return false;
            }

            try {
                Directory.CreateDirectory(targetDirectory);
                CopyFileIfDifferent(sourceCraft, targetCraft);

                var sourceLoadMeta = Path.ChangeExtension(sourceCraft, ".loadmeta");
                if (File.Exists(sourceLoadMeta)) {
                    var targetLoadMeta = Path.Combine(targetDirectory, craftName + ".loadmeta");
                    CopyFileIfDifferent(sourceLoadMeta, targetLoadMeta);
                }

                Debug.Log(
                    "[kRPC testing tools]: Staged " + options.CraftDirectory +
                    " craft \"" + craftName + "\" from " + sourceCraft);
                return true;
            } catch (Exception e) {
                Debug.LogError(
                    "[kRPC testing tools]: Failed to stage craft \"" + craftName + "\": " + e);
                return false;
            }
        }

        static string FindCraftSource(TestingToolsOptions options)
        {
            // Without an explicit fixture directory, the craft is expected to be
            // already staged in the save's Ships directory; PrepareCraftLaunch
            // handles that case when this returns null.
            if (string.IsNullOrEmpty(options.CraftFixtureDirectory))
                return null;

            var source = FindCraftInDirectory(options.CraftFixtureDirectory, options.Craft);
            if (source == null)
                Debug.LogWarning(
                    "[kRPC testing tools]: Craft \"" + options.Craft +
                    "\" was not found in fixture directory " + options.CraftFixtureDirectory);
            return source;
        }

        static string FindCraftInDirectory(string directory, string craftName)
        {
            try {
                var path = Path.Combine(directory, craftName + ".craft");
                return File.Exists(path) ? path : null;
            } catch (Exception) {
                return null;
            }
        }

        static void CopyFileIfDifferent(string source, string target)
        {
            if (PathsEqual(source, target))
                return;
            File.Copy(source, target, true);
        }

        static bool PathsEqual(string first, string second)
        {
            return string.Equals(
                Path.GetFullPath(first).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                Path.GetFullPath(second).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                StringComparison.OrdinalIgnoreCase);
        }
    }
}
