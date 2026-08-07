using System;
using System.Linq;
using System.Collections.Generic;
using KRPC.Service.Attributes;
using KRPC.Utils;
using UnityEngine;
using ObjectDestroyedException = KRPC.Service.KRPC.ObjectDestroyedException;

namespace KRPC.LiDAR
{
    /// <summary>
    /// A LaserDist laser.
    /// </summary>
    [KRPCClass(Service = "LiDAR")]
    public class Laser : Equatable<Laser>, IGameObjectState
    {
        // The module that makes a part a laser. The mod's API takes the part rather than
        // the module, so the part is what identifies this and all it has to reach.
        const string moduleName = "LiDARModule";

        internal static bool Is(SpaceCenter.Services.Parts.Part part)
        {
            return part.InternalPart.Modules.Contains(moduleName);
        }

        /// <summary>
        /// What the game holds for the laser. It belongs to its part, so it is exactly as
        /// live, dormant or destroyed as the part, and destroyed when a part that is there
        /// to look at no longer carries the module.
        /// </summary>
        public GameObjectState GameObjectState
        {
            get
            {
                var state = Part.GameObjectState;
                if (state != GameObjectState.Live)
                    return state;
                return Part.InternalPart.Modules.Contains(moduleName)
                    ? GameObjectState.Live : GameObjectState.Destroyed;
            }
        }

        // The part it is on, checked to still carry the module. Every member that reaches
        // the mod goes through this.
        global::Part InternalPart
        {
            get
            {
                var innerPart = Part.InternalPart;
                if (!innerPart.Modules.Contains(moduleName))
                    throw new ObjectDestroyedException(
                        "The laser no longer exists, as its part no longer has one.");
                return innerPart;
            }
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
                    return API.GetCloud(InternalPart);
                else
                    return new List<double>();
            }
        }
    }
}
