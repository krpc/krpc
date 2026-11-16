using System;
using System.Collections.Generic;
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
        LaunchSite (string name, global::CelestialBody body, KSPEditorFacility editorFacility,
                    double latitude, double longitude, double altitude) : this ()
        {
            if (ReferenceEquals (body, null))
                throw new ArgumentNullException (nameof (body));
            Name = name;
            Body = new CelestialBody (body);
            EditorFacility = (EditorFacility)editorFacility;
            Latitude = latitude;
            Longitude = longitude;
            Altitude = altitude;
        }

        /// <summary>
        /// The launch sites in the game, from its launch site list and its space center
        /// facilities that launch vessels.
        /// </summary>
        internal static IList<LaunchSite> All {
            get {
                var psystem = PSystemSetup.Instance;
                var list = new List<LaunchSite> (psystem.LaunchSites.Count + psystem.SpaceCenterFacilities.Length);
                foreach (var site in psystem.LaunchSites)
                    list.Add (Create (site));
                foreach (var facility in psystem.SpaceCenterFacilities)
                    if (facility.IsLaunchFacility ())
                        list.Add (Create (facility));
                return list;
            }
        }

        /// <summary>
        /// The launch site with the given name, or <c>null</c> if there is none.
        /// </summary>
        internal static LaunchSite? Find (string name)
        {
            if (string.IsNullOrEmpty (name))
                return null;
            var psystem = PSystemSetup.Instance;
            foreach (var site in psystem.LaunchSites)
                if (site.name == name)
                    return Create (site);
            foreach (var facility in psystem.SpaceCenterFacilities)
                if (facility.IsLaunchFacility () && facility.facilityName == name)
                    return Create (facility);
            return null;
        }

        static LaunchSite Create (global::LaunchSite site)
        {
            double latitude = double.NaN, longitude = double.NaN, altitude = double.NaN;
            if (site.spawnPoints.Length > 0)
                site.spawnPoints [0].GetSpawnPointLatLonAlt (out latitude, out longitude, out altitude);
            return new LaunchSite (site.name, site.Body, site.editorFacility, latitude, longitude, altitude);
        }

        static LaunchSite Create (PSystemSetup.SpaceCenterFacility facility)
        {
            double latitude = double.NaN, longitude = double.NaN, altitude = double.NaN;
            if (facility.spawnPoints.Length > 0)
                facility.spawnPoints [0].GetSpawnPointLatLonAlt (out latitude, out longitude, out altitude);
            return new LaunchSite (facility.facilityName, facility.hostBody, facility.editorFacility, latitude, longitude, altitude);
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

        /// <summary>
        /// The latitude of the point vessels launch from, in degrees, or <c>NaN</c> if the
        /// site has no spawn point.
        /// </summary>
        [KRPCProperty]
        public double Latitude { get; set; }

        /// <summary>
        /// The longitude of the point vessels launch from, in degrees, or <c>NaN</c> if the
        /// site has no spawn point.
        /// </summary>
        [KRPCProperty]
        public double Longitude { get; set; }

        /// <summary>
        /// The altitude above sea level of the point vessels launch from, in meters, or
        /// <c>NaN</c> if the site has no spawn point.
        /// </summary>
        [KRPCProperty]
        public double Altitude { get; set; }
    }
}
