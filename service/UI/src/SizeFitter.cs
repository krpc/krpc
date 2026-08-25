using System;
using System.Runtime.CompilerServices;
using KRPC.Service.Attributes;
using KRPC.UI.ExtensionMethods;
using KRPC.Utils;
using ObjectDestroyedException = KRPC.Service.KRPC.ObjectDestroyedException;

namespace KRPC.UI
{
    /// <summary>
    /// Sizes a panel to fit what it contains, rather than the other way round.
    /// See <see cref="Panel.SizeFitter" />.
    /// </summary>
    [KRPCClass (Service = "UI")]
    public class SizeFitter : Equatable<SizeFitter>, IGameObjectState
    {
        // The game's size fitter. It belongs to the interface element it was taken from and
        // lives exactly as long as that element, and the game gives it no other name
        readonly UnityEngine.UI.ContentSizeFitter fitter;

        internal SizeFitter (UnityEngine.UI.ContentSizeFitter innerFitter)
        {
            fitter = innerFitter;
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (SizeFitter other)
        {
            return !ReferenceEquals (other, null) &&
            ReferenceEquals (fitter, other.fitter);
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            // The size fitter's identity hash, so that nothing the game does to it changes it
            // while a client holds this object
            return RuntimeHelpers.GetHashCode (fitter);
        }

        /// <summary>
        /// The state of the size fitter. It belongs to the interface element it was taken
        /// from, and the game destroys it with that element. The size fitter is never built
        /// again.
        /// </summary>
        public GameObjectState GameObjectState {
            get {
                return fitter == null
                    ? GameObjectState.Destroyed : GameObjectState.Live;
            }
        }

        // The game's size fitter, checked to still exist.
        UnityEngine.UI.ContentSizeFitter Internal {
            get {
                if (GameObjectState == GameObjectState.Destroyed)
                    throw new ObjectDestroyedException (
                        "The size fitter no longer exists, as the user interface object " +
                        "it belongs to has been removed.");
                return fitter;
            }
        }

        /// <summary>
        /// How the width of the panel is chosen.
        /// </summary>
        [KRPCProperty]
        public ContentSizeFit HorizontalFit {
            get { return Internal.horizontalFit.ToContentSizeFit (); }
            set { Internal.horizontalFit = value.FromContentSizeFit (); }
        }

        /// <summary>
        /// How the height of the panel is chosen.
        /// </summary>
        [KRPCProperty]
        public ContentSizeFit VerticalFit {
            get { return Internal.verticalFit.ToContentSizeFit (); }
            set { Internal.verticalFit = value.FromContentSizeFit (); }
        }
    }
}
