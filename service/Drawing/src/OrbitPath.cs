using System;
using System.Collections.Generic;
using KRPC.Service.Attributes;
using KRPC.Utils;

namespace KRPC.Drawing
{
    /// <summary>
    /// The path of an orbit. Created using <see cref="Drawing.AddOrbit" />.
    /// </summary>
    [KRPCClass (Service = "Drawing")]
    public class OrbitPath : Path<OrbitPath>
    {
        // The number of points in a full turn of the orbit
        const int pointsPerTurn = 256;
        const int minPoints = 8;
        // The fraction of the angle to its asymptotes that a hyperbola is drawn out to, when it
        // does not leave a sphere of influence first
        const double asymptoteFraction = 0.99;

        double startTrueAnomaly;
        double endTrueAnomaly;

        internal OrbitPath (SpaceCenter.Services.Orbit orbit, bool visible)
        {
            Orbit = orbit;
            WholeOrbit = true;
            Visible = visible;
        }

        /// <summary>
        /// Whether the orbit can be drawn.
        /// </summary>
        protected override bool CanBeDrawn {
            get {
                return GameObjectState == GameObjectState.Live &&
                Orbit.GameObjectState == GameObjectState.Live;
            }
        }

        /// <summary>
        /// Whether the path is a whole closed orbit.
        /// </summary>
        protected override bool Closed {
            get { return WholeOrbit && MaxTrueAnomaly (Orbit.InternalOrbit) >= Math.PI; }
        }

        // The largest true anomaly at which the orbit is inside the sphere of influence of its body
        static double MaxTrueAnomaly (global::Orbit orbit)
        {
            var eccentricity = orbit.eccentricity;
            var maxTrueAnomaly = Math.PI;
            if (eccentricity >= 1)
                maxTrueAnomaly = asymptoteFraction * Math.Acos (-1 / eccentricity);
            var soi = orbit.referenceBody.sphereOfInfluence;
            if (eccentricity > 0 && !double.IsInfinity (soi)) {
                var cosine = (orbit.semiLatusRectum / soi - 1) / eccentricity;
                if (cosine > -1)
                    maxTrueAnomaly = Math.Min (maxTrueAnomaly, Math.Acos (Math.Min (1, cosine)));
            }
            return maxTrueAnomaly;
        }

        /// <summary>
        /// The points of the orbit, in world space, in the direction of travel.
        /// </summary>
        protected override IList<Vector3d> WorldPoints ()
        {
            var orbit = Orbit.InternalOrbit;
            var maxTrueAnomaly = MaxTrueAnomaly (orbit);
            double start, end;
            if (WholeOrbit) {
                start = -maxTrueAnomaly;
                end = maxTrueAnomaly;
            } else {
                start = startTrueAnomaly;
                end = endTrueAnomaly;
                if (maxTrueAnomaly >= Math.PI) {
                    // A closed orbit runs on from the start to the next time it reaches the end,
                    // at most one turn later
                    var turn = 2 * Math.PI;
                    end = start + (((end - start) % turn) + turn) % turn;
                    if (end <= start)
                        end += turn;
                } else {
                    start = Math.Max (-maxTrueAnomaly, Math.Min (maxTrueAnomaly, start));
                    end = Math.Max (start, Math.Min (maxTrueAnomaly, end));
                }
            }
            var closed = WholeOrbit && maxTrueAnomaly >= Math.PI;
            var count = Math.Max (minPoints, (int)Math.Ceiling (pointsPerTurn * (end - start) / (2 * Math.PI)));
            // The path closes a whole orbit back onto its first point
            var last = closed ? count - 1 : count;
            var points = new List<Vector3d> (last + 1);
            for (int i = 0; i <= last; i++)
                points.Add (orbit.getPositionFromTrueAnomaly (start + (end - start) * i / count));
            return points;
        }

        /// <summary>
        /// The orbit.
        /// </summary>
        [KRPCProperty]
        public SpaceCenter.Services.Orbit Orbit { get; set; }

        /// <summary>
        /// Whether the whole orbit is drawn, up to where it leaves the sphere of influence of
        /// its body. When false, the orbit is drawn from <see cref="StartTrueAnomaly" />
        /// to <see cref="EndTrueAnomaly" />.
        /// </summary>
        [KRPCProperty]
        public bool WholeOrbit { get; set; }

        /// <summary>
        /// The true anomaly at which the path starts, in radians.
        /// </summary>
        /// <remarks>
        /// Setting this sets <see cref="WholeOrbit" /> to false.
        /// </remarks>
        [KRPCProperty]
        public double StartTrueAnomaly {
            get { return startTrueAnomaly; }
            set {
                startTrueAnomaly = value;
                WholeOrbit = false;
            }
        }

        /// <summary>
        /// The true anomaly at which the path ends, in radians.
        /// </summary>
        /// <remarks>
        /// Setting this sets <see cref="WholeOrbit" /> to false.
        /// </remarks>
        [KRPCProperty]
        public double EndTrueAnomaly {
            get { return endTrueAnomaly; }
            set {
                endTrueAnomaly = value;
                WholeOrbit = false;
            }
        }
    }
}
