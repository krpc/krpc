using System.Collections.Generic;
using UnityEngine;

namespace KRPC.SpaceCenter.ExtensionMethods
{
    static class PartInertiaExtensions
    {
        /// <summary>
        /// The world-space center of mass of the part. In flight this is the
        /// rigidbody's. In the editor it is the volume-weighted center of the
        /// part's colliders.
        /// </summary>
        public static Vector3 WorldCenterOfMass (this Part part)
        {
            if (part.HasPhysicsBody ())
                return part.rb.worldCenterOfMass;
            Vector3 local;
            float volume;
            if (TryColliderCenter (part, out local, out volume) && volume > 0f)
                return part.transform.TransformPoint (local);
            return part.transform.position;
        }

        /// <summary>
        /// The world-space center of mass of a vessel in the editor, weighted by
        /// each part's mass including resources and crew.
        /// </summary>
        public static Vector3 WorldCenterOfMass (this ShipConstruct ship)
        {
            var parts = ship.Parts;
            Vector3 com = Vector3.zero;
            float mass = 0f;
            for (int i = 0; i < parts.Count; i++) {
                var part = parts [i];
                var m = part.PhysicsMass ();
                if (m <= 0f)
                    continue;
                com += part.WorldCenterOfMass () * m;
                mass += m;
            }
            if (mass > 0f)
                return com / mass;
            var root = RootPart (parts);
            return root != null ? root.transform.position : Vector3.zero;
        }

        /// <summary>
        /// Inertia tensor of the part about its center of mass, in the part's
        /// axes, in tonne.m^2. Worked out from the colliders: each is treated as
        /// a solid of uniform density, and the part's mass is shared between them
        /// by volume. A part lighter than a tonne is given the inertia of a one
        /// tonne part of its shape, which is what KSP stores on the rigidbody
        /// once the vessel is in flight.
        /// </summary>
        public static Matrix4x4 ColliderInertiaTensor (this Part part)
        {
            var mass = part.PhysicsMass ();
            if (mass <= 0f)
                return Matrix4x4.zero;
            Matrix4x4 tensor;
            if (!TryColliderInertia (part, Mathf.Max (mass, 1f), out tensor))
                return Matrix4x4.zero;
            return tensor;
        }

        /// <summary>
        /// Inertia tensor of a vessel in the editor about its center of mass, in
        /// the root part's axes, in tonne.m^2. The parallel-axis contribution of
        /// each part is measured from the part's origin, not its center of mass,
        /// which is how KSP assembles a vessel's tensor in flight.
        /// </summary>
        public static Matrix4x4 ComputeInertiaTensor (this ShipConstruct ship)
        {
            var parts = ship.Parts;
            if (parts.Count == 0)
                return Matrix4x4.zero;

            var root = RootPart (parts);
            if (root == null)
                return Matrix4x4.zero;
            var vesselTransform = root.transform;
            var com = ship.WorldCenterOfMass ();

            Matrix4x4 inertiaTensor = Matrix4x4.zero;
            for (int i = 0; i < parts.Count; i++) {
                var part = parts [i];
                var mass = part.PhysicsMass ();
                if (mass <= 0f)
                    continue;

                var partTensor = part.ColliderInertiaTensor ();
                var rot = Quaternion.Inverse (vesselTransform.rotation) * part.transform.rotation;
                var rotMatrix = Matrix4x4.TRS (Vector3.zero, rot, Vector3.one);
                var invMatrix = Matrix4x4.TRS (Vector3.zero, Quaternion.Inverse (rot), Vector3.one);
                inertiaTensor = inertiaTensor.Add (rotMatrix * partTensor * invMatrix);

                var position = vesselTransform.InverseTransformDirection (part.transform.position - com);
                inertiaTensor = inertiaTensor.Add ((mass * position.sqrMagnitude).ToDiagonalMatrix ());
                inertiaTensor = inertiaTensor.Add (position.OuterProduct (-mass * position));
            }
            return inertiaTensor;
        }

