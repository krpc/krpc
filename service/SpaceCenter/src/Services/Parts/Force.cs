using KRPC.Service;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.Utils;
using UnityEngine;
using ObjectDestroyedException = KRPC.Service.KRPC.ObjectDestroyedException;
using Tuple3 = System.Tuple<double, double, double>;

namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// Obtained by calling <see cref="Part.AddForce"/>.
    /// </summary>
    [KRPCClass (Service = "SpaceCenter", GameScene = GameScene.Flight)]
    public sealed class Force : IGameObjectState
    {
        Vector3 force;
        Vector3 position;
        ReferenceFrame frame;
        // Whether the force has been taken off the part. The object goes on standing for
        // an instruction the game is no longer given, so it says it is gone rather than
        // reporting a force that is not being applied.
        bool removed;

        internal Force (Part part, Tuple3 forceVector, Tuple3 forcePosition, ReferenceFrame referenceFrame)
        {
            Part = part;
            force = forceVector.ToVector ();
            position = forcePosition.ToVector ();
            frame = referenceFrame;
        }

        /// <summary>
        /// The part that this force is applied to.
        /// </summary>
        [KRPCProperty]
        public Part Part { get; private set; }

        /// <summary>
        /// The state of the force. A force is applied to a part and takes that part's
        /// state: a destroyed part can never be pushed again, and an unloaded part has no
        /// rigidbody to push until it is loaded. A force the client has removed is
        /// destroyed whatever its part's state.
        /// </summary>
        public GameObjectState GameObjectState {
            get { return removed ? GameObjectState.Destroyed : Part.GameObjectState; }
        }

        /// <summary>
        /// Raise if the force is no longer applied to its part.
        /// </summary>
        void CheckExists ()
        {
            if (removed)
                throw new ObjectDestroyedException (
                    "The force no longer exists, as it has been removed.");
        }

        /// <summary>
        /// The force vector, in Newtons.
        /// </summary>
        /// <returns>A vector pointing in the direction that the force acts,
        /// with its magnitude equal to the strength of the force in Newtons.</returns>
        [KRPCProperty]
        public Tuple3 ForceVector {
            get { CheckExists (); return force.ToTuple (); }
            set { CheckExists (); force = value.ToVector (); }
        }

        /// <summary>
        /// The position at which the force acts, in reference frame <see cref="ReferenceFrame"/>.
        /// </summary>
        /// <returns>The position as a vector.</returns>
        [KRPCProperty]
        public Tuple3 Position {
            get { CheckExists (); return position.ToTuple (); }
            set { CheckExists (); position = value.ToVector (); }
        }

        /// <summary>
        /// The reference frame of the force vector and position.
        /// </summary>
        [KRPCProperty]
        public ReferenceFrame ReferenceFrame {
            get { CheckExists (); return frame; }
            set { CheckExists (); frame = value; }
        }

        /// <summary>
        /// Remove the force.
        /// </summary>
        /// <remarks>
        /// Any further use of this object throws an exception.
        /// </remarks>
        [KRPCMethod]
        public void Remove ()
        {
            CheckExists ();
            removed = true;
            PartForcesAddon.Remove (this);
        }

        /// <summary>
        /// Apply the force for one physics step, if there is a part to apply it to and a
        /// reference frame to measure it in. The addon has already dropped the forces whose
        /// part is gone; what is left to skip is a part the game has unloaded, and a frame
        /// defined against something that is gone, which the client can point elsewhere.
        /// </summary>
        internal void Update ()
        {
            if (Part.GameObjectState != GameObjectState.Live ||
                frame.GameObjectState != GameObjectState.Live)
                return;
            var worldForce = frame.DirectionToWorldSpace (force);
            var worldPosition = frame.PositionToWorldSpace (position);
            Part.InternalPart.AddForceAtPosition (worldForce / 1000f, worldPosition);
        }
    }
}
