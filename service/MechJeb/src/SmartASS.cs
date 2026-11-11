using System;
using KRPC.MechJeb.Util;
using KRPC.Service.Attributes;

namespace KRPC.MechJeb
{
    /// <summary>
    /// The Smart A.S.S. window, which points the vessel in a chosen direction.
    /// </summary>
    [KRPCClass (Service = "MechJeb")]
    public sealed class SmartASS : DisplayModule
    {
        // MechJeb's names of the attitude references, in the order of AttitudeReference
        static readonly string [] attitudeReferenceNames = {
            "INERTIAL", "ORBIT", "ORBIT_HORIZONTAL", "SURFACE_NORTH", "SURFACE_VELOCITY", "TARGET",
            "RELATIVE_VELOCITY", "TARGET_ORIENTATION", "MANEUVER_NODE", "SUN", "SURFACE_HORIZONTAL"
        };

        static Value<int> mode;
        static Value<int> target;
        static Value<bool> forceRol;
        static Value<bool> forcePitch;
        static Value<bool> forceYaw;
        static Value<double> srfHdg;
        static Value<double> srfPit;
        static Value<double> srfRol;
        static Value<double> srfVelYaw;
        static Value<double> srfVelPit;
        static Value<double> srfVelRol;
        static Value<int> advReference;
        static Value<int> advDirection;
        static Action<object, bool> engage;
        static int [] attitudeReferences;

        internal static new void Load ()
        {
            var type = Reflection.Type ("MuMech.MechJebModuleSmartASS");
            mode = Reflection.Field<int> (type, "mode");
            target = Reflection.Field<int> (type, "target");
            forceRol = Reflection.Field<bool> (type, "forceRol");
            forcePitch = Reflection.Field<bool> (type, "forcePitch");
            forceYaw = Reflection.Field<bool> (type, "forceYaw");
            srfHdg = Reflection.EditableDouble (type, "srfHdg");
            srfPit = Reflection.EditableDouble (type, "srfPit");
            srfRol = Reflection.EditableDouble (type, "srfRol");
            srfVelYaw = Reflection.EditableDouble (type, "srfVelYaw");
            srfVelPit = Reflection.EditableDouble (type, "srfVelPit");
            srfVelRol = Reflection.EditableDouble (type, "srfVelRol");
            advReference = Reflection.Field<int> (type, "advReference");
            advDirection = Reflection.Field<int> (type, "advDirection");
            engage = Reflection.Method<Action<object, bool>> (type, "Engage");
            var reference = Reflection.Type ("MuMech.AttitudeReference");
            attitudeReferences = reference == null ? null :
                Array.ConvertAll (attitudeReferenceNames, name => (int)Enum.Parse (reference, name));
        }

        internal SmartASS () : base ("MechJebModuleSmartASS")
        {
        }

        /// <summary>
        /// The mode of the window, which selects its buttons and nothing else.
        /// Throws an exception if set to <see cref="SmartASSInterfaceMode.Automatic"/>.
        /// </summary>
        [KRPCProperty]
        public SmartASSInterfaceMode InterfaceMode {
            get { return (SmartASSInterfaceMode)mode.Get (Instance); }
            set {
                if (value == SmartASSInterfaceMode.Automatic)
                    throw new MJServiceException ("Cannot set SmartASSInterfaceMode to Automatic");
                mode.Set (Instance, (int)value);
            }
        }

        /// <summary>
        /// The autopilot mode. Throws an exception if set to
        /// <see cref="SmartASSAutopilotMode.Automatic"/>.
        /// </summary>
        /// <remarks>Call <see cref="Update"/> to apply a change.</remarks>
        [KRPCProperty]
        public SmartASSAutopilotMode AutopilotMode {
            get { return (SmartASSAutopilotMode)target.Get (Instance); }
            set {
                if (value == SmartASSAutopilotMode.Automatic)
                    throw new MJServiceException ("Cannot set SmartASSAutopilotMode to Automatic");
                target.Set (Instance, (int)value);
            }
        }

        /// <summary>
        /// Whether the autopilot controls the yaw in the <see cref="SmartASSAutopilotMode.Surface"/>,
        /// <see cref="SmartASSAutopilotMode.SurfacePrograde"/> and
        /// <see cref="SmartASSAutopilotMode.SurfaceRetrograde"/> modes.
        /// </summary>
        [KRPCProperty]
        public bool ForceYaw {
            get { return forceYaw.Get (Instance); }
            set { forceYaw.Set (Instance, value); }
        }

        /// <summary>
        /// Whether the autopilot controls the pitch in the <see cref="SmartASSAutopilotMode.Surface"/>,
        /// <see cref="SmartASSAutopilotMode.SurfacePrograde"/> and
        /// <see cref="SmartASSAutopilotMode.SurfaceRetrograde"/> modes.
        /// </summary>
        [KRPCProperty]
        public bool ForcePitch {
            get { return forcePitch.Get (Instance); }
            set { forcePitch.Set (Instance, value); }
        }

