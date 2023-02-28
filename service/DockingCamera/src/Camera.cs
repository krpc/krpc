using System;
using System.Linq;
using System.Collections.Generic;
using KRPC.Service.Attributes;
using KRPC.Utils;
using UnityEngine;

namespace KRPC.DockingCamera
{
    /// <summary>
    /// A Docking Camera.
    /// </summary>
    [KRPCClass(Service = "DockingCamera")]
    public class Camera : Equatable<Camera>
    {
        readonly SpaceCenter.Services.Parts.Part part;

        internal static bool Is(SpaceCenter.Services.Parts.Part innerPart)
        {
            return innerPart.InternalPart.Modules.Contains("PartCameraModule");
        }

        internal Camera(SpaceCenter.Services.Parts.Part innerPart)
        {
            part = innerPart;
            if (!Is(part))
            {
                throw new ArgumentException("Part is not a Camera");
            }
        }

        /// <summary>
        /// Check that the Camera are the same.
        /// </summary>
        public override bool Equals(Camera other)
        {
            return !ReferenceEquals(other, null) && part == other.part;
        }

        /// <summary>
        /// Hash the Camera.
        /// </summary>
        public override int GetHashCode()
        {
            return part.GetHashCode();
        }

        /// <summary>
        /// Get the part containing this Camera.
        /// </summary>
        [KRPCProperty]
        public SpaceCenter.Services.Parts.Part Part
        {
            get { return part; }
        }

        /// <summary>
        /// Get the image.
        /// </summary>
        [KRPCProperty]
        public byte[] Image
        {
            get {
                if (API.IsAvailable)
                {
                    try
                    {
                        var image = API.GetImage(part.InternalPart);
                        Debug.Log("CAMERA IMAGE: OK");
                        return image;
                    }
                    catch(System.Exception e)
                    {
                        Debug.Log("CAMERA IMAGE: " + e.Message + Environment.NewLine + e.StackTrace);
                        return Array.Empty<byte>();
                    }
                    
                }
                else
                {
                    Debug.Log("CAMERA IMAGE: FAILED");
                    return Array.Empty<byte>();
                }
            }
        }
    }
}
