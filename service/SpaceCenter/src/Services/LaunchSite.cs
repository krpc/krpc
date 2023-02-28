using KRPC.Service.Attributes;
using KRPC.Utils;

namespace KRPC.SpaceCenter.Services
{
    /// <summary>
    /// A place where crafts can be launched from.  More of these can be added with mods like Kerbal Konstructs.
    /// </summary>
    [KRPCClass(Service = "SpaceCenter")]
    public class LaunchSite : Equatable<LaunchSite>
    {
        /// <summary>
        /// The name of the launchsite.
        /// </summary>
        [KRPCProperty]
        public string Name { get; private set; }

        /// <summary>
        /// The body the launchsite is on.
        /// </summary>
        [KRPCProperty]
        public CelestialBody Body { get; private set; }

        /// <summary>
        /// Which editor type this launchsite noramlly handles.  One of "None", "VAB" or "SPH"
        /// </summary>
        [KRPCProperty]
        public string EditorFacility { get; private set; }

        /// <summary>
        /// Creates a launchsite object for communication with the client
        /// </summary>
        /// <param name="name"></param>
        /// <param name="body"></param>
        /// <param name="editorFacility"></param>
        public LaunchSite(string name, CelestialBody body, EditorFacility editorFacility)
        {
            Name = name;
            Body = body;
            EditorFacility = editorFacility.ToString();
        }

        /// <summary>
        /// Compare for equality.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public override bool Equals(LaunchSite other)
        {
            return Name == other.Name;
        }

        /// <summary>
        /// Gets the HashCode.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }
    }

}