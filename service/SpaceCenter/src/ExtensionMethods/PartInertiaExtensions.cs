using System.Collections.Generic;
using UnityEngine;

namespace KRPC.SpaceCenter.ExtensionMethods
{
    static class PartInertiaExtensions
    {
        /// <summary>
        /// Integrals of a collision mesh in its own unscaled space, keyed by
        /// <see cref="Object.GetInstanceID"/>. Volume, centroid and covariance
        /// belong to the mesh; the collider's scale is applied when a solid is
        /// built from the cache.
        /// </summary>
        static readonly Dictionary<int, MeshIntegral> MeshIntegrals =
            new Dictionary<int, MeshIntegral> ();

        /// <summary>
        /// The world-space center of mass of a part in the editor:
        /// <c>transform.position + rotation * CoMOffset</c>, which is what the
        /// editor's own CoM marker sums and what the rigidbody uses in flight.
        /// </summary>
        public static Vector3 WorldCenterOfMass (this Part part)
        {
            return part.transform.position + part.transform.rotation * part.CoMOffset;
        }

        /// <summary>
        /// The world-space center of mass of a vessel in the editor, weighted by
        /// each part's mass including resources, crew and physicsless children.
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
            var transform = ship.VesselTransform ();
            return transform != null ? transform.position : Vector3.zero;
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
        /// the root part's reference transform, in tonne.m^2. Those are the same
        /// pitch, roll and yaw axes a vessel reports in flight. The parallel-axis
        /// contribution of each part is measured from the part's origin, not its
        /// center of mass, which is how KSP assembles a vessel's tensor in flight.
        /// </summary>
        public static Matrix4x4 ComputeInertiaTensor (this ShipConstruct ship)
        {
            var parts = ship.Parts;
            if (parts.Count == 0)
                return Matrix4x4.zero;

            var vesselTransform = ship.VesselTransform ();
            if (vesselTransform == null)
                return Matrix4x4.zero;
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

        /// <summary>
        /// The transform <see cref="ComputeInertiaTensor"/> reports axes in. The
        /// root part's reference transform, falling back to its model transform.
        /// </summary>
        public static Transform VesselTransform (this ShipConstruct ship)
        {
            var root = RootPart (ship.Parts);
            if (root == null)
                return null;
            var reference = root.GetReferenceTransform ();
            return reference != null ? reference : root.transform;
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

        struct MeshIntegral
        {
            public bool IsBox;
            public Vector3 BoxCenter;
            public Vector3 BoxSize;
            public float Volume;
            public Vector3 FirstMoment;
            public float Ixx;
            public float Iyy;
            public float Izz;
            public float Ixy;
            public float Ixz;
            public float Iyz;
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
                if (collider == null || !collider.enabled)
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
            return SolidFromBox (part, box.transform, box.center, box.size, out solid);
        }

        static bool SolidFromBox (
            Part part, Transform from, Vector3 localCenter, Vector3 localSize, out Solid solid)
        {
            solid = default (Solid);
            var size = Vector3.Scale (localSize, Abs (from.lossyScale));
            var volume = Mathf.Abs (size.x * size.y * size.z);
            if (volume <= 0f)
                return false;
            var x2 = size.x * size.x;
            var y2 = size.y * size.y;
            var z2 = size.z * size.z;
            var diagonal = new Vector3 ((y2 + z2) / 12f, (x2 + z2) / 12f, (x2 + y2) / 12f);
            solid.Volume = volume;
            solid.Center = PartLocalPoint (part, from, localCenter);
            solid.InertiaPerMass = RotateTensor (diagonal.ToDiagonalMatrix (), PartLocalRotation (part, from));
            return true;
        }

        static bool TrySphere (Part part, SphereCollider sphere, out Solid solid)
        {
            solid = default (Solid);
            if (sphere == null)
                return false;
            // A SphereCollider stays a sphere; Unity scales its radius by the
            // largest absolute component of lossyScale, not per axis.
            var radius = sphere.radius * Max (Abs (sphere.transform.lossyScale));
            var volume = (4f / 3f) * Mathf.PI * radius * radius * radius;
            if (volume <= 0f)
                return false;
            var diagonal = Vector3.one * (0.4f * radius * radius);
            solid.Volume = volume;
            solid.Center = PartLocalPoint (part, sphere.transform, sphere.center);
            solid.InertiaPerMass = diagonal.ToDiagonalMatrix ();
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

            MeshIntegral integral;
            if (!TryGetMeshIntegral (mesh, out integral))
                return false;
            if (integral.IsBox)
                return SolidFromBox (part, meshCollider.transform, integral.BoxCenter, integral.BoxSize, out solid);

            var scale = meshCollider.transform.lossyScale;
            var ax = Mathf.Abs (scale.x);
            var ay = Mathf.Abs (scale.y);
            var az = Mathf.Abs (scale.z);
            var det = ax * ay * az;
            if (det <= 1e-12f)
                return false;

            var volume = integral.Volume * det;
            if (volume <= 1e-8f)
                return false;

            // Second moments about the origin, then scale: x'^2 dV' = |det| sx^2 x^2 dV.
            var pxx = (integral.Iyy + integral.Izz - integral.Ixx) * 0.5f;
            var pyy = (integral.Ixx + integral.Izz - integral.Iyy) * 0.5f;
            var pzz = (integral.Ixx + integral.Iyy - integral.Izz) * 0.5f;
            var ixx = det * (scale.y * scale.y * pyy + scale.z * scale.z * pzz);
            var iyy = det * (scale.x * scale.x * pxx + scale.z * scale.z * pzz);
            var izz = det * (scale.x * scale.x * pxx + scale.y * scale.y * pyy);
            var ixy = det * scale.x * scale.y * integral.Ixy;
            var ixz = det * scale.x * scale.z * integral.Ixz;
            var iyz = det * scale.y * scale.z * integral.Iyz;

            var firstMoment = new Vector3 (
                                  scale.x * det * integral.FirstMoment.x,
                                  scale.y * det * integral.FirstMoment.y,
                                  scale.z * det * integral.FirstMoment.z);
            var com = firstMoment / volume;
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

            var unscaledCom = integral.FirstMoment / integral.Volume;
            solid.Volume = volume;
            solid.Center = PartLocalPoint (part, meshCollider.transform, unscaledCom);
            solid.InertiaPerMass = RotateTensor (tensor, PartLocalRotation (part, meshCollider.transform));
            return true;
        }

        static bool TryGetMeshIntegral (Mesh mesh, out MeshIntegral integral)
        {
            var id = mesh.GetInstanceID ();
            if (MeshIntegrals.TryGetValue (id, out integral))
                return integral.Volume > 1e-8f;

            if (!mesh.isReadable) {
                var bounds = mesh.bounds;
                integral = new MeshIntegral {
                    IsBox = true,
                    BoxCenter = bounds.center,
                    BoxSize = bounds.size,
                    Volume = Mathf.Abs (bounds.size.x * bounds.size.y * bounds.size.z)
                };
                MeshIntegrals [id] = integral;
                return integral.Volume > 0f;
            }

            var vertices = mesh.vertices;
            var triangles = mesh.triangles;
            if (vertices == null || triangles == null || triangles.Length < 3) {
                integral = default (MeshIntegral);
                return false;
            }

            float volume = 0f;
            var firstMoment = Vector3.zero;
            float ixx = 0f, iyy = 0f, izz = 0f, ixy = 0f, ixz = 0f, iyz = 0f;
            for (int i = 0; i < triangles.Length; i += 3) {
                AddTetrahedron (
                    vertices [triangles [i]],
                    vertices [triangles [i + 1]],
                    vertices [triangles [i + 2]],
                    ref volume, ref firstMoment,
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
            integral = new MeshIntegral {
                Volume = volume,
                FirstMoment = firstMoment,
                Ixx = ixx,
                Iyy = iyy,
                Izz = izz,
                Ixy = ixy,
                Ixz = ixz,
                Iyz = iyz
            };
            MeshIntegrals [id] = integral;
            return volume > 1e-8f;
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

        static float Max (Vector3 v)
        {
            return Mathf.Max (v.x, Mathf.Max (v.y, v.z));
        }
    }
}
