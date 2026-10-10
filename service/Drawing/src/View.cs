using UnityEngine;

namespace KRPC.Drawing
{
    /// <summary>
    /// The view that drawables are drawn in, updated once per frame by the addon.
    /// </summary>
    /// <remarks>
    /// The map view and the tracking station render scaled space, on its own layer and with its
    /// own camera. A drawable is placed there at its scaled position, and its size grows with
    /// its distance from the camera so that it keeps the same size on screen.
    /// </remarks>
    static class View
    {
        // The layer the flight and editor cameras render
        const int localLayer = 0;
        // The layer the map view camera renders
        const int scaledLayer = 10;
        // The distance from the camera, in meters, at which a drawable in the flight scene has
        // the on-screen size it is drawn with in scaled space
        const float referenceDistance = 50f;

        static Camera camera;

        /// <summary>
        /// Whether drawables are drawn in scaled space.
        /// </summary>
        public static bool Scaled { get; private set; }

        /// <summary>
        /// The layer drawables are drawn on.
        /// </summary>
        public static int Layer {
            get { return Scaled ? scaledLayer : localLayer; }
        }

        /// <summary>
        /// Update the view for the current frame.
        /// </summary>
        public static void Update ()
        {
            if (HighLogic.LoadedScene == GameScenes.TRACKSTATION) {
                Scaled = true;
                camera = PlanetariumCamera.Camera;
            } else if (HighLogic.LoadedSceneIsEditor) {
                Scaled = false;
                camera = null;
            } else {
                Scaled = MapView.MapIsEnabled;
                camera = Scaled ? PlanetariumCamera.Camera : null;
            }
        }

        /// <summary>
        /// Convert a position in world space to the space of the view.
        /// </summary>
        public static Vector3 ToView (Vector3d position)
        {
            return Scaled ? ScaledSpace.LocalToScaledSpace (position) : position;
        }

        /// <summary>
        /// The factor that sizes a drawable at the given position in the space of the view.
        /// </summary>
        public static float SizeAt (Vector3 position)
        {
            if (!Scaled || camera == null)
                return 1f;
            return Vector3.Distance (camera.transform.position, position) / referenceDistance;
        }
    }
}
