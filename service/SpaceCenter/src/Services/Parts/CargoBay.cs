using System;
using KRPC.Service;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.Utils;

namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// A cargo bay. Obtained by calling <see cref="Part.CargoBay"/>.
    /// </summary>
    [KRPCClass (Service = "SpaceCenter", GameScene = GameScene.Flight)]
    public class CargoBay : Equatable<CargoBay>, IGameObjectState
    {
        ModuleRef bayRef;
        ModuleRef animationRef;

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
            bayRef = ModuleRef.ForType<ModuleCargoBay> (internalPart);
            animationRef = ModuleRef.ForType<ModuleAnimateGeneric> (internalPart);
        }

        ModuleCargoBay InternalBay {
            get { return (ModuleCargoBay)bayRef.Get (Part.InternalPart); }
        }

        ModuleAnimateGeneric InternalAnimation {
            get { return (ModuleAnimateGeneric)animationRef.Find (Part.InternalPart); }
        }

        /// <summary>
        /// What the game holds for the cargo bay: the state of the part
        /// carrying it, or destroyed once that part no longer has the module.
        /// </summary>
        public GameObjectState GameObjectState {
            get { return bayRef.StateOn (Part); }
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (CargoBay other)
        {
            return !ReferenceEquals (other, null) && Part == other.Part && bayRef == other.bayRef;
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            return Hash.Of (Part).And (bayRef);
        }

        /// <summary>
        /// The part object for this cargo bay.
        /// </summary>
        [KRPCProperty (GameScene = GameScene.Flight | GameScene.Editor)]
        public Part Part { get; private set; }

        /// <summary>
        /// The state of the cargo bay.
        /// </summary>
        /// <remarks>
        /// This describes where the bay's doors are, which is not the same as whether the parts
        /// inside are sheltered: a bay whose open end has nothing attached to it never shelters
        /// anything, however tightly shut it is. Use <see cref="Part.Shielded"/> for that.
        ///
        /// A cargo bay is never <see cref="DeployableState.Broken" />, as the game
        /// does not track damage for them.
        /// </remarks>
        [KRPCProperty]
        public DeployableState State {
            get {
                if (InternalAnimation.IsMoving ())
                    return OpeningOrOpen
                        ? DeployableState.Deploying
                        : DeployableState.Retracting;
                return OpeningOrOpen
                    ? DeployableState.Deployed
                    : DeployableState.Retracted;
            }
        }

        /// <summary>
        /// Whether the cargo bay is open.
        /// </summary>
        [KRPCProperty]
        public bool Open {
            get { return OpeningOrOpen; }
            set {
                if (value == OpeningOrOpen)
                    return;
                var toggle = ToggleEvent;
                if (toggle != null)
                    toggle.Invoke ();
            }
        }

        /// <summary>
        /// Whether the bay is open, or travelling that way.
        /// </summary>
        /// <remarks>
        /// The animation runs forwards, towards a scalar of 1, while animSwitch is false, and
        /// backwards towards 0 while it is true. closedPosition is the scalar at which the bay is
        /// shut, so which of those opens it depends on which end of the animation that is.
        ///
        /// A bay that is not moving is sitting at whichever end it last travelled to, so the same
        /// answer covers a moving and a stationary bay alike. Reading the direction rather than
        /// the position matters: the animation stops fractionally short of its end, so comparing
        /// the scalar against closedPosition reports a shut bay as open for as long as it takes
        /// the last frame to land.
        /// </remarks>
        bool OpeningOrOpen {
            get { return !InternalAnimation.animSwitch == (InternalBay.closedPosition < 0.5f); }
        }

        /// <summary>
        /// The event that opens or closes the bay, whichever it currently offers. It is looked up
        /// by id -- the name of the method implementing it -- which the game does not translate,
        /// unlike the display name on the button. Null when the bay cannot currently be toggled,
        /// for example while it is shielded from the airstream.
        /// </summary>
        BaseEvent ToggleEvent {
            get {
                var toggle = InternalAnimation.Events ["Toggle"];
                if (toggle == null)
                    return null;
                var available = HighLogic.LoadedSceneIsEditor ? toggle.guiActiveEditor : toggle.guiActive;
                return available ? toggle : null;
            }
        }
    }
}
