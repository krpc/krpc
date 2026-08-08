using KRPC.Utils;
using UnityEngine;

namespace KRPC
{
    /// <summary>
    /// Shuts kRPC down for as long as no game is loaded: stops the servers, so that a
    /// client is disconnected rather than left waiting on a server nothing is driving,
    /// and releases the state kRPC holds on behalf of clients.
    /// </summary>
    /// <remarks>
    /// Every other addon is declared for the scenes a game is loaded in, so none of them
    /// exists in the main menu and nothing there drives the servers or sweeps up after a
    /// client. The servers were left listening, and the state of the game just left was
    /// held until a game was loaded again: anything the game keeps across a scene change,
    /// such as a user interface element on the stock canvas, stayed on the screen for the
    /// whole of the main menu.
    ///
    /// Declared for the main menu rather than for every scene. The game starts the addons
    /// of a scene as that scene finishes loading, and it loads a scene of its own between
    /// two game scenes, which is not a scene a game is loaded in either: an addon declared
    /// for every scene would be started there and would take a switch from one game scene
    /// to another for the game being unloaded. The main menu is the only way out of a
    /// game other than quitting, which stops the servers on its way out.
    /// </remarks>
    [KSPAddon (KSPAddon.Startup.MainMenu, false)]
    public sealed class NoGameAddon : MonoBehaviour
    {
        /// <summary>
        /// Whether the state kRPC holds is still waiting to be released.
        /// </summary>
        bool releasePending;

        /// <summary>
        /// Stop the servers, as soon as the main menu is being entered.
        /// </summary>
        public void Awake ()
        {
            if (!ServicesChecker.OK)
                return;
            Utils.Logger.WriteLine ("No game is loaded");
            Service.CallContext.GameScene = Service.GameScene.None;
            var core = Core.Instance;
            if (core.AnyRunning) {
                Utils.Logger.WriteLine ("Stopping the server, as no game is loaded");
                core.StopAll ();
            }
            releasePending = true;
        }

        /// <summary>
        /// Release the state kRPC holds on behalf of clients.
        /// </summary>
        /// <remarks>
        /// Not done in Awake, which runs while the scene is still loading: a destroy asked
        /// for then does not take effect during that load, and the object it was asked for
        /// lives on through the whole of the scene being entered. Asking once the scene is
        /// running takes effect on the next frame.
        /// </remarks>
        public void Update ()
        {
            if (!releasePending)
                return;
            releasePending = false;
            Utils.Logger.WriteLine ("Releasing the state held for clients");
            ClientOwnedState.ReleaseAll ();
        }
    }
}
