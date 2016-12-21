using System;
using System.Linq;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.Utils;

namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// A cargo bay. Obtained by calling <see cref="Part.CargoBay"/>.
    /// </summary>
    [KRPCClass (Service = "SpaceCenter")]
    public class CargoBay : Equatable<CargoBay>
    {
        readonly ModuleCargoBay bay;
        readonly ModuleAnimateGeneric animation;

        internal static bool Is (Part part)
        {
            var internalPart = part.InternalPart;
            return
            internalPart.HasModule<ModuleCargoBay> () &&
            internalPart.HasModule<ModuleAnimateGeneric> () &&
            !internalPart.HasModule<ModuleProceduralFairing> ();
        }

        internal CargoBay (Part part)
        {
            if (!Is (part))
                throw new ArgumentException ("Part is not a cargo bay");
            Part = part;
            var internalPart = part.InternalPart;
            bay = internalPart.Module<ModuleCargoBay> ();
            animation = internalPart.Module<ModuleAnimateGeneric> ();
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (CargoBay other)
        {
            return !ReferenceEquals (other, null) && Part == other.Part && bay.Equals (other.bay);
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            return Part.GetHashCode () ^ bay.GetHashCode ();
        }

        /// <summary>
        /// The part object for this cargo bay.
        /// </summary>
        [KRPCProperty]
        public Part Part { get; private set; }

        /// <summary>
        /// The state of the cargo bay.
        /// </summary>
        [KRPCProperty]
        public CargoBayState State {
            get {
                if (bay.ClosedAndLocked ())
                    return CargoBayState.Closed;
                else if (!animation.IsMoving ())
                    return CargoBayState.Open;
                else if (!animation.animSwitch)
                    return animation.startEventGUIName == "Open" ? CargoBayState.Opening : CargoBayState.Closing;
                else
                    return animation.startEventGUIName == "Close" ? CargoBayState.Opening : CargoBayState.Closing;
            }
        }

        /// <summary>
        /// Whether the cargo bay is open.
        /// </summary>
        [KRPCProperty]
        public bool Open {
            get {
                var state = State;
                return state == CargoBayState.Open || state == CargoBayState.Opening;
            }
            set {
                var openEvent = OpenEvent;
                var closeEvent = CloseEvent;
                if (value && openEvent != null)
                    openEvent.Invoke ();
                else if (!value && closeEvent != null)
                    closeEvent.Invoke ();
            }
        }

        BaseEvent OpenEvent {
            get {
                return animation.Events
                    .Where (x => x != null && (HighLogic.LoadedSceneIsEditor ? x.guiActiveEditor : x.guiActive))
                    .FirstOrDefault (x => x.guiName == "Open");
            }
        }

        BaseEvent CloseEvent {
            get {
                return animation.Events
                    .Where (x => x != null && (HighLogic.LoadedSceneIsEditor ? x.guiActiveEditor : x.guiActive))
                    .FirstOrDefault (x => x.guiName == "Close");
            }
        }
    }
}
