using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
        internal static bool Is(SpaceCenter.Services.Parts.Part innerPart)
        {
            return innerPart.InternalPart.Modules.Contains("PartCameraModule");
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
        /// Get an image.
        /// Returns an empty byte array on failure.
        /// </summary>
        [KRPCProperty]
        [SuppressMessage ("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        [SuppressMessage ("Gendarme.Rules.Performance", "AvoidReturningArraysOnPropertiesRule")]
        public byte[] Image
        {
            get {
                if (API.IsAvailable)
                {
                    try
                    {
                        var image = API.GetImage(Part.InternalPart);
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
