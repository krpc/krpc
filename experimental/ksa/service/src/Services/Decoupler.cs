using System;
using System.Collections.Generic;
using KRPC.KSA.Utils;
using KRPC.Service.Attributes;
using KRPC.Utils;

namespace KRPC.KSA.Services
{
    /// <summary>
    /// A decoupler. Obtained from <see cref="Part.Decoupler"/>.
    /// </summary>
    [KRPCClass (Service = "KSA")]
    public class Decoupler : Equatable<Decoupler>
    {
        readonly global::KSA.Decoupler decoupler;

        internal Decoupler (Part part, global::KSA.Decoupler decoupler)
        {
            Part = part;
            this.decoupler = decoupler;
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (Decoupler other)
        {
            return !ReferenceEquals (other, null) && ReferenceEquals (decoupler, other.decoupler);
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            return decoupler.GetHashCode ();
        }

        /// <summary>
        /// The part the decoupler is in.
        /// </summary>
        [KRPCProperty]
        public Part Part { get; }

        /// <summary>
        /// Whether the decoupler can fire.
        /// </summary>
        [KRPCProperty]
        public bool Enabled {
            get {
                _ = Part.InternalPart;
                return decoupler.IsEnabled;
            }
        }

        /// <summary>
        /// Whether the decoupler has fired, or is attached to nothing.
        /// </summary>
        [KRPCProperty]
        public bool Decoupled {
            get { return !Attached (Part.InternalPart); }
        }

        // The game's decouple splits the vehicle at the connector, and leaves the module's
        // active flag unset
        bool Attached (global::KSA.Part part)
        {
            return decoupler.Connector.Connection?.OtherPart (part.FullPart) != null;
        }

        /// <summary>
        /// Fire the decoupler, separating the vehicle into two. Returns once it has fired.
        /// Throws an exception if the decoupler is disabled or <see cref="Decoupled"/>.
        /// </summary>
        /// <returns>The vehicles that separated from this one.</returns>
        [KRPCMethod]
        public IList<Vehicle> Decouple ()
        {
            var p = Part.InternalPart;
            if (!decoupler.IsEnabled || !Attached (p))
                throw new InvalidOperationException ("The decoupler cannot fire");
            var before = new HashSet<global::KSA.Vehicle> (Vehicles.All);
            decoupler.SetIsActive (p.Tree.OwningVehicle, true);
            return Vehicles.SeparatedWhen (() => !Attached (p), before);
        }
    }
}
