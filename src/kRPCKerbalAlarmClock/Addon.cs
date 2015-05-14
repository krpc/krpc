using UnityEngine;

namespace KRPCKerbalAlarmClock
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
