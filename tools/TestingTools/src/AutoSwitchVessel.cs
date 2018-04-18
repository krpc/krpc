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
using UnityEngine;

namespace TestingTools
{
    /// <summary>
    /// Addon that automatically switches to a vessel when a save is loaded
    /// </summary>
    [KSPAddon(KSPAddon.Startup.SpaceCentre, false)]
    public class AutoSwitchVessel : MonoBehaviour
    {
        /// <summary>
        /// The vessel load.
        /// </summary>
        public static int Vessel {
            get { return vessel; }
            set { vessel = value; }
        }

        static int vessel = -1;

        /// <summary>
        /// Initialize the addon.
        /// </summary>
        public void Awake()
        {
            GameEvents.onLevelWasLoadedGUIReady.Add(OnLevelWasLoaded);
        }

        void OnLevelWasLoaded(GameScenes scene)
        {
            if (scene == GameScenes.SPACECENTER && Vessel >= 0)
                StartCoroutine(CallbackUtil.DelayedCallback(15, SwitchVessel));
        }

        [SuppressMessage("Gendarme.Rules.BadPractice", "DisableDebuggingCodeRule")]
        static void SwitchVessel()
        {
            Console.WriteLine("[kRPC testing tools]: Switching to active vessel");
            FlightDriver.StartAndFocusVessel(AutoLoadGame.Save, Vessel);
            Vessel = -1;
        }
    }
}
