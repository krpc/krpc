#pragma warning disable 0618

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using KRPC.Continuations;
using KRPC.Service.Attributes;
using UnityEngine;

namespace TestingTools
{
    /// <summary>
    /// kRPC testing tools.
    /// </summary>
    [KRPCService]
    public static class TestingTools
    {
        /// <summary>
        /// Get the name of the current save game.
        /// </summary>
        [KRPCProperty]
        public static string CurrentSave {
            get {
                var title = HighLogic.CurrentGame.Title.Split (' ');
                var name = title.Take (title.Length - 1).ToArray ();
                return String.Join (" ", name);
            }
        }

        /// <summary>
        /// Load an existing save game.
        /// </summary>
        [KRPCProcedure]
        public static void LoadSave (string directory, string name)
        {
            HighLogic.SaveFolder = directory;
            var game = GamePersistence.LoadGame (name, HighLogic.SaveFolder, true, false);
            if (game == null || game.flightState == null || !game.compatible)
                throw new ArgumentException ("Failed to load save '" + name + "'");
            FlightDriver.StartAndFocusVessel (game, game.flightState.activeVesselIdx);
            throw new YieldException (new ParameterizedContinuationVoid<int> (WaitForVesselSwitch, 0));
        }

        /// <summary>
        /// Remove all vessels except the active vessel.
        /// </summary>
        [KRPCProcedure]
        public static void RemoveOtherVessels ()
        {
            var vessels = FlightGlobals.Vessels.Where (v => v != FlightGlobals.ActiveVessel).ToList ();
            foreach (var vessel in vessels)
                vessel.Die ();
        }

        /// <summary>
        /// Set the orbit of the active vessel to a circular orbit.
        /// </summary>
        /// <param name="body">Body to orbit.</param>
        /// <param name="altitude">Altitude to orbit at, in meters above MSL.</param>
        [KRPCProcedure]
        public static void SetCircularOrbit (string body, double altitude)
        {
            var celestialBody = FlightGlobals.Bodies.First (b => b.bodyName == body);
            var semiMajorAxis = celestialBody.Radius + altitude;
            OrbitTools.OrbitDriver.orbit.Set (OrbitTools.CreateOrbit (celestialBody, semiMajorAxis, 0, 0, 0, 0, 0, 0));
            throw new YieldException (new ParameterizedContinuationVoid<int> (WaitForVesselSwitch, 0));
        }

        /// <summary>
        /// Set the orbit of the active vessel.
        /// </summary>
        [KRPCProcedure]
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidLongParameterListsRule")]
        public static void SetOrbit (string body, double semiMajorAxis, double eccentricity, double inclination, double longitudeOfAscendingNode, double argumentOfPeriapsis, double meanAnomalyAtEpoch, double epoch)
        {
            var celestialBody = FlightGlobals.Bodies.First (b => b.bodyName == body);
            OrbitTools.OrbitDriver.orbit.Set (OrbitTools.CreateOrbit (celestialBody, semiMajorAxis, eccentricity, inclination, longitudeOfAscendingNode, argumentOfPeriapsis, meanAnomalyAtEpoch, epoch));
            throw new YieldException (new ParameterizedContinuationVoid<int> (WaitForVesselSwitch, 0));
        }

        static Quaternion ZeroRotation {
            get {
                var vessel = FlightGlobals.ActiveVessel;
                var vesselCoM = vessel.CoM;
                var right = vesselCoM - vessel.mainBody.position;
                var northPole = vessel.mainBody.position + ((Vector3d)vessel.mainBody.transform.up) * vessel.mainBody.Radius - vesselCoM;
                northPole.Normalize ();
                var up = Vector3.Exclude (right, northPole);
                var forward = Vector3.Cross (right, northPole);
                Vector3.OrthoNormalize (ref forward, ref up);
                var rotation = Quaternion.LookRotation (forward, up);
                return Quaternion.AngleAxis (90, new Vector3 (0, -1, 0)) * rotation;
            }
        }

        /// <summary>
        /// Clear the rotational velocity of the given vessel.
        /// </summary>
        /// <param name="vessel">Vessel.</param>
        [KRPCProcedure]
        public static void ClearRotation (KRPC.SpaceCenter.Services.Vessel vessel = null)
        {
            Vessel internalVessel = vessel == null ? FlightGlobals.ActiveVessel : vessel.InternalVessel;
            internalVessel.SetRotation (ZeroRotation);
        }

        /// <summary>
        /// Apply a rotation to the given vessel.
        /// </summary>
        [KRPCProcedure]
        public static void ApplyRotation (float angle, KRPC.Utils.Tuple<float,float,float> axis, KRPC.SpaceCenter.Services.Vessel vessel = null)
        {
            Vessel internalVessel = vessel == null ? FlightGlobals.ActiveVessel : vessel.InternalVessel;
            var axisVector = new Vector3 (axis.Item1, axis.Item2, axis.Item3).normalized;
            var rotation = internalVessel.transform.rotation * Quaternion.AngleAxis (angle, axisVector);
            internalVessel.SetRotation (rotation);
        }

        static void WaitForVesselSwitch (int tick)
        {
            if (FlightGlobals.ActiveVessel.packed)
                throw new YieldException (new ParameterizedContinuationVoid<int> (WaitForVesselSwitch, 0));
            if (tick < 10)
                throw new YieldException (new ParameterizedContinuationVoid<int> (WaitForVesselSwitch, tick + 1));
        }
    }
}
