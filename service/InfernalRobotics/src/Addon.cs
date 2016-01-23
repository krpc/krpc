using UnityEngine;

namespace KRPC.InfernalRobotics
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
