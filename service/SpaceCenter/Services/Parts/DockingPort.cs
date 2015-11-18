using System;
using System.Linq;
using KRPC.Continuations;
using KRPC.Service.Attributes;
using KRPC.Utils;
using KRPCSpaceCenter.ExtensionMethods;
using Tuple3 = KRPC.Utils.Tuple<double, double, double>;
using Tuple4 = KRPC.Utils.Tuple<double, double, double, double>;

namespace KRPCSpaceCenter.Services.Parts
{
    /// <summary>
    /// See <see cref="DockingPort.State"/>.
    /// </summary>
    [KRPCEnum (Service = "SpaceCenter")]
    public enum DockingPortState
    {
        /// <summary>
        /// The docking port is ready to dock to another docking port.
        /// </summary>
        Ready,
        /// <summary>
        /// The docking port is docked to another docking port, or docked to
        /// another part (from the VAB/SPH).
        /// </summary>
        Docked,
        /// <summary>
        /// The docking port is very close to another docking port,
        /// but has not docked. It is using magnetic force to acquire a solid dock.
        /// </summary>
        Docking,
        /// <summary>
        /// The docking port has just been undocked from another docking port,
        /// and is disabled until it moves away by a sufficient distance
        /// (<see cref="DockingPort.ReengageDistance"/>).
        /// </summary>
        Undocking,
        /// <summary>
        /// The docking port has a shield, and the shield is closed.
        /// </summary>
        Shielded,
        /// <summary>
        /// The docking ports shield is currently opening/closing.
        /// </summary>
        Moving
    }

    /// <summary>
    /// Obtained by calling <see cref="Part.DockingPort"/>
    /// </summary>
    [KRPCClass (Service = "SpaceCenter")]
    public sealed class DockingPort : Equatable<DockingPort>
    {
        readonly Part part;
        readonly ModuleDockingNode port;
        readonly ModuleAnimateGeneric shield;

        readonly PartModule portNameModule;
        readonly BaseField portNameField;

        internal DockingPort (Part part)
        {
            this.part = part;
            port = part.InternalPart.Module<ModuleDockingNode> ();
            shield = part.InternalPart.Module<ModuleAnimateGeneric> ();
            foreach (PartModule module in part.InternalPart.Modules) {
                if (module.moduleName == "ModuleDockingNodeNamed") {
                    portNameModule = module;
                    portNameField = module.Fields.Cast<BaseField> ().FirstOrDefault (x => x.guiName == "Port Name");
                    break;
                }
            }
            if (port == null)
                throw new ArgumentException ("Part does not have a ModuleDockingNode PartModule");
        }

        public override bool Equals (DockingPort obj)
        {
            return part == obj.part;
        }

        public override int GetHashCode ()
        {
            return part.GetHashCode ();
        }

        internal ModuleDockingNode InternalPort {
            get { return port; }
        }

        /// <summary>
        /// The part object for this docking port.
        /// </summary>
        [KRPCProperty]
        public Part Part {
            get { return part; }
        }

