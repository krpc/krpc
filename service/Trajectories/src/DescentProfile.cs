using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.Utils;

namespace KRPC.Trajectories
{
    /// <summary>
    /// Trajectories descent profile.
    /// 
    /// 
    /// Body atmosphere depth is defined as vessel altitude between equatorial radius and max atmosphere height.
    /// 
    /// On bodies without an atmosphere <see cref="KRPC.Trajectories.DescentProfile.FinalApproach"/> is always used.
    /// </summary>
    [KRPCClass(Service = "Trajectories")]
    public class DescentProfile : Equatable<DescentProfile>
    {
        private static DescentProfileNode entry;
        private static DescentProfileNode highAltitude;
        private static DescentProfileNode lowAltitude;
        private static DescentProfileNode finalApproach;

        /// <summary>
        /// Atmospheric entry, 50 to 100% of body atmosphere depth
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
        /// High Altitude, 25 to 50% of body atmosphere depth
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
        /// Low Altitude, 5 to 25% of body atmosphere depth
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
        /// Final Approach, under 5% of body atmosphere depth or non-atmospheric Body
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
        /// Saves the current descent profile to vessel.
        /// 
        /// You don't need to call this after changing angles/modes, only use to include current descent profile in save file. 
        /// </summary>
        [KRPCMethod]
        public static void Save()
        {
            API.DescentProfile.Save();
        }

        /// <summary>
        /// Resets all to the passed angle of attack in degrees from prograde.
        /// </summary>
        /// <param name="aoa">Angle of attack in degrees, default value is retrograde (180 degrees)</param>
        [KRPCMethod]
        public static void Reset(double aoa = 180.0)
        {
            API.API.ResetDescentProfile(GeometryExtensions.ToRadians(aoa));
        }

        /// <summary>
        /// Check that DescentProfile is the same.
        /// </summary>
        public override bool Equals(DescentProfile other)
        {
            return !ReferenceEquals(other, null) && Entry == other.Entry;
        }

        /// <summary>
        /// Hash the Node.
        /// </summary>
        public override int GetHashCode()
        {
            return Entry.GetHashCode();
        }
    }
}
