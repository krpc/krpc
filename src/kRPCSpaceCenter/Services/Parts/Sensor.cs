using System;
using KRPC.Service.Attributes;
using KRPC.Utils;
using KRPCSpaceCenter.ExtensionMethods;

namespace KRPCSpaceCenter.Services.Parts
{
    [KRPCClass (Service = "SpaceCenter")]
    public sealed class Sensor : Equatable<Sensor>
    {
        readonly Part part;
        readonly ModuleEnviroSensor sensor;

        internal Sensor (Part part)
        {
            this.part = part;
            sensor = part.InternalPart.Module<ModuleEnviroSensor> ();
            if (sensor == null)
                throw new ArgumentException ("Part does not have a ModuleEnviroSensor PartModule");
        }

        public override bool Equals (Sensor obj)
        {
            return part == obj.part;
        }

        public override int GetHashCode ()
        {
            return part.GetHashCode ();
        }

        [KRPCProperty]
        public Part Part {
            get { return part; }
        }

        [KRPCProperty]
        public bool Active {
            get { return sensor.sensorActive; }
            set { sensor.sensorActive = value; }
        }

        [KRPCProperty]
        public string Value {
            get { return sensor.readoutInfo; }
        }

        [KRPCProperty]
        public float PowerUsage {
            get { return sensor.powerConsumption; }
        }
    }
}
