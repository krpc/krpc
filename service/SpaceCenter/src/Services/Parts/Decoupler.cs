using System;
using System.Linq;
using KRPC.Service;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.Utils;

namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// A decoupler. Obtained by calling <see cref="Part.Decoupler"/>
    /// </summary>
    [KRPCClass (Service = "SpaceCenter", GameScene = GameScene.Flight)]
    public class Decoupler : Equatable<Decoupler>, IGameObjectState
    {
        ModuleRef decouplerRef;

        internal static bool Is (Part part)
        {
            var internalPart = part.InternalPart;
            return
            internalPart.HasModule<ModuleDecouple> () ||
            internalPart.HasModule<ModuleAnchoredDecoupler> ();
        }

        internal Decoupler (Part part)
        {
            Part = part;
            var module = part.InternalPart.DecouplerModule ();
            if (module == null)
                throw new ArgumentException("Part is not a decoupler");
            decouplerRef = ModuleRef.ForModule (module);
        }

        /// <summary>
        /// The decoupler's part module, found on the part again on every access.
        /// </summary>
        ModuleDecouplerBase InternalDecoupler {
            get { return (ModuleDecouplerBase)decouplerRef.Get (Part.InternalPart); }
        }

        /// <summary>
        /// What the game holds for the decoupler: the state of the part carrying it, or
        /// destroyed once that part no longer has the module.
        /// </summary>
        public GameObjectState GameObjectState {
            get { return decouplerRef.StateOn (Part); }
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (Decoupler other)
        {
            return !ReferenceEquals (other, null) &&
            Part == other.Part && decouplerRef == other.decouplerRef;
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            return Hash.Of (Part).And (decouplerRef);
        }

        /// <summary>
        /// The part object for this decoupler.
        /// </summary>
        [KRPCProperty (GameScene = GameScene.Flight | GameScene.Editor)]
        public Part Part { get; private set; }

        /// <summary>
        /// Fires the decoupler. Returns the new vessel created when the decoupler fires.
        /// Throws an exception if the decoupler has already fired.
        /// </summary>
        /// <remarks>
        /// When called, the active vessel may change. It is therefore possible that,
        /// after calling this function, the object(s) returned by previous call(s) to
        /// <see cref="SpaceCenter.ActiveVessel"/> no longer refer to the active vessel.
        /// </remarks>
        [KRPCMethod]
        public Vessel Decouple ()
        {
            if (Decoupled)
                throw new InvalidOperationException ("Decoupler has already fired");

            var preVesselIds = FlightGlobals.Vessels.Select (v => v.id).ToList ();

            // Fire the decoupler
            InternalDecoupler.Decouple ();

            return PartSeparation.NewVessel (Part, preVesselIds, () => Decoupled);
        }

        /// <summary>
        /// Whether the decoupler has fired.
        /// </summary>
        [KRPCProperty]
        public bool Decoupled {
            get {
                return InternalDecoupler.isDecoupled;
            }
        }

        /// <summary>
        /// Whether the decoupler is enabled in the staging sequence.
        /// </summary>
        [KRPCProperty (GameScene = GameScene.Flight | GameScene.Editor)]
        public bool Staged {
            get { return InternalDecoupler.StagingEnabled (); }
        }

        /// <summary>
        /// The impulse that the decoupler imparts when it is fired, in Newton seconds.
        /// </summary>
        [KRPCProperty (GameScene = GameScene.Flight | GameScene.Editor)]
        public float Impulse {
            get { return InternalDecoupler.ejectionForce * 10f; }
        }

        /// <summary>
        /// Whether the decoupler is an omni-decoupler (e.g. stack separator)
        /// </summary>
        [KRPCProperty (GameScene = GameScene.Flight | GameScene.Editor)]
        public bool IsOmniDecoupler
        {
            get { return InternalDecoupler.isOmniDecoupler; }
        }

        /// <summary>
        /// The part attached to this decoupler's explosive node.
        /// </summary>
        [KRPCProperty (Nullable = true, GameScene = GameScene.Flight | GameScene.Editor)]
        public Part AttachedPart
        {
            get
            {
                var attach = InternalDecoupler.ExplosiveNode;
                if (attach == null || attach.attachedPart == null)
                {
                    return null;
                }
                return new Part(attach.attachedPart);
            }
        }
    }
}
