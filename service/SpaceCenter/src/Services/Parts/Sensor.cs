using System;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.Utils;

namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// Obtained by calling <see cref="Part.Sensor"/>.
    /// </summary>
    [KRPCClass (Service = "SpaceCenter")]
    public sealed class Sensor : Equatable<Sensor>
    {
        readonly Part part;
        readonly ModuleEnviroSensor sensor;

        internal static bool Is (Part part)
        {
            return part.InternalPart.HasModule<ModuleEnviroSensor> ();
        }

        internal Sensor (Part part)
        {
            this.part = part;
            sensor = part.InternalPart.Module<ModuleEnviroSensor> ();
            if (sensor == null)
                throw new ArgumentException ("Part is not a sensor");
        }

        /// <summary>
        /// Check if sensors are equal.
        /// </summary>
        public override bool Equals (Sensor obj)
        {
            return part == obj.part && sensor == obj.sensor;
        }

        /// <summary>
        /// Hash the sensor.
        /// </summary>
        public override int GetHashCode ()
        {
            return part.GetHashCode () ^ sensor.GetHashCode ();
        }

        /// <summary>
        /// The part object for this sensor.
        /// </summary>
        [KRPCProperty]
        public Part Part {
            get { return part; }
        }

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

        /// <summary>
        /// The current power usage of the sensor, in units of charge per second.
        /// </summary>
        [KRPCProperty]
        public float PowerUsage {
            get { return sensor.powerConsumption; }
        }
    }
}
