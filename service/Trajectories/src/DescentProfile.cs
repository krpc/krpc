using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.Utils;

namespace KRPC.Trajectories
{
    /// <summary>
    /// The descent profile, which sets the attitude of the vessel during the descent.
    /// </summary>
    /// <remarks>
    /// Each node covers a band of altitude, as a percentage of the height of the atmosphere.
    /// A body without an atmosphere uses <see cref="FinalApproach"/> throughout.
    /// </remarks>
    [KRPCClass(Service = "Trajectories")]
    public class DescentProfile : Equatable<DescentProfile>
    {
        private static DescentProfileNode entry;
        private static DescentProfileNode highAltitude;
        private static DescentProfileNode lowAltitude;
        private static DescentProfileNode finalApproach;

        /// <summary>
        /// The node for atmospheric entry, from 50% to 100% of the height of the atmosphere.
        /// </summary>
        [KRPCProperty]
        public DescentProfileNode Entry
        {
            get
            {
                if (entry == null)
                    entry = new DescentProfileNode(API.DescentProfile.AtmosphericEntry);

                return entry;
            }
        }

        /// <summary>
        /// The node for high altitude, from 25% to 50% of the height of the atmosphere.
        /// </summary>
        [KRPCProperty]
        public DescentProfileNode HighAltitude
        {
            get
            {
                if (highAltitude == null)
                    highAltitude = new DescentProfileNode(API.DescentProfile.HighAlt);

                return highAltitude;
            }
        }

        /// <summary>
        /// The node for low altitude, from 5% to 25% of the height of the atmosphere.
        /// </summary>
        [KRPCProperty]
        public DescentProfileNode LowAltitude
        {
            get
            {
                if (lowAltitude == null)
                    lowAltitude = new DescentProfileNode(API.DescentProfile.LowAlt);

                return lowAltitude;
            }
        }

        /// <summary>
        /// The node for the final approach, below 5% of the height of the atmosphere.
        /// </summary>
        [KRPCProperty]
        public DescentProfileNode FinalApproach
        {
            get
            {
                if (finalApproach == null)
                    finalApproach = new DescentProfileNode(API.DescentProfile.Approach);

                return finalApproach;
            }
        }

        /// <summary>
        /// Saves the descent profile to the active vessel.
        /// </summary>
        /// <remarks>
        /// A change to a node applies without saving. Saving keeps it in the saved game.
        /// </remarks>
        [KRPCMethod]
        public static void Save()
        {
            API.DescentProfile.Save();
        }

        /// <summary>
        /// Sets every node to an angle of attack, and saves the descent profile.
        /// </summary>
        /// <param name="aoa">The angle of attack in degrees. The default, 180, is retrograde.</param>
        [KRPCMethod]
        public static void Reset(double aoa = 180.0)
        {
            API.API.ResetDescentProfile(GeometryExtensions.ToRadians(aoa));
        }

        /// <summary>
        /// Whether the descent profiles are the same.
        /// </summary>
        public override bool Equals(DescentProfile other)
        {
            return !ReferenceEquals(other, null) && Entry == other.Entry;
        }

        /// <summary>
        /// Hash the descent profile.
        /// </summary>
        public override int GetHashCode()
        {
            return Entry.GetHashCode();
        }
    }
}