        /// <summary>
        /// The port name of the docking port. This is the name of the port that can be set
        /// in the right click menu, when the
        /// <a href="http://forum.kerbalspaceprogram.com/threads/43901">Docking Port Alignment Indicator</a>
        /// mod is installed. If this mod is not installed, returns the title of the part
        /// (<see cref="Part.Title"/>).
        /// </summary>
        [KRPCProperty]
        public string Name {
            get { return portNameField == null ? part.Title : portNameField.GetValue (portNameModule).ToString (); }
            set {
                if (portNameField == null)
                    throw new InvalidOperationException ("Docking port does not have a 'Port Name' field");
                portNameField.SetValue (Convert.ChangeType (value, portNameField.FieldInfo.FieldType), portNameModule);
            }
        }

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
                } else {
                    // This docking port is not "Docked", but check if it is connected to another docking port
                    // If it is, this docking port is "Docking"
                    return dockedPort != null ? DockingPortState.Docking : state;
                }
            }
        }

        /// <summary>
        /// The part that this docking port is docked to. Returns <c>null</c> if this
        /// docking port is not docked to anything.
        /// </summary>
        [KRPCProperty]
        public Part DockedPart {
            get {
                var dockedPart = GetDockedPart;
                return dockedPart != null ? new Part (dockedPart) : null;
            }
        }

        /// <summary>
        /// Undocks the docking port and returns the vessel that was undocked from.
        ///
        /// After undocking, the active vessel may change (<see cref="SpaceCenter.ActiveVessel"/>).
        /// This method can be called for either docking port in a docked pair - both calls will have the same
        /// effect. Returns <c>null</c> if the docking port is not docked to anything.
        /// </summary>
        [KRPCMethod]
        public Vessel Undock ()
        {
            // Don't do anything if we're not docked
            if (State != DockingPortState.Docked)
                return null;

            var dockedPart = GetDockedPart;
            var dockedPort = dockedPart != null ? dockedPart.Module<ModuleDockingNode> () : null;
            var preActiveVessel = FlightGlobals.ActiveVessel;
            var preVessels = FlightGlobals.Vessels.ToArray ();

            // Try calling "Decouple Node" or "Undock" on this part and on the port we are docked to, if any
            if (InvokeEvent (port, "Decouple Node") || InvokeEvent (port, "Undock") ||
                (dockedPort != null && (InvokeEvent (dockedPort, "Decouple Node") || InvokeEvent (dockedPort, "Undock")))) {
                return PostUndock (preActiveVessel, preVessels);
            }

            // Failed to undock
            throw new InvalidOperationException ("Failed to undock, a suitable event to trigger was not found");
        }

        Vessel PostUndock (global::Vessel preActiveVessel, global::Vessel[] preVessels, int wait = 0)
        {
            //FIXME: sometimes after undocking, KSP changes it's mind as to what the active vessel is, so we wait for 10 frames before getting the active vessel
            // Wait while the port is docked
            if (wait < 10 || State == DockingPortState.Docked)
                throw new YieldException (new ParameterizedContinuation<Vessel, global::Vessel, global::Vessel[], int> (PostUndock, preActiveVessel, preVessels, wait + 1));
            // Return the vessel that was undocked from
            var activeVessel = FlightGlobals.ActiveVessel;
            var newVessel = FlightGlobals.Vessels.Except (preVessels).Select (vessel => new Vessel (vessel)).Single ();
            return activeVessel == preActiveVessel ? newVessel : new Vessel (preActiveVessel);
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
                if (!value && Shielded)
                    shield.Events.First (e => e.guiName == shield.startEventGUIName).Invoke ();
                // Close the shield if we are not shielded, and value is true
                if (value && !Shielded)
                    shield.Events.First (e => e.guiName == shield.endEventGUIName).Invoke ();
            }
        }

        /// <summary>
        /// The position of the docking port in the given reference frame.
        /// </summary>
        [KRPCMethod]
        public Tuple3 Position (ReferenceFrame referenceFrame)
        {
            return referenceFrame.PositionFromWorldSpace (port.nodeTransform.position).ToTuple ();
        }

        /// <summary>
        /// The direction that docking port points in, in the given reference frame.
        /// </summary>
        [KRPCMethod]
        public Tuple3 Direction (ReferenceFrame referenceFrame)
        {
            return referenceFrame.DirectionFromWorldSpace (port.nodeTransform.forward).ToTuple ();
        }

        /// <summary>
        /// The rotation of the docking port, in the given reference frame.
        /// </summary>
        [KRPCMethod]
        public Tuple4 Rotation (ReferenceFrame referenceFrame)
        {
            return referenceFrame.RotationToWorldSpace (port.nodeTransform.rotation).ToTuple ();
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
                if (port.state == "PreAttached") {
                    // If the port is "PreAttached" (docked from the VAB/SPH) find the connected part
                    // If the docking port points at an axially connected child part, return it
                    var child = part.InternalPart.children.SingleOrDefault (p => p.attachMode == AttachModes.STACK);
                    if (child != null && PointsTowards (child))
                        return child;
                    // If the docking port points at an axially connected parent part, return it
                    var parent = part.InternalPart.attachMode == AttachModes.STACK ? part.InternalPart.parent : null;
                    if (parent != null && PointsTowards (parent))
                        return parent;
                    throw new InvalidOperationException ("Docking port is 'PreAttached' but is not docked to any parts");
                } else {
                    // Find the port that is "Docked" to this port, if any
                    return part.InternalPart.vessel [port.dockedPartUId];
                }
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
        static DockingPortState IndividualState (ModuleDockingNode port)
        {
            var state = port.state;
            if (state == "Ready")
                return DockingPortState.Ready;
            else if (state.StartsWith ("Docked") || state == "PreAttached")
                return DockingPortState.Docked;
            else if (state == "Acquire")
                return DockingPortState.Docking;
            else if (state == "Disengage")
                return DockingPortState.Undocking;
            else if (state == "Disabled") {
                var shield = port.part.Module<ModuleAnimateGeneric> ();
                if (shield == null)
                    throw new InvalidOperationException ("Docking port state is '" + port.state + "', but it does not have a shield!");
                if (shield.status.StartsWith ("Moving"))
                    return DockingPortState.Moving;
                else
                    return DockingPortState.Shielded;
            } else
                throw new ArgumentException ("Unknown docking port state '" + port.state + "'");
        }

        /// <summary>
        /// Try invoking a named event for a docking port. Returns true if an event is found and invoked.
        /// </summary>
        static bool InvokeEvent (PartModule port, string eventName)
        {
            var e = port.Events
                .Where (x => x != null && (HighLogic.LoadedSceneIsEditor ? x.guiActiveEditor : x.guiActive) && x.active)
                .FirstOrDefault (x => x.guiName == eventName);
            if (e != null) {
                e.Invoke ();
                return true;
            } else {
                return false;
            }
        }
    }
}
