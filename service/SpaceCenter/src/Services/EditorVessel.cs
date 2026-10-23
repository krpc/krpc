using System;
using System.Collections.Generic;
using System.Linq;
using KRPC.Service;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.Utils;
using UnityEngine;
using Parts = KRPC.SpaceCenter.Services.Parts;
using Tuple3 = System.Tuple<double, double, double>;
using Tuple4 = System.Tuple<double, double, double, double>;

namespace KRPC.SpaceCenter.Services
{
    /// <summary>
    /// The vessel that is being constructed in the editor.
    /// Obtained by calling <see cref="Editor.Vessel"/>.
    /// </summary>
    [KRPCClass (Service = "SpaceCenter", GameScene = GameScene.Editor)]
    public class EditorVessel : Equatable<EditorVessel>
    {
        internal EditorVessel ()
        {
        }

        /// <summary>
        /// Returns true if the objects are equal. The editor only ever holds one vessel,
        /// so every editor vessel object refers to it.
        /// </summary>
        public override bool Equals (EditorVessel other)
        {
            return !ReferenceEquals (other, null);
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            return 0;
        }

        /// <summary>
        /// The KSP ship construct. Looked up on each use, so the object keeps working
        /// after a different vessel is loaded into the editor.
        /// </summary>
        public ShipConstruct InternalShipConstruct {
            get {
                return EditorExtensions.GetShip ();
            }
        }

        /// <summary>
        /// The name of the vessel.
        /// </summary>
        [KRPCProperty]
        public string Name {
            get { return InternalShipConstruct.shipName; }
            set { InternalShipConstruct.shipName = value; }
        }

        /// <summary>
        /// The description of the vessel.
        /// </summary>
        [KRPCProperty]
        public string Description {
            get { return InternalShipConstruct.shipDescription; }
            set { InternalShipConstruct.shipDescription = value; }
        }

        /// <summary>
        /// The editor the vessel is in.
        /// </summary>
        /// <remarks>
        /// This is the editor the vessel is currently loaded into, not the one it was
        /// designed in. A vessel moved from the vehicle assembly building to the space
        /// plane hangar reports the space plane hangar.
        /// </remarks>
        [KRPCProperty]
        public EditorFacility Facility {
            get { return (EditorFacility)InternalShipConstruct.shipFacility; }
        }

        /// <summary>
        /// The total mass of the vessel, including resources and crew, in <unit>kg</unit>.
        /// </summary>
        [KRPCProperty]
        public float Mass {
            get {
                float dryMass;
                float fuelMass;
                return InternalShipConstruct.GetShipMass (out dryMass, out fuelMass, ShipConstruction.ShipManifest) * 1000f;
            }
        }

        /// <summary>
        /// The total mass of the vessel, excluding resources, in <unit>kg</unit>.
        /// </summary>
        [KRPCProperty]
        public float DryMass {
            get {
                float dryMass;
                float fuelMass;
                InternalShipConstruct.GetShipMass (out dryMass, out fuelMass, ShipConstruction.ShipManifest);
                return dryMass * 1000f;
            }
        }

        /// <summary>
        /// The reference frame that is fixed relative to the vessel,
        /// and orientated with the vessel.
        /// <list type="bullet">
        /// <item><description>The origin is at the center of mass of the vessel.</description></item>
        /// <item><description>The axes rotate with the vessel.</description></item>
        /// <item><description>The x-axis points out to the right of the vessel.</description></item>
        /// <item><description>The y-axis points in the forward direction of the vessel.</description></item>
        /// <item><description>The z-axis points out of the bottom off the vessel.</description></item>
        /// </list>
        /// </summary>
        /// <remarks>
        /// The axes are those of the root part. This is the frame
        /// <see cref="Vessel.ReferenceFrame"/> gives at launch, with the root part as the
        /// control point.
        /// </remarks>
        [KRPCProperty]
        public ReferenceFrame ReferenceFrame {
            get { return ReferenceFrame.EditorVessel (); }
        }

        /// <summary>
        /// The reference frame that is fixed relative to the vessel,
        /// and orientated with the vertical of the editor.
        /// <list type="bullet">
        /// <item><description>The origin is at the center of mass of the vessel.</description></item>
        /// <item><description>The axes are fixed in the editor.</description></item>
        /// <item><description>The x-axis points upwards.</description></item>
        /// <item><description>The y-axis and z-axis are horizontal.</description></item>
        /// </list>
        /// </summary>
        /// <remarks>
        /// This is the vessel as it stands in the editor, and as it stands at launch. The
        /// editor has no north or east, so the horizontal axes are fixed in the editor.
        /// </remarks>
        [KRPCProperty]
        public ReferenceFrame SurfaceReferenceFrame {
            get { return ReferenceFrame.EditorSurface (); }
        }

