using UnityEngine;

namespace KRPCInfernalRobotics
{
    [KSPAddon (KSPAddon.Startup.Flight, false)]
    public class Addon : MonoBehaviour
    {
        public void Start ()
        {
            IRWrapper.InitWrapper ();
        }
    }
}
