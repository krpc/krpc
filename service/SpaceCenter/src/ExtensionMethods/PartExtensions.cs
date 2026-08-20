using System;
using System.Linq;
using KRPC.SpaceCenter.Services;
using UnityEngine;

namespace KRPC.SpaceCenter.ExtensionMethods
{
    static class PartExtensions
    {
        /// <summary>
        /// Returns true if the part contains the given part module
        /// </summary>
        public static bool HasModule<T> (this Part part) where T : PartModule
        {
            return part.Modules.Contains<T> ();
        }

        /// <summary>
        /// Returns true if the part contains the given part module
        /// </summary>
        public static bool HasModule (this Part part, string module)
        {
            return part.Modules.Contains (module);
        }

        /// <summary>
        /// Returns the first part module of the specified type, or null if none can be found
        /// </summary>
        public static T Module<T> (this Part part) where T : PartModule
        {
            return part.Modules.OfType<T> ().FirstOrDefault ();
        }

        /// <summary>
        /// Returns the first part module of the named type, or null if none can be found
        /// </summary>
        public static PartModule Module (this Part part, string type)
        {
            foreach (var module in part.Modules) {
                if (module.GetType ().Name == type)
                    return module;
            }
            return null;
        }

        /// <summary>
        /// Returns the decoupler part module for the part, or null if it does not have one.
        /// A part with both a stack and a radial decoupler is treated as a stack decoupler.
        /// </summary>
        public static ModuleDecouplerBase DecouplerModule (this Part part)
        {
            return (ModuleDecouplerBase)part.Module<ModuleDecouple> () ??
            part.Module<ModuleAnchoredDecoupler> ();
        }

        /// <summary>
        /// Returns true if the part is massless
        /// </summary>
        public static bool IsMassless (this Part part)
        {
            return part.physicalSignificance == Part.PhysicalSignificance.NONE || part.HasModule<LaunchClamp> ();
        }

        /// <summary>
        /// Whether the part's rigidbody is the one physics uses. In the editor a part
        /// still has a rigidbody, but the game leaves it as an unconfigured placeholder
        /// weighing Unity's default of one tonne.
        /// </summary>
        public static bool HasPhysicsBody (this Part part)
        {
            return part.rb != null && !HighLogic.LoadedSceneIsEditor;
        }

        /// <summary>
        /// The mass of the part, including resources and crew, in tonnes.
        /// </summary>
        public static float PhysicsMass (this Part part)
        {
            if (part.IsMassless ())
                return 0f;
            if (part.HasPhysicsBody ())
                return part.rb.mass;
            part.UpdateMass ();
            return part.mass + part.GetResourceMass () + part.CrewMass ();
        }

        /// <summary>
        /// The mass of the crew assigned to the part, in tonnes.
        /// </summary>
        public static float CrewMass (this Part part)
        {
            return AssignedCrewCount (part) * PhysicsGlobals.KerbalCrewMass;
        }

        /// <summary>
        /// The mass of the part, including resources, in kg.
        /// </summary>
        public static float WetMass (this Part part)
        {
            return part.PhysicsMass () * 1000f;
        }

        /// <summary>
        /// The mass of the part, excluding resources, in kg. Includes crew, as the
        /// rigidbody mass in flight does.
        /// </summary>
        public static float DryMass (this Part part)
        {
            if (part.IsMassless ())
                return 0f;
            if (part.HasPhysicsBody ())
                return Mathf.Max (0f, (part.rb.mass - part.resourceMass) * 1000f);
            part.UpdateMass ();
            return Mathf.Max (0f, (part.mass + part.CrewMass ()) * 1000f);
        }