        /// <summary>
        /// Returns a <see cref="SimulatedFlight"/> object that can be used to simulate the
        /// vessel in flight, in the specified reference frame.
        /// </summary>
        /// <param name="referenceFrame">
        /// Reference frame. Defaults to the simulated surface reference frame
        /// (<see cref="SimulatedFlight.SurfaceReferenceFrame"/>), and follows it when it is set.
        /// </param>
        /// <remarks>
        /// Each call returns a new object, which is kept until the game leaves the editor.
        /// </remarks>
        [KRPCMethod]
        public SimulatedFlight Flight (ReferenceFrame referenceFrame = null)
        {
            return new SimulatedFlight (referenceFrame);
        }

        /// <summary>
        /// The position of the center of mass of the vessel, in the given reference frame.
        /// </summary>
        /// <returns>The position as a vector.</returns>
        /// <param name="referenceFrame">The reference frame that the returned
        /// position vector is in.</param>
        [KRPCMethod]
        public Tuple3 Position (ReferenceFrame referenceFrame)
        {
            return referenceFrame.PositionFromWorldSpace (InternalShipConstruct.WorldCenterOfMass ()).ToTuple ();
        }

        /// <summary>
        /// The axis-aligned bounding box of the vessel in the given reference frame.
        /// </summary>
        /// <returns>The positions of the minimum and maximum vertices of the box.</returns>
        /// <param name="referenceFrame">The reference frame that the returned
        /// position vectors are in.</param>
        [KRPCMethod]
        public BoundingBox BoundingBox (ReferenceFrame referenceFrame)
        {
            var parts = InternalShipConstruct.Parts;
            if (parts.Count == 0)
                throw new InvalidOperationException ("The editor vessel has no parts");
            var bounds = parts [0].GetBounds (referenceFrame);
            for (int i = 1; i < parts.Count; i++)
                bounds.Encapsulate (parts [i].GetBounds (referenceFrame));
            return new BoundingBox (bounds);
        }

        /// <summary>
        /// The rotation of the vessel, in the given reference frame.
        /// </summary>
        /// <returns>The rotation as a quaternion of the form <c>(x, y, z, w)</c>.</returns>
        /// <param name="referenceFrame">The reference frame that the returned
        /// rotation is in.</param>
        [KRPCMethod]
        public Tuple4 Rotation (ReferenceFrame referenceFrame)
        {
            return referenceFrame.RotationFromWorldSpace (VesselTransform.rotation).ToTuple ();
        }

        /// <summary>
        /// The direction in which the vessel is pointing, in the given reference frame.
        /// </summary>
        /// <returns>The direction as a unit vector.</returns>
        /// <param name="referenceFrame">The reference frame that the returned
        /// direction is in.</param>
        [KRPCMethod]
        public Tuple3 Direction (ReferenceFrame referenceFrame)
        {
            return referenceFrame.DirectionFromWorldSpace (VesselTransform.up).ToTuple ();
        }

        /// <summary>
        /// The root part's reference transform, which gives the vessel its orientation.
        /// </summary>
        internal Transform VesselTransform {
            get {
                var transform = InternalShipConstruct.VesselTransform ();
                if (transform == null)
                    throw new InvalidOperationException ("The editor vessel has no parts");
                return transform;
            }
        }

        /// <summary>
        /// The moment of inertia of the vessel around its center of mass in <unit>kg.m^2</unit>.
        /// The inertia values in the returned 3-tuple are around the
        /// pitch, roll and yaw directions respectively.
        /// This corresponds to the vessels reference frame (<see cref="ReferenceFrame"/>).
        /// </summary>
        [KRPCProperty]
        public Tuple3 MomentOfInertia {
            get { return ComputeInertiaTensor ().Diagonal ().ToTuple (); }
        }

        /// <summary>
        /// The inertia tensor of the vessel around its center of mass,
        /// in the vessels reference frame (<see cref="ReferenceFrame"/>).
        /// Returns the 3x3 matrix as a list of elements, in row-major order.
        /// </summary>
        [KRPCProperty]
        public IList<double> InertiaTensor {
            get { return ComputeInertiaTensor ().ToList (); }
        }

        Matrix4x4 ComputeInertiaTensor ()
        {
            return InternalShipConstruct.ComputeInertiaTensor ().MultiplyScalar (1000f);
        }

        /// <summary>
        /// The total cost of the vessel, including resources, in funds.
        /// </summary>
        [KRPCProperty]
        public float Cost {
            get {
                float dryCost;
                float fuelCost;
                return InternalShipConstruct.GetShipCosts (out dryCost, out fuelCost, ShipConstruction.ShipManifest);
            }
        }

        /// <summary>
        /// The total number of crew that the vessel can hold.
        /// </summary>
        [KRPCProperty]
        public int CrewCapacity {
            get { return InternalShipConstruct.Parts.Sum (part => part.CrewCapacity); }
        }

        /// <summary>
        /// A <see cref="Parts.Parts"/> object, that can be used to interact with the parts
        /// that make up the vessel.
        /// </summary>
        [KRPCProperty]
        public Parts.Parts Parts {
            get { return new Parts.Parts (); }
        }

        /// <summary>
        /// A <see cref="Resources"/> object for the resources the vessel can hold.
        /// </summary>
        [KRPCProperty]
        public Resources Resources {
            get { return new Resources (); }
        }

