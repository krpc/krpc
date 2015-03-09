using System;
using KRPC.Service;
using UnityEngine;

namespace KRPC
{
    [KSPAddon (KSPAddon.Startup.Instantly, true)]
    class ServicesChecker : MonoBehaviour
    {
        public static bool OK { get; private set; }

        public void Start ()
        {
            OK = true;
            try {
                Service.Scanner.Scanner.GetServices ();
            } catch (ServiceException e) {
                OK = false;
                var path = (e.Assembly == null ? "unknown" : e.Assembly.Location);
                PopupDialog.SpawnPopupDialog ("kRPC service error - plugin disabled", e.Message + "\n\n" + path, "OK", true, HighLogic.Skin);
            }
        }
    }
}
