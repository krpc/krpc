using System;
using System.Collections.Generic;
using System.Linq;
using KRPC.Service;
using KRPC.Service.Attributes;
using KRPC.Utils;

namespace KRPC.RasterPropMonitor
{
    /// <summary>
    /// This service provides the IVA screens, variables and external cameras of
    /// <a href="https://github.com/FirstPersonKSP/RasterPropMonitor">Raster Prop Monitor</a>.
    /// </summary>
    /// <remarks>
    /// A persistent variable is a number a script and the props share: a prop shows it, and a
    /// switch or numeric input sets it. Raster Prop Monitor updates the variables that depend
    /// on the vessel only for the active vessel.
    /// </remarks>
    [KRPCService (Id = ServiceInfo.Id, GameScene = GameScene.All)]
    [RequiresMod (typeof (API))]
    public static class RasterPropMonitor
    {
        /// <summary>
        /// Whether Raster Prop Monitor is installed.
        /// </summary>
        [KRPCProperty]
        public static bool Available {
            get { return ModAPI.Get<API> ().IsAvailable; }
        }

        /// <summary>
        /// The computer of a part.
        /// Throws an exception if the part has no Raster Prop Monitor computer.
        /// </summary>
        /// <param name="part">The part.</param>
        [KRPCProcedure (GameScene = GameScene.Flight)]
        public static Computer Computer (SpaceCenter.Services.Parts.Part part)
        {
            if (ReferenceEquals (part, null))
                throw new ArgumentNullException (nameof (part));
            return new Computer (part);
        }

        /// <summary>
        /// The computers of the parts of a vessel.
        /// </summary>
        /// <param name="vessel">The vessel.</param>
        [KRPCProcedure (GameScene = GameScene.Flight)]
        public static IList<Computer> Computers (SpaceCenter.Services.Vessel vessel)
        {
            if (ReferenceEquals (vessel, null))
                throw new ArgumentNullException (nameof (vessel));
            return API.Computers (vessel.InternalVessel)
                .Select (computer => new Computer (new SpaceCenter.Services.Parts.Part (computer.part)))
                .ToList ();
        }

        /// <summary>
        /// The monitors in the IVAs of a vessel's parts.
        /// Throws an exception if the vessel is not the active vessel.
        /// </summary>
        /// <param name="vessel">The active vessel.</param>
        /// <remarks>
        /// KSP builds IVAs only for the active vessel.
        /// </remarks>
        [KRPCProcedure (GameScene = GameScene.Flight)]
        public static IList<Monitor> Monitors (SpaceCenter.Services.Vessel vessel)
        {
            if (ReferenceEquals (vessel, null))
                throw new ArgumentNullException (nameof (vessel));
            if (vessel.InternalVessel != FlightGlobals.ActiveVessel)
                throw new ArgumentException ("The vessel is not the active vessel", nameof (vessel));
            return Monitor.Of (vessel.InternalVessel);
        }

        /// <summary>
        /// The display of a vessel with the given name, which every page whose
        /// <c>KRPCDisplay</c> handler names it shows.
        /// </summary>
        /// <param name="vessel">The vessel.</param>
        /// <param name="name">The name of the display.</param>
        [KRPCProcedure (GameScene = GameScene.Flight)]
        public static Display Display (SpaceCenter.Services.Vessel vessel, string name)
        {
            if (ReferenceEquals (vessel, null))
                throw new ArgumentNullException (nameof (vessel));
            if (string.IsNullOrEmpty (name) || name.Trim ().Length == 0)
                throw new ArgumentException ("The name is empty", nameof (name));
            return new Display (DisplayState.Of (vessel.InternalVessel.id, name.Trim ()));
        }

        /// <summary>
        /// The external cameras on a vessel's parts.
        /// </summary>
        /// <param name="vessel">The vessel.</param>
        [KRPCProcedure (GameScene = GameScene.Flight)]
        public static IList<ExternalCamera> Cameras (SpaceCenter.Services.Vessel vessel)
        {
            if (ReferenceEquals (vessel, null))
                throw new ArgumentNullException (nameof (vessel));
            return ExternalCamera.Of (vessel.InternalVessel);
        }

        // A computer on the vessel, through which RPM reaches the vessel's persistent variables
        static PartModule VesselComputer (SpaceCenter.Services.Vessel vessel)
        {
            if (ReferenceEquals (vessel, null))
                throw new ArgumentNullException (nameof (vessel));
            var computer = API.Computers (vessel.InternalVessel).FirstOrDefault ();
            if (computer == null)
                throw new ArgumentException ("The vessel has no Raster Prop Monitor computer", nameof (vessel));
            return computer;
        }

        /// <summary>
        /// The value of a persistent variable of a vessel, or <c>null</c> if it is not set.
        /// Throws an exception if the vessel has no Raster Prop Monitor computer.
        /// </summary>
        /// <param name="vessel">The vessel.</param>
        /// <param name="name">The name of the variable, without the <c>PERSISTENT_</c> prefix.</param>
        [KRPCProcedure (GameScene = GameScene.Flight)]
        public static double? GlobalPersistentVariable (SpaceCenter.Services.Vessel vessel, string name)
        {
            return API.PersistentVariable (VesselComputer (vessel), name, true);
        }

        /// <summary>
        /// Set a persistent variable of a vessel. The props that show it update.
        /// Throws an exception if the vessel has no Raster Prop Monitor computer.
        /// </summary>
        /// <param name="vessel">The vessel.</param>
        /// <param name="name">The name of the variable, without the <c>PERSISTENT_</c> prefix.</param>
        /// <param name="value">The value.</param>
        [KRPCProcedure (GameScene = GameScene.Flight)]
        public static void SetGlobalPersistentVariable (SpaceCenter.Services.Vessel vessel, string name, double value)
        {
            API.SetPersistentVariable (VesselComputer (vessel), name, value, true);
        }

        /// <summary>
        /// Remove a persistent variable of a vessel. The props that show it update.
        /// Throws an exception if the vessel has no Raster Prop Monitor computer.
        /// </summary>
        /// <param name="vessel">The vessel.</param>
        /// <param name="name">The name of the variable, without the <c>PERSISTENT_</c> prefix.</param>
        [KRPCProcedure (GameScene = GameScene.Flight)]
        public static void RemoveGlobalPersistentVariable (SpaceCenter.Services.Vessel vessel, string name)
        {
            API.RemovePersistentVariable (VesselComputer (vessel), name, true);
        }

        /// <summary>
        /// An event that fires when <see cref="GlobalPersistentVariable"/> changes.
        /// The event lasts until it is removed.
        /// Throws an exception if the vessel has no Raster Prop Monitor computer.
        /// </summary>
        /// <param name="vessel">The vessel.</param>
        /// <param name="name">The name of the variable, without the <c>PERSISTENT_</c> prefix.</param>
        [KRPCProcedure (GameScene = GameScene.Flight)]
        public static KRPC.Service.Messages.Event GlobalPersistentVariableChanged (SpaceCenter.Services.Vessel vessel, string name)
        {
            VesselComputer (vessel);
            return ChangedEvent (() => GlobalPersistentVariable (vessel, name));
        }

        // An event that fires on each update in which the value differs from the last one
        internal static KRPC.Service.Messages.Event ChangedEvent (Func<double?> value)
        {
            var last = value ();
            return new Event (() => {
                var current = value ();
                if (Equals (current, last))
                    return false;
                last = current;
                return true;
            }).Message;
        }
    }
}