        /// <summary>
        /// Whether the autopilot controls the roll.
        /// </summary>
        [KRPCProperty]
        public bool ForceRoll {
            get { return forceRol.Get (Instance); }
            set { forceRol.Set (Instance, value); }
        }

        /// <summary>
        /// The heading, in degrees.
        /// </summary>
        /// <remarks>Applies in the <see cref="SmartASSAutopilotMode.Surface"/> mode.</remarks>
        [KRPCProperty]
        public double SurfaceHeading {
            get { return srfHdg.Get (Instance); }
            set { srfHdg.Set (Instance, value); }
        }

        /// <summary>
        /// The pitch above the horizon, in degrees. 90 is straight up.
        /// </summary>
        /// <remarks>Applies in the <see cref="SmartASSAutopilotMode.Surface"/> mode.</remarks>
        [KRPCProperty]
        public double SurfacePitch {
            get { return srfPit.Get (Instance); }
            set { srfPit.Set (Instance, value); }
        }

        /// <summary>
        /// The roll, in degrees. 0 is top side up.
        /// </summary>
        /// <remarks>Applies in the <see cref="SmartASSAutopilotMode.Surface"/> mode.</remarks>
        [KRPCProperty]
        public double SurfaceRoll {
            get { return srfRol.Get (Instance); }
            set { srfRol.Set (Instance, value); }
        }

        /// <summary>
        /// The yaw from the surface velocity, in degrees.
        /// </summary>
        /// <remarks>
        /// Applies in the <see cref="SmartASSAutopilotMode.SurfacePrograde"/> and
        /// <see cref="SmartASSAutopilotMode.SurfaceRetrograde"/> modes.
        /// </remarks>
        [KRPCProperty]
        public double SurfaceVelYaw {
            get { return srfVelYaw.Get (Instance); }
            set { srfVelYaw.Set (Instance, value); }
        }

        /// <summary>
        /// The pitch from the surface velocity, in degrees.
        /// </summary>
        /// <remarks>
        /// Applies in the <see cref="SmartASSAutopilotMode.SurfacePrograde"/> and
        /// <see cref="SmartASSAutopilotMode.SurfaceRetrograde"/> modes.
        /// </remarks>
        [KRPCProperty]
        public double SurfaceVelPitch {
            get { return srfVelPit.Get (Instance); }
            set { srfVelPit.Set (Instance, value); }
        }

        /// <summary>
        /// The roll, in degrees. 0 is top side up.
        /// </summary>
        /// <remarks>
        /// Applies in the <see cref="SmartASSAutopilotMode.SurfacePrograde"/> and
        /// <see cref="SmartASSAutopilotMode.SurfaceRetrograde"/> modes.
        /// </remarks>
        [KRPCProperty]
        public double SurfaceVelRoll {
            get { return srfVelRol.Get (Instance); }
            set { srfVelRol.Set (Instance, value); }
        }

        /// <summary>
        /// The reference frame of <see cref="AdvancedDirection"/>. Throws an exception if
        /// MechJeb uses a reference this enumeration lacks.
        /// </summary>
        /// <remarks>Applies in the <see cref="SmartASSAutopilotMode.Advanced"/> mode.</remarks>
        [KRPCProperty]
        public AttitudeReference AdvancedReference {
            get {
                var index = Array.IndexOf (References, advReference.Get (Instance));
                if (index < 0)
                    throw new MJServiceException ("MechJeb's attitude reference has no equivalent in AttitudeReference");
                return (AttitudeReference)index;
            }
            set { advReference.Set (Instance, References [(int)value]); }
        }

        static int [] References {
            get {
                if (attitudeReferences == null)
                    throw new MissingMemberException ("MechJeb has no AttitudeReference");
                return attitudeReferences;
            }
        }

        /// <summary>
        /// The direction in <see cref="AdvancedReference"/>.
        /// </summary>
        /// <remarks>Applies in the <see cref="SmartASSAutopilotMode.Advanced"/> mode.</remarks>
        [KRPCProperty]
        public Vector6.Direction AdvancedDirection {
            get { return (Vector6.Direction)advDirection.Get (Instance); }
            set { advDirection.Set (Instance, (int)value); }
        }

        /// <summary>
        /// Apply the settings.
        /// </summary>
        /// <param name="resetPID">Whether to reset the attitude controller. Use it only if the
        /// vessel does not follow the new settings.</param>
        [KRPCMethod]
        public void Update (bool resetPID)
        {
            engage (Instance, resetPID);
        }
    }

    /// <summary>
    /// The mode of the Smart A.S.S. window. See <see cref="SmartASS.InterfaceMode"/>.
    /// </summary>
    [KRPCEnum (Service = "MechJeb")]
    public enum SmartASSInterfaceMode
    {
        /// <summary>
        /// The orbital modes.
        /// </summary>
        Orbital,

