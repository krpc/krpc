using System;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.SpaceCenter.ExternalAPI;
using UnityEngine;

namespace KRPC.SpaceCenter.AutoPilot
{
    /// <summary>
    /// The steady angular velocity the spare control torque holds on each axis, worked out from
    /// the game's aerodynamics. Pitch and yaw: the incidence at which the aerodynamic restoring
    /// moment uses up the spare torque, and the rate the aerodynamic force, thrust and weight there
    /// turn the flight path at. The weight helps a turn toward it and opposes a turn away from it,
    /// so pitch and yaw each have a limit for either direction. Roll: the rate at which the
    /// aerodynamic damping moment uses up the spare torque. Commanding a faster rate saturates the
    /// control surfaces, and on an aircraft with little authority that drives a limit cycle.
    /// </summary>
    sealed class AerodynamicRateLimit
    {
        // Seconds between estimates. Each one evaluates the vessel's aerodynamics about 75 times.
        const double UpdateInterval = 0.5;
        // Time constant of the trim, the slow average of the command, in seconds
        const double TrimTimeConstant = 3.0;
        // Largest incidence searched, in degrees
        const double MaxIncidence = 30.0;
        // The time the nose may take to lean to the incidence the spare torque holds, in seconds.
        // The pitch and yaw limits never fall below the rate that does it, so the vessel still
        // corrects a small error in a direction it cannot sustain a turn in. It is long enough
        // that the floor stays below the steady rate where one exists.
        const double IncidenceLeanTime = 10.0;
        // Largest roll rate searched, in rad/s
        const double MaxRollRate = 3.0;
        const int Bisections = 12;
        const int RateIterations = 2;

        static readonly Vector3d Unlimited =
            new Vector3d (double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity);

        Vector3d trim;
        double nextUpdate;

        public AerodynamicRateLimit ()
        {
            Reset ();
        }

        /// <summary>
        /// The angular velocity limit for a positive rotation about each of the pitch, roll and
        /// yaw axes, in the sense of <c>QuaternionD.AngleAxis</c>, in rad/s. Infinite out of the
        /// air, and with Ferram Aerospace Research installed.
        /// </summary>
        public Vector3d PositiveLimit { get; private set; }

        /// <summary>
        /// The angular velocity limit for a negative rotation about each axis, in rad/s.
        /// </summary>
        public Vector3d NegativeLimit { get; private set; }

        public void Reset ()
        {
            trim = Vector3d.zero;
            nextUpdate = 0;
            PositiveLimit = Unlimited;
            NegativeLimit = Unlimited;
        }

        /// <summary>
        /// Advance the trim by one tick of the delivered command, given as a fraction of the
        /// available torque on each axis, and refresh the limit when it is due. Without
        /// <paramref name="estimate"/> the limit is infinite, and the next estimate is due at once.
        /// </summary>
        public void Update (Services.Vessel vessel, Vector3d torque, Vector3d command, double dt, bool estimate)
        {
            var beta = 1.0 - Math.Exp (-dt / TrimTimeConstant);
            trim += beta * (command - trim);
            if (!estimate) {
                nextUpdate = 0;
                PositiveLimit = Unlimited;
                NegativeLimit = Unlimited;
                return;
            }
            if (Time.fixedTime < nextUpdate)
                return;
            nextUpdate = Time.fixedTime + UpdateInterval;
            Estimate (vessel.InternalVessel, torque, vessel.Thrust);
        }

        void Estimate (Vessel vessel, Vector3d torque, double thrust)
        {
            PositiveLimit = Unlimited;
            NegativeLimit = Unlimited;
            var body = vessel.mainBody;
            if (FAR.IsAvailable || !body.atmosphere || vessel.dynamicPressurekPa <= 0)
                return;
            var air = new Air (vessel);
            if (air.Speed < 1)
                return;
            var transform = vessel.ReferenceTransform;
            var right = (Vector3d)transform.right;
            var forward = (Vector3d)transform.up;
            var down = (Vector3d)transform.forward;
            var spare = new Vector3d (
                torque.x * (1 - Math.Min (1.0, Math.Abs (trim.x))),
                torque.y * (1 - Math.Min (1.0, Math.Abs (trim.y))),
                torque.z * (1 - Math.Min (1.0, Math.Abs (trim.z))));
            // Weight in N
            var weight = (Vector3d)vessel.graviticAcceleration * air.Mass;
            double pitchPositive, pitchNegative, yawPositive, yawNegative;
            IncidenceRate (air, right, -down, forward, spare.x, thrust, weight, out pitchPositive, out pitchNegative);
            IncidenceRate (air, down, right, forward, spare.z, thrust, weight, out yawPositive, out yawNegative);
            var roll = RollRate (air, forward, spare.y);
            PositiveLimit = new Vector3d (pitchPositive, roll, yawPositive);
            NegativeLimit = new Vector3d (pitchNegative, roll, yawNegative);
        }

