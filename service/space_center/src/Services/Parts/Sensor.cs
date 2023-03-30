using System;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.Utils;

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
        [KRPCProperty]
        public string Value {
            get { return sensor.readoutInfo; }
        }
    }
}
