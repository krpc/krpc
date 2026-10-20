using System;
using KRPC.Service.Attributes;
using KSPEditorFacility = EditorFacility;

namespace KRPC.SpaceCenter.Services
{
    /// <summary>
    /// A place where craft can be launched from.
    /// More of these can be added with mods like Kerbal Konstructs.
    /// </summary>
    [KRPCStruct (Service = "SpaceCenter")]
    public struct LaunchSite
    {
        internal LaunchSite (string name, CelestialBody body, KSPEditorFacility editorFacility) : this ()
        {
            if (ReferenceEquals (body, null))
                throw new ArgumentNullException (nameof (body));
            Name = name;
            Body = body;
            EditorFacility = (EditorFacility)editorFacility;
        }

        /// <summary>
        /// The name of the launch site.
        /// </summary>
        [KRPCProperty]
        public string Name { get; set; }

        /// <summary>
        /// The celestial body the launch site is on.
        /// </summary>
        [KRPCProperty]
        public CelestialBody Body { get; set; }

        /// <summary>
        /// Which editor is normally used for this launch site.
        /// </summary>
        [KRPCProperty]
        public EditorFacility EditorFacility { get; set; }
    }
}
