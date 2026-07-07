using System;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.Utils;
using KSP.Localization;

namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// A sensor, such as a thermometer. Obtained by calling <see cref="Part.Sensor"/>.
    /// </summary>
    [KRPCClass (Service = "SpaceCenter")]
    public class Sensor : Equatable<Sensor>
    {
        readonly ModuleEnviroSensor sensor;

        internal static bool Is (Part part)
        {
            return part.InternalPart.HasModule<ModuleEnviroSensor> ();
        }

        internal Sensor (Part part)
        {
            Part = part;
            sensor = part.InternalPart.Module<ModuleEnviroSensor> ();
            if (sensor == null)
                throw new ArgumentException ("Part is not a sensor");
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (Sensor other)
        {
            return !ReferenceEquals (other, null) && Part == other.Part && sensor == other.sensor;
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            return Part.GetHashCode () ^ sensor.GetHashCode ();
        }

        /// <summary>
        /// The part object for this sensor.
        /// </summary>
        [KRPCProperty]
        public Part Part { get; private set; }

        /// <summary>
        /// Whether the sensor is active.
        /// </summary>
        [KRPCProperty]
        public bool Active {
            get { return sensor.sensorActive; }
            set { sensor.sensorActive = value; }
        }

        /// <summary>
        /// The current value of the sensor.
        /// </summary>
        /// <remarks>
        /// This is the same readout string shown in the part's right-click menu
        /// in-game. For programmatic access to the underlying quantities, consider
        /// <see cref="Part.Temperature"/> (temperature sensors),
        /// <see cref="Flight.GForce"/> (accelerometers) or
        /// <see cref="Flight.StaticPressure"/> (barometers).
        /// </remarks>
        [KRPCProperty]
        public string Value {
            get {
                // ModuleEnviroSensor only writes its readout into the readoutInfo
                // field while the part's right-click menu (PAW) is open, so reading
                // that field returns a stale "Off" for remote clients (issue #883).
                // Reproduce the game's ModuleEnviroSensor.FixedUpdate computation
                // here so the value is available headless, matching the in-game
                // readout exactly.
                var off = Localizer.Format ("#autoLOC_237153");
                var internalPart = Part.InternalPart;
                if (!sensor.sensorActive || !internalPart.started)
                    return off;
                // No power: only reachable for sensors with a declared input
                // resource (stock sensors have none); the game reports "Off".
                if (sensor.resHandler.currentResourceLowerThanLayoff)
                    return off;
                var internalVessel = internalPart.vessel;
                switch (sensor.sensorType) {
                case ModuleEnviroSensor.SensorType.TEMP:
                    return internalPart.temperature.ToString ("0.##") + " " +
                        Localizer.Format ("#autoLOC_7001406");
                case ModuleEnviroSensor.SensorType.GRAV:
                    var orbit = internalVessel.orbit;
                    if (orbit.altitude <= orbit.referenceBody.Radius * 3d) {
                        var gravity = FlightGlobals.getGeeForceAtPosition (
                            internalPart.transform.position).magnitude;
                        return Localizer.Format (
                            "#autoLOC_237120", new [] { gravity.ToString ("00.00") });
                    }
                    return Localizer.Format ("#autoLOC_6004058");
                case ModuleEnviroSensor.SensorType.ACC:
                    return internalVessel.geeForce.ToString ("00.0##") +
                        Localizer.Format ("#autoLOC_7001413");
                case ModuleEnviroSensor.SensorType.PRES:
                    var pressure = internalVessel.staticPressurekPa;
                    if (pressure > 0.0001d)
                        return pressure.ToString ("0.00##") + " " +
                            Localizer.Format ("#autoLOC_7001410");
                    if (pressure > 0d)
                        return Localizer.Format ("#autoLOC_6004059");
                    return Localizer.Format ("#autoLOC_6004060");
                default:
                    return off;
                }
            }
        }
    }
}
