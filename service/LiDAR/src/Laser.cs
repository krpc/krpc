using System;
using System.Linq;
using System.Collections.Generic;
using KRPC.Service.Attributes;
using KRPC.Utils;
using UnityEngine;

namespace KRPC.LiDAR
{
    /// <summary>
    /// A LaserDist laser.
    /// </summary>
    [KRPCClass(Service = "LiDAR")]
    public class Laser : Equatable<Laser>
    {
        internal static bool Is(SpaceCenter.Services.Parts.Part part)
        {
            return part.InternalPart.Modules.Contains("LiDARModule");
        }

        internal Laser(SpaceCenter.Services.Parts.Part part)
        {
            if (!Is(part))
                throw new ArgumentException("Part is not a LiDAR");
            Part = part;
        }

        /// <summary>
        /// Check that the LiDAR are the same.
        /// </summary>
        public override bool Equals(Laser other)
        {
            return !ReferenceEquals(other, null) && Part == other.Part;
        }

        /// <summary>
        /// Hash the LiDAR.
        /// </summary>
        public override int GetHashCode()
        {
            return Part.GetHashCode();
        }

        /// <summary>
        /// Get the part containing this LiDAR.
        /// </summary>
        [KRPCProperty]
        public SpaceCenter.Services.Parts.Part Part
        {
            get; private set;
        }

        /// <summary>
        /// Get the point cloud from the LiDAR.
        /// Returns an empty list on failure.
        /// </summary>
        [KRPCProperty]
        public IList<double> Cloud
        {
            get {
                if (API.IsAvailable)
                    return API.GetCloud(Part.InternalPart);
                else
                    return new List<double>();
            }
        }
    }
}
