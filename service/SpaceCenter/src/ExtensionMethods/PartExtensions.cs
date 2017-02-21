using System;
using System.Diagnostics.CodeAnalysis;
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
        [SuppressMessage ("Gendarme.Rules.Design.Generic", "AvoidMethodWithUnusedGenericTypeRule")]
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
        [SuppressMessage ("Gendarme.Rules.Design.Generic", "AvoidMethodWithUnusedGenericTypeRule")]
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
        /// Returns true if the part is massless
        /// </summary>
        public static bool IsMassless (this Part part)
        {
            return part.physicalSignificance == Part.PhysicalSignificance.NONE || part.HasModule<LaunchClamp> ();
        }

        /// <summary>
        /// The mass of the part, including resources, in kg.
        /// </summary>
        public static float WetMass (this Part part)
        {
            return !part.IsMassless () && part.rb != null ? part.rb.mass * 1000f : 0f;
        }

        /// <summary>
        /// The mass of the part, excluding resources.
        /// </summary>
        public static float DryMass (this Part part)
        {
            return !part.IsMassless () && part.rb != null ? Mathf.Max (0f, (part.rb.mass - part.resourceMass) * 1000f) : 0f;
        }

        /// <summary>
        /// Returns the index of the stage in which the part will be decoupled, or -1 if it is never decoupled.
        /// Transversed the tree of parts from the desired part to the root, and finds the activation stage
        /// for the first decoupler that will decouple the part (the one with the highest stage number)
        /// </summary>
        [SuppressMessage ("Gendarme.Rules.Maintainability", "AvoidComplexMethodsRule")]
        public static int DecoupledAt (this Part part)
        {
            int stage = -1;
            do {
                int candidate = -1;
                var parent = part.parent;
                var moduleDecouple = part.Module<ModuleDecouple> ();
                var moduleAnchoredDecoupler = part.Module<ModuleAnchoredDecoupler> ();

                // If the part will decouple itself from its parent, use the parts activation stage
                if (part.HasModule<LaunchClamp> ()) {
                    candidate = part.inverseStage;
                } else if (moduleDecouple != null && moduleDecouple.isEnabled) {
                    if (moduleDecouple.isOmniDecoupler)
                        candidate = part.inverseStage;
                    else if (parent != null && moduleDecouple.ExplosiveNode != null && moduleDecouple.ExplosiveNode.attachedPart == parent)
                        candidate = part.inverseStage;
                } else if (moduleAnchoredDecoupler != null && moduleAnchoredDecoupler.isEnabled) {
                    if (parent != null && moduleAnchoredDecoupler.ExplosiveNode != null && moduleAnchoredDecoupler.ExplosiveNode.attachedPart == parent)
                        candidate = part.inverseStage;
                }

                // If the part will be decoupled by its parent, use the parents activation stage
                if (candidate == -1 && parent != null) {
                    if (moduleDecouple != null) {
                        if (moduleDecouple.isOmniDecoupler && moduleDecouple.isEnabled)
                            candidate = parent.inverseStage;
                        else if (moduleDecouple.ExplosiveNode != null && moduleDecouple.ExplosiveNode.attachedPart == part)
                            candidate = parent.inverseStage;
                    } else if (moduleAnchoredDecoupler != null && moduleAnchoredDecoupler.isEnabled) {
                        if (moduleAnchoredDecoupler.ExplosiveNode != null && moduleAnchoredDecoupler.ExplosiveNode.attachedPart == part)
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
            return part.rb != null ? part.rb.worldCenterOfMass : part.transform.position;
        }

        /// <summary>
        /// Computes the axis-aligned bounding box for a part in the given reference frame.
        /// </summary>
        /// <remarks>
        /// This is an expensive calculation. It iterates over the parts collider meshes
        /// to compute a tight axis-aligned bounding box.
        /// It does not use part.collider.bounds, as this is aligned to world space and
        /// would not provide a tight bounding box in the given reference frame.
        /// </remarks>
        public static Bounds GetBounds (this Part part, ReferenceFrame referenceFrame)
        {
            var bounds = new Bounds (referenceFrame.PositionFromWorldSpace (part.WCoM), Vector3.zero);
            var meshes = part.GetComponentsInChildren<MeshFilter> ();
            for (int i = 0; i < meshes.Length; i++) {
                var mesh = meshes [i];
                var vertices = mesh.mesh.bounds.ToVertices ();
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
