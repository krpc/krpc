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
        readonly SpaceCenter.Services.Parts.Part part;

        internal static bool Is(SpaceCenter.Services.Parts.Part innerPart)
        {
            return innerPart.InternalPart.Modules.Contains("LiDARModule");
        }

        internal Laser(SpaceCenter.Services.Parts.Part innerPart)
        {
            part = innerPart;
            if (!Is(part))
            {
                throw new ArgumentException("Part is not a LiDAR");
            }
        }

        /// <summary>
        /// Check that the LiDAR are the same.
        /// </summary>
        public override bool Equals(Laser other)
        {
            return !ReferenceEquals(other, null) && part == other.part;
        }

        /// <summary>
        /// Hash the LiDAR.
        /// </summary>
        public override int GetHashCode()
        {
            return part.GetHashCode();
        }

        /// <summary>
        /// Get the part containing this LiDAR.
        /// </summary>
        [KRPCProperty]
        public SpaceCenter.Services.Parts.Part Part
        {
            get { return part; }
        }

        /// <summary>
        /// Get the pointcloud.
        /// </summary>
        [KRPCProperty]
        public IList<double> Cloud
        {
            get {
                if (API.IsAvailable)
                {
                    var cloud = API.GetCloud(part.InternalPart);
                    IList<double> outCloud = new List<double>();
                    var count = cloud.Count;
                    if (count%3 == 0)
                    {
                        for (int i = 0; i < count; i += 3)
                        {
                            Vector3d p = new Vector3d(cloud[i], cloud[i + 1], cloud[i + 2]);
                            outCloud.Add(p.x);
                            outCloud.Add(p.y);
                            outCloud.Add(p.z);
                        }
                    }
                    return outCloud;
                }
                else
                    return new List<double>();
            }
        }
    }
}
