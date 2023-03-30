using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using UnityEngine;
using Tuple3 = System.Tuple<double, double, double>;

namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// Obtained by calling <see cref="Part.AddForce"/>.
    /// </summary>
    [KRPCClass (Service = "SpaceCenter")]
    public sealed class Force
    {
        Vector3 force;
        Vector3 position;

        internal Force (Part part, Tuple3 forceVector, Tuple3 forcePosition, ReferenceFrame referenceFrame)
        {
            Part = part;
            force = forceVector.ToVector ();
            position = forcePosition.ToVector ();
            ReferenceFrame = referenceFrame;
        }

        /// <summary>
        /// The part that this force is applied to.
        /// </summary>
        [KRPCProperty]
        public Part Part { get; private set; }

        /// <summary>
        /// The force vector, in Newtons.
        /// </summary>
        /// <returns>A vector pointing in the direction that the force acts,
        /// with its magnitude equal to the strength of the force in Newtons.</returns>
        [KRPCProperty]
        public Tuple3 ForceVector {
            get { return force.ToTuple (); }
            set { force = value.ToVector (); }
        }

        /// <summary>
        /// The position at which the force acts, in reference frame <see cref="ReferenceFrame"/>.
        /// </summary>
        /// <returns>The position as a vector.</returns>
        [KRPCProperty]
        public Tuple3 Position {
            get { return position.ToTuple (); }
            set { position = value.ToVector (); }
        }

        /// <summary>
        /// The reference frame of the force vector and position.
        /// </summary>
        [KRPCProperty]
        public ReferenceFrame ReferenceFrame { get; set; }

        /// <summary>
        /// Remove the force.
        /// </summary>
        [KRPCMethod]
        public void Remove ()
        {
            PartForcesAddon.Remove (this);
            // TODO: delete the object
        }

        internal void Update ()
        {
            var worldForce = ReferenceFrame.DirectionToWorldSpace (force);
            var worldPosition = ReferenceFrame.PositionToWorldSpace (position);
            Part.InternalPart.AddForceAtPosition (worldForce / 1000f, worldPosition);
        }
    }
}
