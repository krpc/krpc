using System.Linq;

namespace KRPC.SpaceCenter.ExtensionMethods
{
    static class PartModuleExtensions
    {
        /// <summary>
        /// Try invoking a named event for a part module. Returns true if an event is found
        /// and invoked.
        /// </summary>
        public static bool InvokeEvent (this PartModule module, string eventName)
        {
            var e = module.Events
                .Where (x => x != null && (HighLogic.LoadedSceneIsEditor ? x.guiActiveEditor : x.guiActive) && x.active)
                .FirstOrDefault (x => x.guiName == eventName);
            if (e != null) {
                e.Invoke ();
                return true;
            }
            return false;
        }
    }
}
