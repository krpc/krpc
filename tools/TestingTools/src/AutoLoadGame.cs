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
using System.Diagnostics.CodeAnalysis;
using SaveUpgradePipeline;
using UnityEngine;

namespace TestingTools
{
    /// <summary>
    /// Addon that automatically loads a save when the game starts
    /// </summary>
    [KSPAddon (KSPAddon.Startup.MainMenu, false)]
    public sealed class AutoLoadGame : MonoBehaviour
    {
        /// <summary>
        /// Name of the game to load.
        /// </summary>
        public static string Game {
            get { return "default"; }
        }

        /// <summary>
        /// Name of the save file to load.
        /// </summary>
        public static string Save {
            get { return "persistent"; }
        }

        /// <summary>
        /// Whether the game has been loaded.
        /// </summary>
        public static bool Loaded { get; private set; }

        /// <summary>
        /// Initialize the addon.
        /// </summary>
        public void Awake()
        {
            GameEvents.onLevelWasLoadedGUIReady.Add(OnLevelWasLoaded);
        }

        void OnLevelWasLoaded(GameScenes scene)
        {
            if (scene == GameScenes.MAINMENU)
                StartCoroutine(CallbackUtil.DelayedCallback(15, LoadGame));
        }

        [SuppressMessage("Gendarme.Rules.BadPractice", "DisableDebuggingCodeRule")]
        static void LoadGame ()
        {
            if (Loaded)
                return;
            Loaded = true;

            Console.WriteLine("[kRPC testing tools]: Loading game \"" + Game + "\"");
            var gameObj = GamePersistence.LoadSFSFile(Save, Game);
            if (gameObj == null) {
                Console.WriteLine("[kRPC testing tools]: Failed to load game, got null when loading sfs file");
                return;
            }
            KSPUpgradePipeline.Process(gameObj, Game, LoadContext.SFS, OnLoadDialogPipelineFinished, OnLoadDialogPipelineError);
        }

        [SuppressMessage("Gendarme.Rules.BadPractice", "DisableDebuggingCodeRule")]
        static void OnLoadDialogPipelineError(KSPUpgradePipeline.UpgradeFailOption opt, ConfigNode node) {
            Console.WriteLine("[kRPC testing tools]: KSPUpgradePipeline failed " + opt.ToString() + " " + node);
        }
                           
        [SuppressMessage("Gendarme.Rules.BadPractice", "DisableDebuggingCodeRule")]
        static void OnLoadDialogPipelineFinished(ConfigNode node)
        {
            // Load game cfg
            HighLogic.CurrentGame = GamePersistence.LoadGameCfg(node, Game, true, false);
            if (HighLogic.CurrentGame == null) {
                Console.WriteLine("[kRPC testing tools]: Failed to load game, got null when loading game cfg");
                return;
            }
            if (GamePersistence.UpdateScenarioModules(HighLogic.CurrentGame)) {
                Console.WriteLine("[kRPC testing tools]: Failed to load game, scenario update required");
                return;
            }

            // Find the vessel to switch to
            bool foundVessel = false;
            int vesselIdx = 0;
            foreach (var vessel in HighLogic.CurrentGame.flightState.protoVessels) {
               if (vessel.vesselType != VesselType.SpaceObject) {
                   foundVessel = true;
                   break;
                }
                vesselIdx++;}
            if (!foundVessel) {
                Console.WriteLine("[kRPC testing tools]: Failed to find vessel to switch to");
                return;
            }
            AutoSwitchVessel.Vessel = vesselIdx;

            // Load the game
            HighLogic.CurrentGame.startScene = GameScenes.SPACECENTER;
            HighLogic.SaveFolder = Game;
            HighLogic.CurrentGame.Start();
        }
    }
}
