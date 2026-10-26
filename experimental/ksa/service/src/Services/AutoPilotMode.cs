using System;
using KRPC.Service.Attributes;

namespace KRPC.KSA.Services
{
    /// <summary>
    /// The direction the auto-pilot points the vehicle in.
    /// See <see cref="AutoPilot.Mode" />.
    /// </summary>
    [Serializable]
    [KRPCEnum (Service = "KSA")]
    public enum AutoPilotMode
    {
        /// <summary>
        /// The pitch, heading and roll set by <see cref="AutoPilot.TargetPitch" />,
        /// <see cref="AutoPilot.TargetHeading" /> and <see cref="AutoPilot.TargetRoll" />.
        /// </summary>
        Attitude,
        /// <summary>
        /// Holds the vehicle's rotation at zero, relative to the stars.
        /// </summary>
        Hold,
        /// <summary>
        /// The direction of the vehicle's orbital velocity.
        /// </summary>
        Prograde,
        /// <summary>
        /// The opposite of <see cref="Prograde" />.
        /// </summary>
        Retrograde,
        /// <summary>
        /// Normal to the orbital plane, along the orbit's angular momentum.
        /// </summary>
        Normal,
        /// <summary>
        /// The opposite of <see cref="Normal" />.
        /// </summary>
        AntiNormal,
        /// <summary>
        /// Away from the center of the body being orbited.
        /// </summary>
        RadialOut,
        /// <summary>
        /// Toward the center of the body being orbited.
        /// </summary>
        RadialIn,
        /// <summary>
        /// The direction of the vehicle's velocity relative to the surface.
        /// </summary>
        SurfacePrograde,
        /// <summary>
        /// The opposite of <see cref="SurfacePrograde" />.
        /// </summary>
        SurfaceRetrograde,
        /// <summary>
        /// A target set in the game, which the API does not cover.
        /// </summary>
        Other
    }
}