        /// <summary>
        /// The surface modes.
        /// </summary>
        Surface,

        /// <summary>
        /// The target modes.
        /// </summary>
        Target,

        /// <summary>
        /// The advanced mode.
        /// </summary>
        Advanced,

        /// <summary>
        /// Set by MechJeb's autopilots. Cannot be set.
        /// </summary>
        Automatic
    }

    /// <summary>
    /// The autopilot mode of Smart A.S.S. See <see cref="SmartASS.AutopilotMode"/>.
    /// </summary>
    [KRPCEnum (Service = "MechJeb")]
    public enum SmartASSAutopilotMode
    {
        /// <summary>
        /// Off.
        /// </summary>
        Off,

        /// <summary>
        /// Stop the vessel's rotation.
        /// </summary>
        KillRot,

        /// <summary>
        /// Point at the next maneuver node.
        /// </summary>
        Node,

        /// <summary>
        /// Point in the direction set by <see cref="SmartASS.SurfaceHeading"/>,
        /// <see cref="SmartASS.SurfacePitch"/> and <see cref="SmartASS.SurfaceRoll"/>.
        /// </summary>
        Surface,

        /// <summary>
        /// Point orbital prograde.
        /// </summary>
        Prograde,

        /// <summary>
        /// Point orbital retrograde.
        /// </summary>
        Retrograde,

        /// <summary>
        /// Point orbit normal.
        /// </summary>
        NormalPlus,

        /// <summary>
        /// Point orbit anti-normal.
        /// </summary>
        NormalMinus,

        /// <summary>
        /// Point radially outward.
        /// </summary>
        RadialPlus,

        /// <summary>
        /// Point radially inward.
        /// </summary>
        RadialMinus,

        /// <summary>
        /// Point along the velocity relative to the target.
        /// </summary>
        RelativePlus,

        /// <summary>
        /// Point against the velocity relative to the target.
        /// </summary>
        RelativeMinus,

        /// <summary>
        /// Point at the target.
        /// </summary>
        TargetPlus,

        /// <summary>
        /// Point away from the target.
        /// </summary>
        TargetMinus,

        /// <summary>
        /// Point parallel to the target's orientation. For a docking port, along its axis
        /// and away from it.
        /// </summary>
        ParallelPlus,

        /// <summary>
        /// Point antiparallel to the target's orientation. For a docking port, along its axis
        /// and toward it.
        /// </summary>
        ParallelMinus,

        /// <summary>
        /// Point in the direction set by <see cref="SmartASS.AdvancedReference"/> and
        /// <see cref="SmartASS.AdvancedDirection"/>.
        /// </summary>
        Advanced,

        /// <summary>
        /// Set by MechJeb's autopilots. Cannot be set.
        /// </summary>
        Automatic,

        /// <summary>
        /// Point along the surface velocity.
        /// </summary>
        SurfacePrograde,

        /// <summary>
        /// Point against the surface velocity.
        /// </summary>
        SurfaceRetrograde,

        /// <summary>
        /// Point along the horizontal surface velocity.
        /// </summary>
        HorizontalPlus,

        /// <summary>
        /// Point against the horizontal surface velocity.
        /// </summary>
        HorizontalMinus,

        /// <summary>
        /// Point up, normal to the surface.
        /// </summary>
        VerticalPlus
    }

    /// <summary>
    /// A reference frame of Smart A.S.S. See <see cref="SmartASS.AdvancedReference"/>.
    /// </summary>
    [KRPCEnum (Service = "MechJeb")]
    public enum AttitudeReference
    {
        /// <summary>
        /// The world coordinate system.
        /// </summary>
        Inertial,

        /// <summary>
        /// Forward is prograde, left is normal and up is radial out.
        /// </summary>
        Orbit,

        /// <summary>
        /// Forward is the orbital velocity projected onto the surface, and up is the surface normal.
        /// </summary>
        OrbitHorizontal,

        /// <summary>
        /// Forward is north, left is west and up is the surface normal.
        /// </summary>
        SurfaceNorth,

        /// <summary>
        /// Forward is the surface velocity, and up is the surface normal perpendicular to it.
        /// </summary>
        SurfaceVelocity,

        /// <summary>
        /// Forward is toward the target, and up is the vessel's heading perpendicular to it.
        /// </summary>
        Target,

        /// <summary>
        /// Forward is the velocity relative to the target.
        /// </summary>
        RelativeVelocity,

        /// <summary>
        /// Forward is the direction the target faces, and up is the target's up.
        /// </summary>
        TargetOrientation,

        /// <summary>
        /// Forward is the burn direction of the next maneuver node.
        /// </summary>
        ManeuverNode,

        /// <summary>
        /// Forward is the orbital velocity of the body that orbits the sun, and up is radial
        /// out of that orbit.
        /// </summary>
        Sun,

        /// <summary>
        /// Forward is the horizontal surface velocity, and up is the surface normal.
        /// </summary>
        SurfaceHorizontal
    }
}
