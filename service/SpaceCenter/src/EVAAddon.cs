using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace KRPC.SpaceCenter
{
    /// <summary>
    /// Addon that walks a kerbal on EVA, and flies its jetpack, from the control inputs
    /// set through <see cref="Services.Control"/>. The translation inputs move the kerbal
    /// and the rotation inputs turn it, so the same inputs that fly a craft also drive a
    /// kerbal.
    /// </summary>
    /// <remarks>
    /// A kerbal is moved by its <c>KerbalEVA</c> module, which takes no control input from
    /// the vessel's flight control state. Its <c>FixedUpdate</c> zeroes a set of movement
    /// command fields, fills them from the keyboard and joystick, and then runs the state
    /// machine that consumes them and does the moving - all in one call. A command written
    /// from any other <c>FixedUpdate</c> is therefore either zeroed before the state
    /// machine reads it or written after it has already run.
    ///
    /// The commands are instead applied from inside that window, by prepending a callback
    /// to the <c>OnFixedUpdate</c> of every state of the kerbal's state machine.
    /// <c>KerbalFSM</c> runs the current state's <c>OnFixedUpdate</c> before checking that
    /// state's transitions, so the callback runs after the module has taken keyboard input
    /// and before anything has read the result. The command fields are protected, so they
    /// are read and written by reflection.
    ///
    /// The commands are only written while kRPC has a non-zero input, so that a kerbal
    /// being flown from the keyboard is left alone.
    /// </remarks>
    [KSPAddon (KSPAddon.Startup.Flight, false)]
    public sealed class EVAAddon : MonoBehaviour
    {
        /// <summary>
        /// The movement commands a <c>KerbalEVA</c> fills in for its state machine. They are
        /// protected, so they are accessed by reflection.
        /// </summary>
        static class Commands
        {
            /// <summary>Direction to move in, in world space. Magnitude is ignored.</summary>
            public static readonly FieldInfo Move = Find ("tgtRpos");
            /// <summary>Direction the kerbal is holding, in world space.</summary>
            public static readonly FieldInfo Facing = Find ("tgtFwd");
            /// <summary>Up axis the kerbal is holding, in world space.</summary>
            public static readonly FieldInfo FacingUp = Find ("tgtUp");
            /// <summary>Climbing direction while on a ladder, in world space.</summary>
            public static readonly FieldInfo Climb = Find ("ladderTgtRPos");
            /// <summary>Jetpack translation demand, in world space.</summary>
            public static readonly FieldInfo Translation = Find ("packTgtRPos");
            /// <summary>Jetpack rotation demand, in world space, applied as torque.</summary>
            public static readonly FieldInfo Torque = Find ("cmdRot");
            /// <summary>
            /// Whether rotation is being commanded directly. Holds off the attitude
            /// controller that would otherwise fly the kerbal back to the orientation it
            /// is holding, and which owns the rotation demand when it runs.
            /// </summary>
            public static readonly FieldInfo RotatingManually = Find ("manualAxisControl");
            /// <summary>Parachute steering demand; x leans forward, y leans right.</summary>
            public static readonly FieldInfo ParachuteSteering = Find ("parachuteInput");

            /// <summary>The states of a kerbal's state machine.</summary>
            public static readonly FieldInfo States =
                typeof (KerbalFSM).GetField ("States", BindingFlags.NonPublic | BindingFlags.Instance);

            static FieldInfo Find (string name)
            {
                return typeof (KerbalEVA).GetField (name, BindingFlags.NonPublic | BindingFlags.Instance);
            }

            /// <summary>
            /// Whether every field was found, and so whether a kerbal can be driven at all.
            /// </summary>
            public static bool Available {
                get {
                    return Move != null && Facing != null && FacingUp != null &&
                        Climb != null && Translation != null && Torque != null &&
                        RotatingManually != null && ParachuteSteering != null && States != null;
                }
            }
        }

        /// <summary>
        /// The kerbals whose state machines have had the callback installed, keyed by
        /// vessel id. The module is held so that a kerbal rebuilt in place - which gets a
        /// new state machine, and so loses the callback - is detected and hooked again.
        /// </summary>
        static readonly IDictionary<Guid, KerbalEVA> hooked = new Dictionary<Guid, KerbalEVA> ();

        /// <summary>
        /// The kerbals whose rotation kRPC is driving, by vessel id. Held so that the
        /// attitude controller is only taken from, and handed back to, kerbals kRPC has
        /// actually turned, leaving one being flown from the keyboard alone.
        /// </summary>
        static readonly HashSet<Guid> rotating = new HashSet<Guid> ();

        /// <summary>
        /// The movement mode each kerbal kRPC is walking was in beforehand, by vessel id.
        /// Walking needs the game's first-person mode, and the player may have chosen the
        /// other one, so the mode is put back when the walk ends.
        /// </summary>
        static readonly IDictionary<Guid, bool> walkedMovementMode = new Dictionary<Guid, bool> ();

        /// <summary>
        /// The rate below which a kerbal counts as having stopped rotating, in radians per
        /// second.
        /// </summary>
        const float stoppedRotationRate = 0.05f;

        static bool loggedUnavailable;

        /// <summary>
        /// Wake the addon.
        /// </summary>
        public void Awake ()
        {
            Clear ();
            GameEvents.onVesselDestroy.Add (OnVesselDestroy);
        }

        /// <summary>
        /// Destroy the addon.
        /// </summary>
        public void OnDestroy ()
        {
            GameEvents.onVesselDestroy.Remove (OnVesselDestroy);
            Clear ();
        }

        static void Clear ()
        {
            hooked.Clear ();
            rotating.Clear ();
            walkedMovementMode.Clear ();
        }

        void OnVesselDestroy (Vessel vessel)
        {
            hooked.Remove (vessel.id);
            rotating.Remove (vessel.id);
            walkedMovementMode.Remove (vessel.id);
        }

        /// <summary>
        /// Install the callback on every kerbal on EVA. A kerbal's state machine is built
        /// when its module starts, so this cannot be done once at scene load.
        /// </summary>
        public void FixedUpdate ()
        {
            if (!FlightGlobals.ready)
                return;
            foreach (var vessel in FlightGlobals.VesselsLoaded) {
                var eva = vessel.evaController;
                if (eva == null || eva.fsm == null)
                    continue;
                KerbalEVA existing;
                if (hooked.TryGetValue (vessel.id, out existing) && ReferenceEquals (existing, eva))
                    continue;
                if (Hook (eva))
                    hooked [vessel.id] = eva;
            }
        }

        /// <summary>
        /// Prepend the callback to every state of a kerbal's state machine, so that it runs
        /// whichever state the kerbal is in and before that state acts on the commands.
        /// </summary>
        static bool Hook (KerbalEVA eva)
        {
            if (!Commands.Available) {
                LogUnavailableOnce ();
                return false;
            }
            var states = Commands.States.GetValue (eva.fsm) as List<KFSMState>;
            if (states == null || states.Count == 0)
                return false;
            KFSMCallback callback = () => Apply (eva);
            foreach (var state in states)
                state.OnFixedUpdate = (KFSMCallback)Delegate.Combine (callback, state.OnFixedUpdate);
            return true;
        }

        static void LogUnavailableOnce ()
        {
            if (loggedUnavailable)
                return;
            loggedUnavailable = true;
            KRPC.Utils.Logger.WriteLine (
                "EVAAddon: failed to find the KerbalEVA movement command fields; " +
                "kerbals on EVA cannot be controlled",
                KRPC.Utils.Logger.Severity.Warning);
        }

        /// <summary>
        /// Apply the vessel's control inputs to a kerbal, as the commands its state machine
        /// is about to act on.
        /// </summary>
        static void Apply (KerbalEVA eva)
        {
            var vessel = eva.vessel;
            if (vessel == null || vessel.packed)
                return;
            var inputs = PilotAddon.Get (vessel);
            var transform = eva.transform;

            // On foot the kerbal walks, and off it the jetpack flies it. Both are driven by
            // the same inputs: the game walks the kerbal only while it is on a surface, and
            // thrusts only while the jetpack is deployed
            var onFoot = vessel.LandedOrSplashed;
            var walking = onFoot && (inputs.Forward != 0f || inputs.Right != 0f);

            // Translation in the kerbal's own frame, matching the game's own EVA
            // translation axes.
            var translate = transform.forward * inputs.Forward + transform.right * inputs.Right +
                            transform.up * inputs.Up;
            if (translate != Vector3.zero)
                Set (Commands.Translation, eva, Get (Commands.Translation, eva) + translate);

            // On foot a kerbal can only turn, and only while walking. On a ladder it is
            // held by its hands and cannot turn at all, so the rotation inputs are the
            // jetpack's alone. A command sent on a ladder would stay latched against the
            // motion the ladder imposes
            if (onFoot || eva.OnALadder)
                ReleaseRotation (eva);
            else
                Rotate (eva, transform.up * inputs.Yaw + transform.forward * inputs.Roll -
                             transform.right * inputs.Pitch);

            if (walking)
                Walk (eva, inputs.Forward, inputs.Right, inputs.Yaw);
            else
                ReleaseWalk (eva);

            if (inputs.Forward != 0f || inputs.Right != 0f) {
                Set (Commands.Climb, eva, Get (Commands.Climb, eva) +
                     transform.up * inputs.Forward + transform.right * inputs.Right);
                Steer (eva, inputs.Forward, inputs.Right);
            }
        }

        /// <summary>
        /// Turn the kerbal with its jetpack, at a rate proportional to the input and up to
        /// its turn rate for a full one.
        /// </summary>
        /// <remarks>
        /// The kerbal's own attitude controller owns the jetpack's rotation demand and holds
        /// the kerbal at a fixed orientation, so it has to be held off for as long as the
        /// kerbal is being turned. Its demand is a torque, and a kerbal has so little
        /// inertia that a steady torque would wind it up to an absurd rate within a second,
        /// so the demand is closed around the kerbal's own rotation rate here rather than
        /// passed straight through. When the input stops, the kerbal is brought to a halt
        /// the same way before its attitude controller is handed the orientation it ended up
        /// at, to hold.
        /// </remarks>
        static void Rotate (KerbalEVA eva, Vector3 rotate)
        {
            var id = eva.vessel.id;
            if (rotate == Vector3.zero && !rotating.Contains (id))
                return;
            var rigidbody = eva.part.Rigidbody;
            if (rigidbody == null)
                return;
            var error = rotate * eva.turnRate - rigidbody.angularVelocity;
            if (rotate == Vector3.zero && error.magnitude < stoppedRotationRate) {
                ReleaseRotation (eva);
                return;
            }
            Set (Commands.Torque, eva, Vector3.ClampMagnitude (error, 1f));
            Commands.RotatingManually.SetValue (eva, true);
            rotating.Add (id);
        }

        /// <summary>
        /// Give the kerbal's attitude controller its orientation back, to hold from here on.
        /// </summary>
        static void ReleaseRotation (KerbalEVA eva)
        {
            if (!rotating.Remove (eva.vessel.id))
                return;
            Commands.RotatingManually.SetValue (eva, false);
            Set (Commands.Facing, eva, eva.transform.forward);
            Set (Commands.FacingUp, eva, eva.transform.up);
        }

        /// <summary>
        /// Walk the kerbal in the horizontal plane, relative to the direction it is facing,
        /// and steer it as it goes.
        /// </summary>
        /// <remarks>
        /// The direction is taken from the heading the kerbal is holding rather than from
        /// which way it happens to be pointing at this instant, so that walking sideways is
        /// a straight line rather than a curve that feeds its own turn. The kerbal is put
        /// into the game's first-person movement mode for as long as it is being walked,
        /// because the other mode moves it along its nose and relative to the camera, which
        /// makes both sideways movement and a repeatable heading impossible. The mode it
        /// was in is put back by <see cref="ReleaseWalk"/> when the walk ends.
        /// </remarks>
        static void Walk (KerbalEVA eva, float forward, float right, float yaw)
        {
            var up = eva.fUp;
            var heading = Vector3.ProjectOnPlane (Get (Commands.Facing, eva), up);
            if (heading.sqrMagnitude < 1e-6f)
                heading = Vector3.ProjectOnPlane (eva.transform.forward, up);
            heading = Quaternion.AngleAxis (
                yaw * eva.turnRate * Mathf.Rad2Deg * Time.fixedDeltaTime, up) * heading.normalized;
            // The walking direction is normalized and carries no speed, so the size of the
            // input picks the direction and nothing else.
            var direction = heading * forward + Vector3.Cross (up, heading) * right;
            Set (Commands.Move, eva, (Get (Commands.Move, eva) + direction).normalized);
            Set (Commands.Facing, eva, heading);
            Set (Commands.FacingUp, eva, up);
            if (!walkedMovementMode.ContainsKey (eva.vessel.id))
                walkedMovementMode [eva.vessel.id] = eva.CharacterFrameMode;
            eva.CharacterFrameMode = true;
        }

        /// <summary>
        /// Put the kerbal back into the movement mode it was in before kRPC walked it.
        /// </summary>
        static void ReleaseWalk (KerbalEVA eva)
        {
            bool mode;
            if (!walkedMovementMode.TryGetValue (eva.vessel.id, out mode))
                return;
            walkedMovementMode.Remove (eva.vessel.id);
            eva.CharacterFrameMode = mode;
        }

        /// <summary>
        /// Lean the kerbal under a deployed EVA parachute, as the same two inputs do when
        /// they are thrusting the jetpack.
        /// </summary>
        static void Steer (KerbalEVA eva, float forward, float right)
        {
            var steering = Get (Commands.ParachuteSteering, eva) + new Vector3 (forward, right, 0f);
            Set (Commands.ParachuteSteering, eva, new Vector3 (
                Mathf.Clamp (steering.x, -1f, 1f), Mathf.Clamp (steering.y, -1f, 1f), 0f));
        }

        static Vector3 Get (FieldInfo command, KerbalEVA eva)
        {
            return (Vector3)command.GetValue (eva);
        }

        static void Set (FieldInfo command, KerbalEVA eva, Vector3 value)
        {
            command.SetValue (eva, value);
        }
    }
}
