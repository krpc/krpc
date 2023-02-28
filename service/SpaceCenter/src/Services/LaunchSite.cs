using System;
using KRPC.Service.Attributes;
using KRPC.Utils;
using KSPEditorFacility = EditorFacility;

namespace KRPC.SpaceCenter.Services
{
    /// <summary>
    /// A place where craft can be launched from.
    /// More of these can be added with mods like Kerbal Konstructs.
    /// </summary>
    [KRPCClass(Service = "SpaceCenter")]
    public class LaunchSite : Equatable<LaunchSite>
    {
        /// <summary>
        /// Create a launch site
        /// </summary>
        public LaunchSite(string name, CelestialBody body, KSPEditorFacility editorFacility)
        {
            if (ReferenceEquals (body, null))
                throw new ArgumentNullException (nameof (body));
            if (ReferenceEquals (editorFacility, null))
                throw new ArgumentNullException (nameof (editorFacility));
            Name = name;
            Body = body;
            EditorFacility = (EditorFacility)editorFacility;
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (LaunchSite other)
        {
            return !ReferenceEquals (other, null) && Name == other.Name;
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            return Name.GetHashCode ();
        }

        /// <summary>
        /// The name of the launch site.
        /// </summary>
        [KRPCProperty]
        public string Name { get; private set; }

        /// <summary>
        /// The celestial body the launch site is on.
        /// </summary>
        [KRPCProperty]
        public CelestialBody Body { get; private set; }

        /// <summary>
        /// Which editor is normally used for this launch site.
        /// </summary>
        [KRPCProperty]
        public EditorFacility EditorFacility { get; private set; }
    }
}