        /// <summary>
        /// How many crew occupy the part. In the editor that is the assignment on the
        /// craft's manifest, which is what the game will seat when the vessel is launched.
        /// </summary>
        static int AssignedCrewCount (Part part)
        {
            if (!HighLogic.LoadedSceneIsEditor)
                return part.protoModuleCrew.Count;
            var manifest = ShipConstruction.ShipManifest;
            if (manifest == null)
                return 0;
            var partManifest = manifest.GetPartCrewManifest (part.craftID);
            if (partManifest == null)
                return 0;
            var crew = partManifest.GetPartCrew ();
            if (crew == null)
                return 0;
            int count = 0;
            for (int i = 0; i < crew.Length; i++) {
                if (crew [i] != null)
                    count++;
            }
            return count;
        }

        /// <summary>
        /// Returns the index of the stage in which the part will be decoupled, or -1 if it is never decoupled.
        /// Transversed the tree of parts from the desired part to the root, and finds the activation stage
        /// for the first decoupler that will decouple the part (the one with the highest stage number)
        /// </summary>
        public static int DecoupledAt (this Part part)
        {
            int stage = -1;
            do {
                int candidate = -1;
                var parent = part.parent;
                var decoupler = part.DecouplerModule ();

                // If the part will decouple itself from its parent, use the parts activation stage
                if (part.HasModule<LaunchClamp> ()) {
                    candidate = part.inverseStage;
                } else if (decoupler != null && decoupler.isEnabled) {
                    if (decoupler.isOmniDecoupler)
                        candidate = part.inverseStage;
                    else if (parent != null && decoupler.ExplosiveNode != null && decoupler.ExplosiveNode.attachedPart == parent)
                        candidate = part.inverseStage;
                }

                // If the part will be decoupled by its parent, use the parents activation stage
                if (candidate == -1 && parent != null) {
                    decoupler = parent.DecouplerModule ();
                    if (decoupler != null && decoupler.isEnabled) {
                        if (decoupler.isOmniDecoupler)
                            candidate = parent.inverseStage;
                        else if (decoupler.ExplosiveNode != null && decoupler.ExplosiveNode.attachedPart == part)
                            candidate = parent.inverseStage;
                    }
                }

                stage = Math.Max (candidate, stage);
                part = part.parent;
            } while (part != null);
            return stage;
        }

        /// <summary>
        /// Returns the position in world space of the center of mass of the part, or the parts transform position if it has no mass.
        /// </summary>
        public static Vector3d CenterOfMass (this Part part)
        {
            return part.WorldCenterOfMass ();
        }

        /// <summary>
        /// Computes the axis-aligned bounding box for a part in the given reference frame.
        /// </summary>
        /// <remarks>
        /// This is an expensive calculation. It iterates over the meshes of the parts model
        /// to compute a tight axis-aligned bounding box.
        /// It does not use part.collider.bounds, as this is aligned to world space and
        /// would not provide a tight bounding box in the given reference frame.
        /// </remarks>
        public static Bounds GetBounds (this Part part, ReferenceFrame referenceFrame)
        {
            var bounds = new Bounds (referenceFrame.PositionFromWorldSpace (part.WCoM), Vector3.zero);
            // Only the parts own model, the same subtree KSP measures a part against. Searching
            // the whole part transform instead would pick up the models of physicsless child
            // parts, which hang off it, along with any object a mod parents to the part.
            var meshes = part.FindModelComponents<MeshFilter> ();
            for (int i = 0; i < meshes.Count; i++) {
                var mesh = meshes [i];
                // The model subtree is walked in full, so meshes that are currently switched
                // off - a hidden part variant, a stowed animation state - have to be skipped.
                if (!mesh.gameObject.activeInHierarchy)
                    continue;
                // sharedMesh, not mesh: the latter instantiates a private copy of the mesh
                // on the first access and leaves the part rendering that copy.
                var geometry = mesh.sharedMesh;
                if (geometry == null)
                    continue;
                var vertices = geometry.bounds.ToVertices ();
                for (int j = 0; j < vertices.Length; j++) {
                    // mesh space -> world space -> reference frame space
                    var vertex = referenceFrame.PositionFromWorldSpace (mesh.transform.TransformPoint (vertices [j]));
                    bounds.Encapsulate (vertex);
                }
            }
            return bounds;
        }
    }
}
