using System;
using KRPC.Service.Attributes;
using KRPC.Utils;

namespace KRPC.MechJeb.Maneuver
{
    /// <summary>
    /// An interplanetary transfer to the target body, at the departure that needs the least
    /// delta-v before <see cref="MaxArrivalTime"/>.
    /// </summary>
    /// <remarks>
    /// MechJeb searches for the departure in the background. The first call to
    /// <c>MakeNodes</c> starts the search, and throws an <see cref="OperationException"/>
    /// until it finishes. See <see cref="Searching"/>.
    /// </remarks>
    [KRPCClass (Service = "MechJeb")]
    public sealed class OperationAdvancedTransfer : Operation
    {
        // MechJeb's mode that searches up to the arrival time, rather than the porkchop plot
        // its window selects a point on
        const int limitedTime = 0;

        static Field<object, int> selectionMode;
        static Field<object, double> maxArrivalTime;
        static Field<object, bool> includeCaptureBurn;
        static Field<object, double> periapsisHeight;
        static Field<object, object> worker;
        static Field<object, global::CelestialBody> lastTargetCelestial;
        static Action<object, global::Orbit, global::Orbit, double> computeTimes;
        static Func<object, bool> finished;
        static Func<object, int> progress;
        static Field<object, bool> stop;
        static Func<object, global::Orbit> originOrbit;
        static Func<object, global::Orbit> destinationOrbit;

        internal static new void Load ()
        {
            var type = Bindings.Type ("MuMech.OperationAdvancedTransfer");
            selectionMode = type.Field<object, int> ("selectionMode");
            maxArrivalTime = Bindings.EditableDouble (type, "maxArrivalTime");
            includeCaptureBurn = type.Field<object, bool> ("includeCaptureBurn");
            periapsisHeight = Bindings.EditableDouble (type, "periapsisHeight");
            worker = type.Field<object, object> ("worker");
            lastTargetCelestial = type.Field<object, global::CelestialBody> ("lastTargetCelestial");
            computeTimes = type.Method<Action<object, global::Orbit, global::Orbit, double>> ("ComputeTimes");
            var calculator = Bindings.Type ("MuMech.TransferCalculator");
            finished = calculator.Getter<object, bool> ("Finished");
            progress = calculator.Getter<object, int> ("Progress");
            stop = calculator.Field<object, bool> ("Stop");
            originOrbit = calculator.Getter<object, global::Orbit> ("OriginOrbit");
            destinationOrbit = calculator.Getter<object, global::Orbit> ("DestinationOrbit");
        }

        // The target orbit that MaxArrivalTime was set or computed for
        global::Orbit arrivalTarget;

        internal OperationAdvancedTransfer () : base ("OperationAdvancedTransfer")
        {
        }

        /// <summary>
        /// The latest arrival time, as a time from now, in seconds.
        /// </summary>
        /// <remarks>
        /// <c>MakeNodes</c> computes a default for a new target, from the synodic period and the
        /// transfer time, unless it was set while the target was selected.
        /// </remarks>
        [KRPCProperty]
        public double MaxArrivalTime {
            get { return maxArrivalTime.Get (Instance); }
            set {
                var instance = Instance;
                if (maxArrivalTime.Get (instance) != value)
                    StopSearch (instance);
                maxArrivalTime.Set (instance, value);
                var targetController = MechJeb.TargetController;
                targetController.Update ();
                arrivalTarget = targetController.InternalTargetOrbit;
            }
        }

        /// <summary>
        /// Whether to plan a capture burn at the destination.
        /// </summary>
        [KRPCProperty]
        public bool IncludeCaptureBurn {
            get { return includeCaptureBurn.Get (Instance); }
            set {
                var instance = Instance;
                if (includeCaptureBurn.Get (instance) != value)
                    StopSearch (instance);
                includeCaptureBurn.Set (instance, value);
            }
        }

        /// <summary>
        /// The altitude of the periapsis at the destination, in meters.
        /// </summary>
        /// <remarks>
        /// <c>MakeNodes</c> sets a default for a new target body, 10 km above its atmosphere or
        /// 100 km, unless it was set while the body was the target.
        /// </remarks>
        [KRPCProperty]
        public double PeriapsisHeight {
            // MechJeb holds it in kilometers
            get { return periapsisHeight.Get (Instance) * 1000d; }
            set {
                var instance = Instance;
                if (periapsisHeight.Get (instance) != value / 1000d)
                    StopSearch (instance);
                periapsisHeight.Set (instance, value / 1000d);
                // MechJeb's window keeps the value while the target body stays the same
                var targetController = MechJeb.TargetController;
                targetController.Update ();
                var body = targetController.Target as global::CelestialBody;
                if (body != null)
                    lastTargetCelestial.Set (instance, body);
            }
        }

        /// <summary>
        /// Whether MechJeb is searching for the departure.
        /// </summary>
        [KRPCProperty]
        public bool Searching {
            get {
                var calculator = worker.Get (Instance);
                return calculator != null && !finished (calculator);
            }
        }

        /// <summary>
        /// The progress of the search for the departure, as a percentage.
        /// </summary>
        [KRPCProperty]
        public int SearchProgress {
            get {
                var calculator = worker.Get (Instance);
                return calculator == null ? 0 : progress (calculator);
            }
        }

        // Stop the search, so the next MakeNodes starts one with the current settings. MechJeb's
        // window does this on any change to its settings.
        static void StopSearch (object instance)
        {
            var calculator = worker.Get (instance);
            if (calculator == null)
                return;
            stop.Set (calculator, true);
            worker.Set (instance, null);
        }

        internal override void Prepare (object instance, global::Orbit orbit, double time)
        {
            // MechJeb's window records the target body, which sizes the capture orbit, and sets
            // a default periapsis when the body changes
            var body = MechJeb.TargetController.Target as global::CelestialBody;
            if (body == null)
                throw new OperationException ("The target must be a celestial body");
            if (lastTargetCelestial.Get (instance) != body) {
                periapsisHeight.Set (instance, body.atmosphere ? body.atmosphereDepth / 1000d + 10d : 100d);
                lastTargetCelestial.Set (instance, body);
            }
            selectionMode.Set (instance, limitedTime);
            var target = MechJeb.TargetController.InternalTargetOrbit;
            var calculator = worker.Get (instance);
            // A search from another orbit, or to another target, is restarted, as in MechJeb's window
            if (calculator != null && (originOrbit (calculator) != orbit || destinationOrbit (calculator) != target))
                StopSearch (instance);
            if (target != arrivalTarget) {
                computeTimes (instance, orbit, target, time);
                arrivalTarget = target;
            }
        }
    }
}