        /// <summary>
        /// The flight path rates a pitch or yaw incidence holds, for a positive and a negative
        /// rotation. The vessel turns about <paramref name="axis"/>, and the aerodynamic force
        /// along <paramref name="normal"/> and the thrust across the flight path turn the flight
        /// path, with <paramref name="weight"/> adding to a turn toward it.
        /// </summary>
        static void IncidenceRate (Air air, Vector3d axis, Vector3d normal, Vector3d forward, double spare,
            double thrust, Vector3d weight, out double positive, out double negative)
        {
            positive = 0;
            negative = 0;
            if (spare <= 0)
                return;
            // The weight across the flight path, toward the side a positive rotation turns the nose to
            var nose = QuaternionD.AngleAxis (1, axis) * forward - forward;
            var weightAcross = Vector3d.Dot (weight, nose.normalized);
            // Turn the direction that pushes the vessel along the normal
            var sign = Vector3d.Dot (air.Force (axis, 1, 0), normal) >= Vector3d.Dot (air.Force (axis, 0, 0), normal) ? 1.0 : -1.0;
            var trimMoment = air.Moment (axis, 0, 0);
            var rate = 0.0;
            var incidence = MaxIncidence;
            for (int iteration = 0; iteration < RateIterations; iteration++) {
                var damping = rate;
                Func<double, bool> withinSpare = angle =>
                    Math.Abs (air.Moment (axis, sign * angle, sign * damping) - trimMoment) < spare;
                incidence = MaxIncidence;
                if (!withinSpare (MaxIncidence)) {
                    double low = 0, high = MaxIncidence;
                    for (int i = 0; i < Bisections; i++) {
                        var middle = 0.5 * (low + high);
                        if (withinSpare (middle))
                            low = middle;
                        else
                            high = middle;
                    }
                    incidence = 0.5 * (low + high);
                }
                var force = Math.Abs (Vector3d.Dot (air.Force (axis, sign * incidence, 0), normal)) +
                    thrust * Math.Sin (incidence * Math.PI / 180);
                positive = Math.Max (0.0, (force + weightAcross) / (air.Mass * air.Speed));
                negative = Math.Max (0.0, (force - weightAcross) / (air.Mass * air.Speed));
                rate = Math.Max (positive, negative);
            }
            var lean = incidence * Math.PI / 180 / IncidenceLeanTime;
            positive = Math.Max (positive, lean);
            negative = Math.Max (negative, lean);
        }

        /// <summary>
        /// The roll rate at which the aerodynamic damping moment uses up the spare torque.
        /// </summary>
        static double RollRate (Air air, Vector3d axis, double spare)
        {
            if (spare <= 0)
                return 0;
            var still = air.Moment (axis, 0, 0);
            Func<double, bool> withinSpare = rate => Math.Abs (air.Moment (axis, 0, rate) - still) < spare;
            if (withinSpare (MaxRollRate))
                return MaxRollRate;
            double low = 0, high = MaxRollRate;
            for (int i = 0; i < Bisections; i++) {
                var middle = 0.5 * (low + high);
                if (withinSpare (middle))
                    low = middle;
                else
                    high = middle;
            }
            return 0.5 * (low + high);
        }

        /// <summary>
        /// The vessel's present airflow, and its aerodynamics evaluated turned about an axis.
        /// </summary>
        sealed class Air
        {
            readonly Vessel vessel;
            readonly Vector3d com;
            readonly Vector3d velocity;
            readonly double ut;

            public Air (Vessel vessel)
            {
                this.vessel = vessel;
                com = vessel.CoM;
                velocity = vessel.srf_velocity + vessel.mainBody.getRFrmVel (com);
                ut = Planetarium.GetUniversalTime ();
                Speed = vessel.srf_velocity.magnitude;
                Mass = vessel.totalMass * 1000;
            }

            public double Speed { get; private set; }

            // In kg
            public double Mass { get; private set; }

            StockAerodynamics.AerodynamicWrench Wrench (Vector3d axis, double angle, double rate)
            {
                var turn = QuaternionD.AngleAxis (angle, axis);
                return StockAerodynamics.SimAeroWrench (
                    vessel.mainBody, vessel, velocity, axis * rate,
                    com, turn, ut, true, false);
            }

            // The force, in N, with the vessel turned by angle degrees about the axis and
            // turning at rate rad/s about it
            public Vector3d Force (Vector3d axis, double angle, double rate)
            {
                return Wrench (axis, angle, rate).Force;
            }

            // The moment about the axis, in N.m
            public double Moment (Vector3d axis, double angle, double rate)
            {
                return Vector3d.Dot (Wrench (axis, angle, rate).Torque, axis);
            }
        }
    }
}