        static Part RootPart (IList<Part> parts)
        {
            if (parts == null || parts.Count == 0)
                return null;
            var part = parts [0];
            while (part.parent != null)
                part = part.parent;
            return part;
        }

        static bool TryColliderCenter (Part part, out Vector3 localCom, out float volume)
        {
            localCom = Vector3.zero;
            volume = 0f;
            var solids = CollectSolids (part);
            for (int i = 0; i < solids.Count; i++) {
                var solid = solids [i];
                localCom += solid.Center * solid.Volume;
                volume += solid.Volume;
            }
            if (volume <= 0f)
                return false;
            localCom /= volume;
            return true;
        }

        static bool TryColliderInertia (Part part, float mass, out Matrix4x4 tensor)
        {
            tensor = Matrix4x4.zero;
            var solids = CollectSolids (part);
            float volume = 0f;
            for (int i = 0; i < solids.Count; i++)
                volume += solids [i].Volume;
            if (volume <= 0f)
                return false;

            Vector3 localCom = Vector3.zero;
            for (int i = 0; i < solids.Count; i++)
                localCom += solids [i].Center * solids [i].Volume;
            localCom /= volume;

            for (int i = 0; i < solids.Count; i++) {
                var solid = solids [i];
                var solidMass = mass * (solid.Volume / volume);
                var offset = solid.Center - localCom;
                tensor = tensor.Add (solid.InertiaPerMass.MultiplyScalar (solidMass));
                tensor = tensor.Add ((solidMass * offset.sqrMagnitude).ToDiagonalMatrix ());
                tensor = tensor.Add (offset.OuterProduct (-solidMass * offset));
            }
            return true;
        }

        struct Solid
        {
            public float Volume;
            public Vector3 Center;
            public Matrix4x4 InertiaPerMass;
        }

        static List<Solid> CollectSolids (Part part)
        {
            var solids = new List<Solid> ();
            var colliders = part.GetPartColliders ();
            if (colliders == null || colliders.Length == 0) {
                var found = part.FindModelComponents<Collider> ();
                if (found == null || found.Count == 0)
                    return solids;
                colliders = new Collider [found.Count];
                for (int j = 0; j < found.Count; j++)
                    colliders [j] = found [j];
            }
            for (int i = 0; i < colliders.Length; i++) {
                var collider = colliders [i];
                if (collider == null || !collider.enabled || collider.isTrigger)
                    continue;
                if (!collider.gameObject.activeInHierarchy)
                    continue;

                Solid solid;
                if (TryBox (part, collider as BoxCollider, out solid) ||
                    TrySphere (part, collider as SphereCollider, out solid) ||
                    TryCapsule (part, collider as CapsuleCollider, out solid) ||
                    TryMesh (part, collider as MeshCollider, out solid))
                    solids.Add (solid);
            }
            return solids;
        }

        static bool TryBox (Part part, BoxCollider box, out Solid solid)
        {
            solid = default (Solid);
            if (box == null)
                return false;
            var size = Vector3.Scale (box.size, Abs (box.transform.lossyScale));
            var volume = Mathf.Abs (size.x * size.y * size.z);
            if (volume <= 0f)
                return false;
            var x2 = size.x * size.x;
            var y2 = size.y * size.y;
            var z2 = size.z * size.z;
            var diagonal = new Vector3 ((y2 + z2) / 12f, (x2 + z2) / 12f, (x2 + y2) / 12f);
            solid.Volume = volume;
            solid.Center = PartLocalPoint (part, box.transform, box.center);
            solid.InertiaPerMass = RotateTensor (diagonal.ToDiagonalMatrix (), PartLocalRotation (part, box.transform));
            return true;
        }

