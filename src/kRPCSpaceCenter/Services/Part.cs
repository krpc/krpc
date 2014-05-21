using System;
using System.Collections.Generic;
using System.Linq;
using KRPC.Service.Attributes;
using KRPC.Utils;
using KRPCSpaceCenter.ExtensionMethods;

namespace KRPCSpaceCenter.Services
{
    [KRPCEnum (Service = "SpaceCenter")]
    public enum PartState
    {
        Idle,
        Active,
        Dead
    }

    [KRPCClass (Service = "SpaceCenter")]
    public sealed class Part : Equatable<Part>
    {
        global::Part part;

        internal Part (global::Part part)
        {
            this.part = part;
        }

        public override bool Equals (Part other)
        {
            return part == other.part;
        }

        public override int GetHashCode ()
        {
            return part.GetHashCode ();
        }

        [KRPCProperty]
        public string Name {
            get { return part.ClassName; }
        }

        [KRPCProperty]
        public Vessel Vessel {
            get { return new Vessel (part.vessel); }
        }

        [KRPCProperty]
        public Part Parent {
            get { return new Part (part.parent); }
        }

        [KRPCProperty]
        public IList<Part> Children {
            get { return part.children.Select (p => new Part (p)).ToList (); }
        }

        [KRPCProperty]
        public PartState State {
            get { return part.State.ToPartState (); }
        }

        [KRPCProperty]
        public int Stage {
            get { return part.DecoupledAt (); }
        }

        [KRPCProperty]
        public double Mass {
            get { return part.mass; }
        }

        [KRPCProperty]
        public double DryMass {
            get { return Mass - part.GetResourceMass (); }
        }

        [KRPCProperty]
        public double Temperature {
            get { return part.temperature; }
        }

        [KRPCProperty]
        public double MaxTemperature {
            get { return part.maxTemp; }
        }

        [KRPCMethod]
        public void Activate ()
        {
            part.force_activate ();
        }

        [KRPCMethod]
        public void Deactivate ()
        {
            part.deactivate ();
        }

        [KRPCMethod]
        public void Decouple ()
        {
            part.disconnect (true);
        }

        [KRPCProperty]
        public IList<PartModule> Modules {
            get {
                //TODO: remove cast
                return ((IEnumerable<global::PartModule>)part.Modules).Select (pm => new PartModule (pm)).ToList ();
            }
        }

        [KRPCProperty]
        public IList<string> Events {
            get { return part.Events.Select (e => e.name).ToList (); }
        }

        [KRPCMethod]
        public void TriggerEvent (string eventName)
        {
            part.SendEvent (eventName);
        }

        [KRPCProperty]
        public IDictionary<string,string> Fields {
            get {
                var result = new Dictionary<string,string> ();
                foreach (var field in part.Fields) {
                    var kspField = field as KSPField;
                    if (kspField != null)
                        result [kspField.guiName] = kspField.guiUnits;
                }
                return result;
            }
        }
    }
}
