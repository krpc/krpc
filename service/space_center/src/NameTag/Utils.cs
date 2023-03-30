#pragma warning disable 1591

using UnityEngine;

namespace KRPC.SpaceCenter.NameTag
{
    public static class Utils
    {
        public static Camera GetCurrentCamera()
        {
            // man, KSP could really just use a simple "get whatever the current camera is" method:
            return HighLogic.LoadedSceneIsEditor ?
                       EditorLogic.fetch.editorCamera :
                       (MapView.MapIsEnabled ?
                           PlanetariumCamera.Camera : FlightCamera.fetch.mainCamera);
        }
    }
}