        static bool TrySphere (Part part, SphereCollider sphere, out Solid solid)
        {
            solid = default (Solid);
            if (sphere == null)
                return false;
            var radii = sphere.radius * Abs (sphere.transform.lossyScale);
            var volume = (4f / 3f) * Mathf.PI * radii.x * radii.y * radii.z;
            if (volume <= 0f)
                return false;
            var x2 = radii.x * radii.x;
            var y2 = radii.y * radii.y;
            var z2 = radii.z * radii.z;
            var diagonal = new Vector3 ((y2 + z2) / 5f, (x2 + z2) / 5f, (x2 + y2) / 5f);
            solid.Volume = volume;
            solid.Center = PartLocalPoint (part, sphere.transform, sphere.center);
            solid.InertiaPerMass = RotateTensor (diagonal.ToDiagonalMatrix (), PartLocalRotation (part, sphere.transform));
            return true;
        }

        static bool TryCapsule (Part part, CapsuleCollider capsule, out Solid solid)
        {
            solid = default (Solid);
            if (capsule == null)
                return false;
            var lossy = Abs (capsule.transform.lossyScale);
            float heightScale;
            float radiusScale;
            switch (capsule.direction) {
            case 0:
                heightScale = lossy.x;
                radiusScale = Mathf.Max (lossy.y, lossy.z);
                break;
            case 2:
                heightScale = lossy.z;
                radiusScale = Mathf.Max (lossy.x, lossy.y);
                break;
            default:
                heightScale = lossy.y;
                radiusScale = Mathf.Max (lossy.x, lossy.z);
                break;
            }
            var radius = capsule.radius * radiusScale;
            var height = Mathf.Max (capsule.height * heightScale, 2f * radius);
            var cylinderHeight = Mathf.Max (0f, height - 2f * radius);
            var r2 = radius * radius;
            var vCyl = Mathf.PI * r2 * cylinderHeight;
            var vSph = (4f / 3f) * Mathf.PI * r2 * radius;
            var volume = vCyl + vSph;
            if (volume <= 0f)
                return false;
            var mCyl = vCyl / volume;
            var mSph = vSph / volume;
            var along = 0.5f * mCyl * r2 + 0.4f * mSph * r2;
            var halfH = 0.5f * cylinderHeight;
            var perp = mCyl * (cylinderHeight * cylinderHeight / 12f + r2 / 4f) + mSph * (0.4f * r2 + halfH * halfH);
            Vector3 diagonal;
            switch (capsule.direction) {
            case 0:
                diagonal = new Vector3 (along, perp, perp);
                break;
            case 2:
                diagonal = new Vector3 (perp, perp, along);
                break;
            default:
                diagonal = new Vector3 (perp, along, perp);
                break;
            }
            solid.Volume = volume;
            solid.Center = PartLocalPoint (part, capsule.transform, capsule.center);
            solid.InertiaPerMass = RotateTensor (diagonal.ToDiagonalMatrix (), PartLocalRotation (part, capsule.transform));
            return true;
        }

        static bool TryMesh (Part part, MeshCollider meshCollider, out Solid solid)
        {
            solid = default (Solid);
            if (meshCollider == null)
                return false;
            var mesh = meshCollider.sharedMesh;
            if (mesh == null)
                return false;
            var vertices = mesh.vertices;
            var triangles = mesh.triangles;
            if (vertices == null || triangles == null || triangles.Length < 3)
                return false;

            var colliderTransform = meshCollider.transform;
            float volume = 0f;
            var firstMoment = Vector3.zero;
            float ixx = 0f, iyy = 0f, izz = 0f, ixy = 0f, ixz = 0f, iyz = 0f;
            for (int i = 0; i < triangles.Length; i += 3) {
                var a = PartLocalPoint (part, colliderTransform, vertices [triangles [i]]);
                var b = PartLocalPoint (part, colliderTransform, vertices [triangles [i + 1]]);
                var c = PartLocalPoint (part, colliderTransform, vertices [triangles [i + 2]]);
                AddTetrahedron (a, b, c, ref volume, ref firstMoment,
                    ref ixx, ref iyy, ref izz, ref ixy, ref ixz, ref iyz);
            }
            if (volume < 0f) {
                volume = -volume;
                firstMoment = -firstMoment;
                ixx = -ixx;
                iyy = -iyy;
                izz = -izz;
                ixy = -ixy;
                ixz = -ixz;
                iyz = -iyz;
            }
            if (volume <= 1e-8f)
                return false;

            var com = firstMoment / volume;
            // Shift the origin-based tensor onto the mesh's own center of mass.
            ixx -= volume * (com.y * com.y + com.z * com.z);
            iyy -= volume * (com.x * com.x + com.z * com.z);
            izz -= volume * (com.x * com.x + com.y * com.y);
            ixy += volume * com.x * com.y;
            ixz += volume * com.x * com.z;
            iyz += volume * com.y * com.z;

            var tensor = Matrix4x4.zero;
            tensor [0, 0] = ixx / volume;
            tensor [0, 1] = ixy / volume;
            tensor [0, 2] = ixz / volume;
            tensor [1, 0] = ixy / volume;
            tensor [1, 1] = iyy / volume;
            tensor [1, 2] = iyz / volume;
            tensor [2, 0] = ixz / volume;
            tensor [2, 1] = iyz / volume;
            tensor [2, 2] = izz / volume;
            solid.Volume = volume;
            solid.Center = com;
            solid.InertiaPerMass = tensor;
            return true;
        }

