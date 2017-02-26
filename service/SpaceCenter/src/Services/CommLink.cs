using System;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.Utils;

namespace KRPC.SpaceCenter.Services
{
    /// <summary>
    /// Represents a communication node in the network. For example, a vessel or the KSC.
    /// </summary>
    [KRPCClass (Service = "SpaceCenter")]
    public class CommLink : Equatable<CommLink>
    {
        /// <summary>
        /// Construct from a KSP CommLink object.
        /// </summary>
        public CommLink (CommNet.CommLink link)
        {
            if (ReferenceEquals (link, null))
                throw new ArgumentNullException (nameof (link));
            InternalLink = link;
        }

        /// <summary>
        /// KSP CommLink object.
        /// </summary>
        public CommNet.CommLink InternalLink { get; private set;}

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (CommLink other)
        {
            return !ReferenceEquals (other, null) && InternalLink == other.InternalLink;
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            return InternalLink.GetHashCode ();
        }

        /// <summary>
        /// The type of link.
        /// </summary>
        [KRPCProperty]
        public CommLinkType Type {
            get { return InternalLink.hopType.ToCommLinkType (); }
        }

        /// <summary>
        /// Signal strength of the link.
        /// </summary>
        [KRPCProperty]
        public double SignalStrength {
            get { return InternalLink.signalStrength; }
        }

        /// <summary>
        /// Start point of the link.
        /// </summary>
        [KRPCProperty]
        public CommNode Start {
            get { return new CommNode (InternalLink.start); }
        }

        /// <summary>
        /// Start point of the link.
        /// </summary>
        [KRPCProperty]
        public CommNode End {
            get { return new CommNode (InternalLink.end); }
        }
    }
}
