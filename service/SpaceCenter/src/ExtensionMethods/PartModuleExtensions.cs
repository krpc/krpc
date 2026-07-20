using System.Linq;

namespace KRPC.SpaceCenter.ExtensionMethods
{
    static class PartModuleExtensions
    {
        /// <summary>
        /// Try invoking an event for a part module, identified by its id -- the name of the
        /// method implementing it, which the game does not translate, unlike the display name.
        /// Returns true if an event is found and invoked.
        /// </summary>
        public static bool InvokeEvent (this PartModule module, string eventId)
        {
            var e = module.Events
                .Where (x => x != null && (HighLogic.LoadedSceneIsEditor ? x.guiActiveEditor : x.guiActive) && x.active)
                .FirstOrDefault (x => x.name == eventId);
            if (e != null) {
                e.Invoke ();
                return true;
            }
            return false;
        }
    }
}
