using System;
using System.Linq;
using System.Collections.Generic;
using KRPC.Service;
using KRPC.Service.Attributes;
using KRPC.Utils;
using UnityEngine;
using ObjectDestroyedException = KRPC.Service.KRPC.ObjectDestroyedException;

namespace KRPC.DockingCamera
{
    /// <summary>
    /// A camera from Docking Camera (KURS), on an onboard camera part or a docking port.
    /// </summary>
    [KRPCClass(Service = "DockingCamera")]
    public class Camera : Equatable<Camera>, IGameObjectState
    {
        const int imageSize = 256;

        internal static bool Is(SpaceCenter.Services.Parts.Part part)
        {
            return API.GetModule(part.InternalPart) != null;
        }

        /// <summary>
        /// The state of the camera. It takes the state of its part, and is destroyed once a
        /// live part no longer carries the module.
        /// </summary>
        public GameObjectState GameObjectState
        {
            get
            {
                var state = Part.GameObjectState;
                if (state != GameObjectState.Live)
                    return state;
                return API.GetModule(Part.InternalPart) != null
                    ? GameObjectState.Live : GameObjectState.Destroyed;
            }
        }

        // The camera module of the part. Every member that reaches the mod goes through this.
        PartModule Module
        {
            get
            {
                var module = API.GetModule(Part.InternalPart);
                if (module == null)
                    throw new ObjectDestroyedException(
                        "The camera no longer exists, as its part no longer has one.");
                return module;
            }
        }

        internal Camera(SpaceCenter.Services.Parts.Part part)
        {
            if (!Is(part))
                throw new ArgumentException("Part is not a Camera");
            Part = part;
        }

        /// <summary>
        /// Check that the cameras are the same.
        /// </summary>
        public override bool Equals(Camera other)
        {
            return !ReferenceEquals(other, null) && Part == other.Part;
        }

        /// <summary>
        /// Hash the Camera.
        /// </summary>
        public override int GetHashCode()
        {
            return Part.GetHashCode();
        }

        /// <summary>
        /// Get the part containing this camera.
        /// </summary>
        [KRPCProperty]
        public SpaceCenter.Services.Parts.Part Part
        {
            get; private set;
        }

        /// <summary>
        /// The image from the camera, a 256 by 256 pixel PNG.
        /// Throws an exception if the camera is off or not started.
        /// </summary>
        /// <remarks>
        /// The camera is switched on by setting the <c>IsEnabled</c> field of its
        /// <c>PartCameraModule</c> or <c>DockingCameraModule</c> to <c>true</c>, which also opens
        /// the camera window.
        /// </remarks>
        [KRPCProperty]
        public byte[] Image
        {
            get { return API.GetImage(Module, imageSize, imageSize); }
        }
    }
}
