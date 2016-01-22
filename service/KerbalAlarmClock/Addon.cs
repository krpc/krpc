using UnityEngine;

namespace KRPC.KerbalAlarmClock
{
    [KSPAddon (KSPAddon.Startup.Flight, false)]
    public class Addon : MonoBehaviour
    {
        public void Start ()
        {
            KACWrapper.InitKACWrapper ();
        }
    }
}