        /// <summary>
        /// Accumulate volume, first moment and the inertia tensor about the origin
        /// for the tetrahedron that a triangle makes with the origin, density 1.
        /// </summary>
        static void AddTetrahedron (
            Vector3 a, Vector3 b, Vector3 c,
            ref float volume, ref Vector3 firstMoment,
            ref float ixx, ref float iyy, ref float izz,
            ref float ixy, ref float ixz, ref float iyz)
        {
            var v6 = Vector3.Dot (a, Vector3.Cross (b, c));
            var v = v6 / 6f;
            volume += v;
            firstMoment += (a + b + c) * (v * 0.25f);

            var xx = a.x * a.x + b.x * b.x + c.x * c.x + a.x * b.x + a.x * c.x + b.x * c.x;
            var yy = a.y * a.y + b.y * b.y + c.y * c.y + a.y * b.y + a.y * c.y + b.y * c.y;
            var zz = a.z * a.z + b.z * b.z + c.z * c.z + a.z * b.z + a.z * c.z + b.z * c.z;
            ixx += v6 * (yy + zz) / 60f;
            iyy += v6 * (xx + zz) / 60f;
            izz += v6 * (xx + yy) / 60f;

            var xy = 2f * (a.x * a.y + b.x * b.y + c.x * c.y) + a.x * b.y + a.y * b.x + a.x * c.y + a.y * c.x + b.x * c.y + b.y * c.x;
            var xz = 2f * (a.x * a.z + b.x * b.z + c.x * c.z) + a.x * b.z + a.z * b.x + a.x * c.z + a.z * c.x + b.x * c.z + b.z * c.x;
            var yz = 2f * (a.y * a.z + b.y * b.z + c.y * c.z) + a.y * b.z + a.z * b.y + a.y * c.z + a.z * c.y + b.y * c.z + b.z * c.y;
            ixy -= v6 * xy / 120f;
            ixz -= v6 * xz / 120f;
            iyz -= v6 * yz / 120f;
        }

        static Vector3 PartLocalPoint (Part part, Transform from, Vector3 localPoint)
        {
            return part.transform.InverseTransformPoint (from.TransformPoint (localPoint));
        }

        static Quaternion PartLocalRotation (Part part, Transform from)
        {
            return Quaternion.Inverse (part.transform.rotation) * from.rotation;
        }

        static Matrix4x4 RotateTensor (Matrix4x4 tensor, Quaternion rotation)
        {
            var rot = Matrix4x4.TRS (Vector3.zero, rotation, Vector3.one);
            var inv = Matrix4x4.TRS (Vector3.zero, Quaternion.Inverse (rotation), Vector3.one);
            return rot * tensor * inv;
        }

        static Vector3 Abs (Vector3 v)
        {
            return new Vector3 (Mathf.Abs (v.x), Mathf.Abs (v.y), Mathf.Abs (v.z));
        }
    }
}
