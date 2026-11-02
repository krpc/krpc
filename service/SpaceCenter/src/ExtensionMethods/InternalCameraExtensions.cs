using System;
using KRPC.Utils;

namespace KRPC.SpaceCenter.ExtensionMethods
{
    /// <summary>
    /// Getters and setters for private members of the internal camera.
    /// </summary>
    public static class InternalCameraExtensions
    {
        /// <summary>
        /// Sets the pitch of the internal camera.
        /// </summary>
        public static readonly Action<InternalCamera, float> SetPitch =
            Accessor.Setter<InternalCamera, float> ("currentPitch");

        /// <summary>
        /// Gets the pitch of the internal camera.
        /// </summary>
        public static readonly Func<InternalCamera, float> GetPitch =
            Accessor.Getter<InternalCamera, float> ("currentPitch");

        /// <summary>
        /// Sets the rotation of the internal camera.
        /// </summary>
        public static readonly Action<InternalCamera, float> SetRot =
            Accessor.Setter<InternalCamera, float> ("currentRot");

        /// <summary>
        /// Sets the current zoom of the internal camera.
        /// </summary>
        public static readonly Action<InternalCamera, float> SetZoom =
            Accessor.Setter<InternalCamera, float> ("currentZoom");

        /// <summary>
        /// Gets the rotation of the internal camera.
        /// </summary>
        public static readonly Func<InternalCamera, float> GetRot =
            Accessor.Getter<InternalCamera, float> ("currentRot");

        /// <summary>
        /// Gets the field of view of the internal camera.
        /// </summary>
        public static readonly Func<InternalCamera, float> GetFoV =
            Accessor.Getter<InternalCamera, float> ("currentFoV");

        /// <summary>
        /// Gets the default field of view of the internal camera.
        /// </summary>
        public static readonly Func<InternalCamera, float> GetDefaultFoV =
            Accessor.Getter<InternalCamera, float> ("initialZoom");
    }
}
