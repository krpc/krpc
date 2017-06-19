using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using KRPC.Continuations;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.Utils;
using UnityEngine;
using Tuple3 = KRPC.Utils.Tuple<double, double, double>;
using Tuple4 = KRPC.Utils.Tuple<double, double, double, double>;

namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// A docking port. Obtained by calling <see cref="Part.DockingPort"/>
    /// </summary>
    [KRPCClass (Service = "SpaceCenter")]
    [SuppressMessage ("Gendarme.Rules.Maintainability", "AvoidLackOfCohesionOfMethodsRule")]
    public class DockingPort : Equatable<DockingPort>
    {
        readonly ModuleDockingNode port;
        readonly ModuleAnimateGeneric shield;

        internal static bool Is (Part part)
        {
            return part.InternalPart.HasModule<ModuleDockingNode> ();
        }

        internal DockingPort (Part part)
        {
            Part = part;
            var internalPart = part.InternalPart;
            port = internalPart.Module<ModuleDockingNode> ();
            shield = internalPart.Module<ModuleAnimateGeneric> ();
            if (port == null)
                throw new ArgumentException ("Part is not a docking port");
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (DockingPort other)
        {
            return
            !ReferenceEquals (other, null) &&
            Part == other.Part &&
            port.Equals (other.port) &&
            (shield == other.shield || shield.Equals (other.shield));
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            int hash = Part.GetHashCode () ^ port.GetHashCode ();
            if (shield != null)
                hash ^= shield.GetHashCode ();
            return hash;
        }

        /// <summary>
        /// The KSP docking node object.
        /// </summary>
        public ModuleDockingNode InternalPort {
            get { return port; }
        }

        /// <summary>
        /// The part object for this docking port.
        /// </summary>
        [KRPCProperty]
        public Part Part { get; private set; }

        /// <summary>
        /// The current state of the docking port.
        /// </summary>
        [KRPCProperty]
        public DockingPortState State {
            get {
                // Get the state of this docking port
                var state = IndividualState (port);
                // Get the part and port docked to this docking port, if any
                var dockedPart = GetDockedPart;
                var dockedPort = dockedPart != null ? dockedPart.Module<ModuleDockingNode> () : null;

                if (state == DockingPortState.Docked) {
                    // If this docking port is "Docked" to another docking port, and that docking port is also "Docked", we are docked
                    if (dockedPort != null && IndividualState (dockedPort) == DockingPortState.Docked)
                        return DockingPortState.Docked;
                    // If this docking port is "Docked" and connected to a non-docking port part, we are docked
                    if (dockedPart != null)
                        return DockingPortState.Docked;
                    // Otherwise, this docking port is actually "Docking"
                    return DockingPortState.Docking;
                }
                // This docking port is not "Docked", but check if it is connected to another docking port
                // If it is, this docking port is "Docking"
                return dockedPort != null ? DockingPortState.Docking : state;
            }
        }

        /// <summary>
        /// The part that this docking port is docked to. Returns <c>null</c> if this
        /// docking port is not docked to anything.
        /// </summary>
        [KRPCProperty]
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidCodeDuplicatedInSameClassRule")]
        public Part DockedPart {
            get {
                var dockedPart = GetDockedPart;
                return dockedPart != null ? new Part (dockedPart) : null;
            }
        }

        /// <summary>
        /// Undocks the docking port and returns the new <see cref="Vessel" /> that is created.
        /// This method can be called for either docking port in a docked pair.
        /// Throws an exception if the docking port is not docked to anything.
        /// </summary>
        /// <remarks>
        /// When called, the active vessel may change. It is therefore possible that,
        /// after calling this function, the object(s) returned by previous call(s) to
        /// <see cref="SpaceCenter.ActiveVessel"/> no longer refer to the active vessel.
        /// </remarks>
        [KRPCMethod]
        public Vessel Undock ()
        {
            // Error if we're not docked
            if (State != DockingPortState.Docked)
                throw new InvalidOperationException ("The docking port is not docked");

            var dockedPart = GetDockedPart;
            var dockedPort = dockedPart != null ? dockedPart.Module<ModuleDockingNode> () : null;
            var preVesselIds = FlightGlobals.Vessels.Select (v => v.id).ToList ();

            // Try calling "Decouple Node" or "Undock" on this part and on the port we are docked to, if any
            if (InvokeEvent (port, "Decouple Node") || InvokeEvent (port, "Undock") ||
                (dockedPort != null && (InvokeEvent (dockedPort, "Decouple Node") || InvokeEvent (dockedPort, "Undock")))) {
                return PostUndock (preVesselIds);
            }

            // Failed to undock
            throw new InvalidOperationException ("Failed to undock, a suitable event to trigger was not found");
        }

        Vessel PostUndock (IList<Guid> preVesselIds, int wait = 0)
        {
            // FIXME: sometimes after undocking, KSP changes it's mind as to what the active vessel is, so we wait for 10 frames before getting the active vessel
            // Wait while the port is docked
            if (wait < 10 || State == DockingPortState.Docked)
                throw new YieldException (new ParameterizedContinuation<Vessel, IList<Guid>, int> (PostUndock, preVesselIds, wait + 1));
            // Return the newly created vessel
            return new Vessel (FlightGlobals.Vessels.Select (v => v.id).Except (preVesselIds).Single ());
        }

        /// <summary>
        /// The distance a docking port must move away when it undocks before it
        /// becomes ready to dock with another port, in meters.
        /// </summary>
        [KRPCProperty]
        public float ReengageDistance {
            get { return port.minDistanceToReEngage; }
        }

        /// <summary>
        /// Whether the docking port has a shield.
        /// </summary>
        [KRPCProperty]
        public bool HasShield {
            get { return shield != null; }
        }

        /// <summary>
        /// The state of the docking ports shield, if it has one.
        ///
        /// Returns <c>true</c> if the docking port has a shield, and the shield is
        /// closed. Otherwise returns <c>false</c>. When set to <c>true</c>, the shield is
        /// closed, and when set to <c>false</c> the shield is opened. If the docking
        /// port does not have a shield, setting this attribute has no effect.
        /// </summary>
        [KRPCProperty]
        public bool Shielded {
            get { return HasShield && State == DockingPortState.Shielded; }
            set {
                // Don't do anything if there is no shield
                if (!HasShield)
                    return;
                var state = State;
                // Don't do anything if we are aren't in a state where the shield can be opened or closed
                if (state != DockingPortState.Shielded && state != DockingPortState.Ready)
                    return;
                // Open the shield if we are shielded, and value is false
                var shielded = Shielded;
                if (!value && shielded)
                    shield.Events.First (e => e.guiName == shield.startEventGUIName).Invoke ();
                // Close the shield if we are not shielded, and value is true
                else if (value && !shielded)
                    shield.Events.First (e => e.guiName == shield.endEventGUIName).Invoke ();
            }
        }

        /// <summary>
        /// The position of the docking port in the given reference frame.
        /// </summary>
        [KRPCMethod]
        public Tuple3 Position (ReferenceFrame referenceFrame)
        {
            if (ReferenceEquals (referenceFrame, null))
                throw new ArgumentNullException (nameof (referenceFrame));
            return referenceFrame.PositionFromWorldSpace (port.nodeTransform.position).ToTuple ();
        }

        /// <summary>
        /// The direction that docking port points in, in the given reference frame.
        /// </summary>
        [KRPCMethod]
        public Tuple3 Direction (ReferenceFrame referenceFrame)
        {
            if (ReferenceEquals (referenceFrame, null))
                throw new ArgumentNullException (nameof (referenceFrame));
            return referenceFrame.DirectionFromWorldSpace (port.nodeTransform.forward).ToTuple ();
        }

        /// <summary>
        /// The rotation of the docking port, in the given reference frame.
        /// </summary>
        [KRPCMethod]
        public Tuple4 Rotation (ReferenceFrame referenceFrame)
        {
            if (ReferenceEquals (referenceFrame, null))
                throw new ArgumentNullException (nameof (referenceFrame));
            return referenceFrame.RotationFromWorldSpace (port.nodeTransform.rotation * Quaternion.Euler (90, 0, 0)).ToTuple ();
        }

        /// <summary>
        /// The reference frame that is fixed relative to this docking port, and
        /// oriented with the port.
        /// <list type="bullet">
        /// <item><description>The origin is at the position of the docking port.</description></item>
        /// <item><description>The axes rotate with the docking port.</description></item>
        /// <item><description>The x-axis points out to the right side of the docking port.</description></item>
        /// <item><description>The y-axis points in the direction the docking port is facing.</description></item>
        /// <item><description>The z-axis points out of the bottom off the docking port.</description></item>
        /// </list>
        /// </summary>
        /// <remarks>
        /// This reference frame is not necessarily equivalent to the reference frame
        /// for the part, returned by <see cref="Part.ReferenceFrame"/>.
        /// </remarks>
        [KRPCProperty]
        public ReferenceFrame ReferenceFrame {
            get { return ReferenceFrame.Object (port); }
        }

        /// <summary>
        /// Gets the part docked to this port, if any. Returns null if there is nothing docked.
        /// </summary>
        global::Part GetDockedPart {
            get {
                var part = Part.InternalPart;
                if (port.state == "PreAttached") {
                    // If the port is "PreAttached" (docked from the VAB/SPH) find the connected part
                    // If the docking port points at an axially connected child part, return it
                    var child = part.children.SingleOrDefault (p => p.attachMode == AttachModes.STACK);
                    if (child != null && PointsTowards (child))
                        return child;
                    // If the docking port points at an axially connected parent part, return it
                    var parent = part.attachMode == AttachModes.STACK ? part.parent : null;
                    if (parent != null && PointsTowards (parent))
                        return parent;
                    throw new InvalidOperationException ("Docking port is 'PreAttached' but is not docked to any parts");
                }
                // Find the port that is "Docked" to this port, if any
                return part.vessel [port.dockedPartUId];
            }
        }

        /// <summary>
        /// Returns true if this docking port points towards the given part
        /// </summary>
        bool PointsTowards (global::Part otherPart)
        {
            return Vector3d.Dot (port.nodeTransform.forward, otherPart.transform.position - port.transform.position) > 0;
        }

        /// <summary>
        /// Gets the state of a docking port. Does not consider the state of an attached docking port.
        /// </summary>
        static DockingPortState IndividualState (ModuleDockingNode node)
        {
            var state = node.state;
            if (state == "Ready")
                return DockingPortState.Ready;
            if (state.StartsWith ("Docked", StringComparison.CurrentCulture) || state == "PreAttached")
                return DockingPortState.Docked;
            if (state.Contains ("Acquire"))
                return DockingPortState.Docking;
            if (state == "Disengage")
                return DockingPortState.Undocking;
            if (state == "Disabled") {
                var shieldModule = node.part.Module<ModuleAnimateGeneric> ();
                if (shieldModule == null)
                    throw new InvalidOperationException ("Docking port state is '" + node.state + "', but it does not have a shield!");
                return shieldModule.status.StartsWith ("Moving", StringComparison.CurrentCulture) ? DockingPortState.Moving : DockingPortState.Shielded;
            }
            throw new ArgumentException ("Unknown docking port state '" + node.state + "'");
        }

        /// <summary>
        /// Try invoking a named event for a docking port. Returns true if an event is found and invoked.
        /// </summary>
        // TODO: move to PartModule extension methods
        static bool InvokeEvent (PartModule module, string eventName)
        {
            var e = module.Events
                .Where (x => x != null && (HighLogic.LoadedSceneIsEditor ? x.guiActiveEditor : x.guiActive) && x.active)
                .FirstOrDefault (x => x.guiName == eventName);
            if (e != null) {
                e.Invoke ();
                return true;
            }
            return false;
        }
    }
}
