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

using System.Collections;
using System.Collections.Generic;
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
        static string craft;
        static string craftDirectory = "VAB";
        static string launchSite = "LaunchPad";

        /// <summary>
        /// Set the craft to launch after the save has loaded.
        /// </summary>
        public static void SetCraftLaunch(string name, string directory, string site)
        {
            craft = name;
            craftDirectory = string.IsNullOrEmpty(directory) ? "VAB" : directory;
            launchSite = string.IsNullOrEmpty(site) ? "LaunchPad" : site;
            vessel = -1;
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
        // Unity's deprecated OnLevelWasLoaded(int) magic message, which would
        // otherwise log a spurious type error on every run.
        void OnGUIReady(GameScenes scene)
        {
            if (scene != GameScenes.SPACECENTER)
                return;
            if (!string.IsNullOrEmpty(craft))
                StartCoroutine(CallbackUtil.DelayedCallback(15, LaunchCraft));
            else if (Vessel >= 0)
                StartCoroutine(CallbackUtil.DelayedCallback(15, SwitchVessel));
        }

        void LaunchCraft()
        {
            // Snapshot and clear the request up front so a repeated scene load
            // cannot launch the same craft twice.
            var name = craft;
            var directory = craftDirectory;
            var site = launchSite;
            craft = null;
            craftDirectory = "VAB";
            launchSite = "LaunchPad";
            if (string.IsNullOrEmpty(name))
                return;
            Debug.Log(
                "[kRPC testing tools]: Launching " + directory +
                " craft \"" + name + "\" at " + site);
            StartCoroutine(LaunchCraftCoroutine(name, directory, site));
        }

        static IEnumerator LaunchCraftCoroutine(string name, string directory, string site)
        {
            var result = new KRPC.SpaceCenter.Services.SpaceCenter.LaunchResult();
            yield return KRPC.SpaceCenter.Services.SpaceCenter.LaunchVesselCoroutine(
                directory, name, site, new List<string>(), result);
            if (!string.IsNullOrEmpty(result.Error))
                Debug.LogError(
                    "[kRPC testing tools]: Failed to launch craft \"" + name + "\": " + result.Error);
        }

        static void SwitchVessel()
        {
            Debug.Log("[kRPC testing tools]: Switching to active vessel");
            FlightDriver.StartAndFocusVessel(AutoLoadGame.Save, Vessel);
            Vessel = -1;
        }
    }
}
