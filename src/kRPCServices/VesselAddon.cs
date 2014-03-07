using UnityEngine;

namespace KRPCServices
{
    [KSPAddon (KSPAddon.Startup.Flight, false)]
    public class VesselAddon : MonoBehaviour
    {
        VesselData vesselData;

        public void Awake ()
        {
        }

        public void FixedUpdate ()
        {
            if (FlightGlobals.ActiveVessel != null) {
                if (vesselData == null) {
                    vesselData = new VesselData (FlightGlobals.ActiveVessel);
                    Services.Flight.VesselData = vesselData;
                }
                vesselData.Update ();
            }
        }

        public void OnDestroy ()
        {
            vesselData = null;
        }
    }
}