        /// <summary>
        /// Whether the stock delta-v figures for the vessel have been calculated.
        /// </summary>
        /// <remarks>
        /// The game recalculates them in the background whenever the vessel changes, and
        /// whenever <see cref="Editor.DeltaVBody"/> or <see cref="Editor.DeltaVAltitude"/>
        /// is set, and they are out of date until it has finished. Reading
        /// <see cref="DeltaV"/>, or any delta-v member of a <see cref="Stage"/>, before
        /// then raises an error rather than returning a stale figure. Poll this after
        /// changing anything the figures depend on.
        /// </remarks>
        [KRPCProperty]
        public bool DeltaVReady {
            get { return EditorDeltaV.Ready; }
        }

        /// <summary>
        /// Activation (burn) stages for the vessel, in ascending stage order. This does
        /// not include the -1 stage (parts with no staging icon), as it carries no
        /// delta-v; use <see cref="StageAt"/> with -1 to get those parts.
        /// </summary>
        [KRPCProperty]
        public IList<Stage> Stages {
            get {
                return ActivationStageNumbers ()
                    .Select (n => new Stage (n, false))
                    .ToList ();
            }
        }

        /// <summary>
        /// The activation stage with the given number. Pass -1 to get the parts that are
        /// never activated (those with no staging icon).
        /// </summary>
        /// <param name="stage">Get activation stage at this index.</param>
        [KRPCMethod]
        public Stage StageAt (int stage)
        {
            if (stage != -1 && !ActivationStageNumbers ().Contains (stage))
                throw new ArgumentException ("Stage not found", nameof (stage));
            return new Stage (stage, false);
        }

        /// <summary>
        /// Decouple stages for the vessel, in ascending stage order. The -1 stage,
        /// containing the parts that are never decoupled and remain on the vessel, is
        /// included first when any such parts exist.
        /// </summary>
        [KRPCProperty]
        public IList<Stage> DecoupleStages {
            get {
                return DecoupleStageNumbers ()
                    .Select (n => new Stage (n, true))
                    .ToList ();
            }
        }

        /// <summary>
        /// The decouple stage with the given number. Pass -1 to get the parts that are
        /// never decoupled and remain on the vessel after all stages have fired.
        /// </summary>
        /// <param name="stage">Get decouple stage at this index.</param>
        [KRPCMethod]
        public Stage DecoupleStageAt (int stage)
        {
            if (!RawDecoupleStageNumbers ().Contains (stage))
                throw new ArgumentException ("Decouple stage not found", nameof (stage));
            return new Stage (stage, true);
        }

        /// <summary>
        /// Total delta-v for the vessel in the situation the game's delta-v readout
        /// assumes, in <unit>m/s</unit>. See <see cref="Editor.DeltaVBody"/>.
        /// </summary>
        [KRPCProperty]
        public float DeltaV {
            get { return (float)RequireDeltaV ().TotalDeltaVActual; }
        }

        /// <summary>
        /// Total vacuum delta-v for the vessel, in <unit>m/s</unit>.
        /// </summary>
        [KRPCProperty]
        public float VacuumDeltaV {
            get { return (float)RequireDeltaV ().TotalDeltaVVac; }
        }

        /// <summary>
        /// Total sea-level delta-v for the vessel, in <unit>m/s</unit>.
        /// </summary>
        [KRPCProperty]
        public float SeaLevelDeltaV {
            get { return (float)RequireDeltaV ().TotalDeltaVASL; }
        }

        /// <summary>
        /// Total burn time for the vessel, in seconds.
        /// </summary>
        [KRPCProperty]
        public float BurnTime {
            get { return (float)RequireDeltaV ().TotalBurnTime; }
        }

        VesselDeltaV RequireDeltaV ()
        {
            if (!EditorDeltaV.Ready)
                throw new InvalidOperationException (
                    "Delta-v has not been calculated for this vessel yet.");
            return InternalShipConstruct.vesselDeltaV;
        }

        IList<int> ActivationStageNumbers ()
        {
            var construct = InternalShipConstruct;
            return StagingExtensions.ActivationStageNumbers (construct.vesselDeltaV, construct.Parts);
        }

        List<int> DecoupleStageNumbers ()
        {
            return InternalShipConstruct.Parts
                .Select (p => p.DecoupledAt ())
                .Distinct ()
                .OrderBy (n => n)
                .ToList ();
        }

        /// <summary>
        /// Stage indices valid for decouple-stage queries, matching the vessel's own
        /// numbering: the -1 stage and every stage number up to the highest in use,
        /// including those with no decoupling parts.
        /// </summary>
        List<int> RawDecoupleStageNumbers ()
        {
            var indices = ActivationStageNumbers ()
                .Concat (DecoupleStageNumbers ())
                .ToList ();
            var max = indices.Count == 0 ? 0 : indices.Max ();
            return new List<int> { -1 }
                .Concat (Enumerable.Range (0, max + 1))
                .ToList ();
        }
    }
}
