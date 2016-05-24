using System;
using System.Linq;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.Utils;

namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// Obtained by calling <see cref="Part.CargoBay"/>.
    /// </summary>
    [KRPCClass (Service = "SpaceCenter")]
    public sealed class CargoBay : Equatable<CargoBay>
    {
        readonly Part part;
        readonly ModuleCargoBay bay;
        readonly ModuleAnimateGeneric animation;

        internal static bool Is (Part part)
        {
            return part.InternalPart.HasModule<ModuleCargoBay> () && !part.InternalPart.HasModule<ModuleProceduralFairing> ();
        }

        internal CargoBay (Part part)
        {
            this.part = part;
            bay = part.InternalPart.Module<ModuleCargoBay> ();
            animation = part.InternalPart.Module<ModuleAnimateGeneric> ();
            if (bay == null || animation == null)
                throw new ArgumentException ("Part is not a cargo bay");
        }

        /// <summary>
        /// Check if cargo bays are equal.
        /// </summary>
        public override bool Equals (CargoBay obj)
        {
            return part == obj.part && bay == obj.bay;
        }

        /// <summary>
        /// Hash the cargo bay.
        /// </summary>
        public override int GetHashCode ()
        {
            return part.GetHashCode () ^ bay.GetHashCode ();
        }

        /// <summary>
        /// The part object for this cargo bay.
        /// </summary>
        [KRPCProperty]
        public Part Part {
            get { return part; }
        }

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
                else if (OpenEvent == null)
                    return CargoBayState.Opening;
                else
                    return CargoBayState.Closing;
            }
        }

        /// <summary>
        /// Whether the cargo bay is open.
        /// </summary>
        [KRPCProperty]
        public bool Open {
            get { return State == CargoBayState.Open || State == CargoBayState.Opening; }
            set {
                if (value && OpenEvent != null)
                    OpenEvent.Invoke ();
                else if (!value && CloseEvent != null)
                    CloseEvent.Invoke ();
            }
        }

        BaseEvent OpenEvent {
            get {
                return animation.Events
                    .Where (x => x != null && (HighLogic.LoadedSceneIsEditor ? x.guiActiveEditor : x.guiActive) && x.active)
                    .FirstOrDefault (x => x.guiName == "Open");
            }
        }

        BaseEvent CloseEvent {
            get {
                return animation.Events
                    .Where (x => x != null && (HighLogic.LoadedSceneIsEditor ? x.guiActiveEditor : x.guiActive) && x.active)
                    .FirstOrDefault (x => x.guiName == "Close");
            }
        }
    }
}
