using System;
using System.Collections.Generic;
using System.Linq;
using KRPC.Service.Attributes;
using KRPC.Utils;

namespace KRPCSpaceCenter.Services
{
    [KRPCClass (Service = "SpaceCenter")]
    public sealed class PartModule : Equatable<PartModule>
    {
        global::PartModule module;

        internal PartModule (global::PartModule module)
        {
            this.module = module;
        }

        public override bool Equals (PartModule other)
        {
            return module == other.module;
        }

        public override int GetHashCode ()
        {
            return module.GetHashCode ();
        }

        [KRPCProperty]
        public Part Part {
            get { return new Part (module.part); }
        }

        [KRPCProperty]
        public IList<string> Events {
            get { return module.Events.Select (e => e.name).ToList (); }
        }

        [KRPCMethod]
        public void TriggerEvent (string eventName)
        {
            module.part.SendEvent (eventName);
        }
    }
}

