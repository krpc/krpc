using System;
using System.Collections.Generic;
using System.Linq;
using KRPC.Service;
using KRPC.Service.Attributes;

namespace Extending
{
    using KRPC.SpaceCenter;
    using KRPC.SpaceCenter.Services;
    using KRPC.SpaceCenter.Services.Parts;
    using KRPC.Utils;

    public class TestFlightFailure : PartModule {
        public bool failed;
    }

    // [docs]
    [KRPCClass (Service = "TestFlight")]
    public class FailureModule : Equatable<FailureModule>, IGameObjectState {
        // The part the module is on, and which of the part's modules it is
        readonly Part part;
        ModuleRef reference;

        internal FailureModule (Part part, ModuleRef reference)
        {
            this.part = part;
            this.reference = reference;
        }

        // The module, which every member reaches the game through
        TestFlightFailure InternalModule {
            get { return (TestFlightFailure)reference.Get (part.InternalPart); }
        }

        /// <summary>The state of the module, which is its part's until the part is there to look in.</summary>
        public GameObjectState GameObjectState {
            get { return reference.StateOn (part); }
        }

        /// <summary>Returns true if the objects are equal.</summary>
        public override bool Equals (FailureModule other)
        {
            return !ReferenceEquals (other, null) && part == other.part && reference == other.reference;
        }

        /// <summary>Hash code for the object.</summary>
        public override int GetHashCode ()
        {
            return Hash.Of (part).And (reference);
        }

        /// <summary>Whether the part has failed.</summary>
        [KRPCProperty]
        public bool Failed { get { return InternalModule.failed; } }
    }

    public static class TestFlightExtensions {
        /// <summary>The failure module of the part, or <c>null</c> if it does not have one.</summary>
        [KRPCProperty (Nullable = true)]
        public static FailureModule GetFailureModule (this Part part)
        {
            var reference = ModuleRef.ForType<TestFlightFailure> (part.InternalPart);
            return reference == ModuleRef.None ? null : new FailureModule (part, reference);
        }
    }
    // [/docs]
}
