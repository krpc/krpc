using System;
using System.Collections.Generic;
using System.Linq;
using KRPC.Service;
using KRPC.Service.Attributes;
using KRPC.Service.StdLib;
using KRPC.Utils;
using UnityEngine;

namespace KRPC.ScanSat
{
    /// <summary>
    /// This service provides the scan data, scanners and maps of
    /// <a href="https://github.com/KSPModStewards/SCANsat">SCANsat</a>.
    /// </summary>
    /// <remarks>
    /// The readouts at a position return <c>null</c> where the scans they need have not
    /// covered it, as the maps of the mod show nothing there.
    /// </remarks>
    [KRPCService (Id = ServiceInfo.Id, GameScene = GameScene.All)]
    public static class ScanSat
    {
        // The scenes with SCANsat's scenario module, which holds the scan data
        const GameScene scanScenes = GameScene.Flight | GameScene.SpaceCenter | GameScene.TrackingStation;
        // The distance between the points the slope is measured over, in meters
        const double slopeDistance = 5;
        // The low resolution scans show heights in steps of 500 m, and abundances in steps of 5%
        const double loResHeightStep = 500;
        const double loResAbundanceStep = 0.05;
        const int minMapWidth = 360;
        const int maxMapWidth = 1440;

        static void CheckAPI ()
        {
            if (!API.IsAvailable)
                throw new InvalidOperationException ("SCANsat is not available");
        }

        static global::CelestialBody InternalBody (SpaceCenter.Services.CelestialBody body)
        {
            CheckAPI ();
            if (ReferenceEquals (body, null))
                throw new ArgumentNullException (nameof (body));
            return body.InternalBody;
        }

        static void CheckScanType (ScanType type)
        {
            if (type == ScanType.None)
                throw new ArgumentException ("No scan types given", nameof (type));
        }

        static bool Covered (global::CelestialBody body, double latitude, double longitude, ScanType type)
        {
            if (latitude < -90 || latitude > 90)
                throw new ArgumentOutOfRangeException (nameof (latitude), "Latitude must be between -90 and 90 degrees");
            // SCANsat's grid wraps the north pole round to the south pole
            latitude = Math.Min (latitude, 89.999999);
            longitude = ((longitude + 180) % 360 + 360) % 360 - 180;
            return API.IsCovered (longitude, latitude, body, (int)type);
        }

        /// <summary>
        /// Whether SCANsat is available.
        /// </summary>
        [KRPCProperty]
        public static bool Available {
            get { return API.IsAvailable; }
        }

        /// <summary>
        /// The fraction of a body covered by scan types, between 0 and 1. For a combination,
        /// the mean coverage of its scan types, so 1 only once every one of them is complete.
        /// SCANsat reports at most 0.999 until the coverage is complete.
        /// Throws an exception if no scan types are given.
        /// </summary>
        /// <param name="body">The body.</param>
        /// <param name="type">The scan types.</param>
        [KRPCProcedure (GameScene = scanScenes)]
        public static double Coverage (SpaceCenter.Services.CelestialBody body, ScanType type)
        {
            var internalBody = InternalBody (body);
            CheckScanType (type);
            return API.GetCoverage ((int)type, internalBody) / 100;
        }

        /// <summary>
        /// Whether at least one of the scan types covers a position on a body. For a
        /// combination, this is <c>true</c> where any one of its scan types covers the position,
        /// and not only where all of them do.
        /// Throws an exception if no scan types are given.
        /// </summary>
        /// <param name="body">The body.</param>
        /// <param name="latitude">The latitude, in degrees.</param>
        /// <param name="longitude">The longitude, in degrees.</param>
        /// <param name="type">The scan types.</param>
        [KRPCProcedure (GameScene = scanScenes)]
        public static bool IsCovered (SpaceCenter.Services.CelestialBody body, double latitude, double longitude, ScanType type)
        {
            var internalBody = InternalBody (body);
            CheckScanType (type);
            return Covered (internalBody, latitude, longitude, type);
        }

        /// <summary>
        /// The height of the terrain relative to sea level at a position, in meters, or
        /// <c>null</c> if no altimetry scan covers it. It is negative over the sea floor. With
        /// low resolution altimetry only, the height is rounded toward zero to a multiple of
        /// 500 meters.
        /// </summary>
        /// <param name="body">The body.</param>
        /// <param name="latitude">The latitude, in degrees.</param>
        /// <param name="longitude">The longitude, in degrees.</param>
        [KRPCProcedure (Nullable = true, GameScene = scanScenes)]
        public static double? Elevation (SpaceCenter.Services.CelestialBody body, double latitude, double longitude)
        {
            var internalBody = InternalBody (body);
            var hiRes = Covered (internalBody, latitude, longitude, ScanType.AltimetryHiRes);
            if (!hiRes && !Covered (internalBody, latitude, longitude, ScanType.AltimetryLoRes))
                return null;
            var elevation = API.GetElevation (internalBody, longitude, latitude);
            if (!hiRes)
                elevation = Math.Truncate (elevation / loResHeightStep) * loResHeightStep;
            return elevation;
        }

        /// <summary>
        /// The terrain slope at a position, in degrees, or <c>null</c> if high resolution
        /// altimetry does not cover it. The slope is averaged over points 5 meters apart.
        /// </summary>
        /// <param name="body">The body.</param>
        /// <param name="latitude">The latitude, in degrees.</param>
        /// <param name="longitude">The longitude, in degrees.</param>
        [KRPCProcedure (Nullable = true, GameScene = scanScenes)]
        public static double? Slope (SpaceCenter.Services.CelestialBody body, double latitude, double longitude)
        {
            var internalBody = InternalBody (body);
            if (!Covered (internalBody, latitude, longitude, ScanType.AltimetryHiRes))
                return null;
            var offset = slopeDistance * 360 / (2 * Math.PI * internalBody.Radius);
            var elevation = API.GetElevation (internalBody, longitude, latitude);
            return API.GetSlope (elevation, internalBody, longitude, latitude, offset);
        }

        /// <summary>
        /// The name of the biome at a position, or <c>null</c> if the biome scan does not
        /// cover it.
        /// Throws an exception if the body has no biomes.
        /// </summary>
        /// <param name="body">The body.</param>
        /// <param name="latitude">The latitude, in degrees.</param>
        /// <param name="longitude">The longitude, in degrees.</param>
        [KRPCProcedure (Nullable = true, GameScene = scanScenes)]
        public static string Biome (SpaceCenter.Services.CelestialBody body, double latitude, double longitude)
        {
            var internalBody = InternalBody (body);
            if (internalBody.BiomeMap == null)
                throw new InvalidOperationException ("Body does not have any biomes");
            if (!Covered (internalBody, latitude, longitude, ScanType.Biome))
                return null;
            return API.GetBiomeName (internalBody, longitude, latitude);
        }

        /// <summary>
        /// The abundance of a resource at a position, between 0 and 1, or <c>null</c> if no
        /// resource scan covers it. With the low resolution scan only, the abundance is rounded
        /// down to a multiple of 0.05.
        /// Throws an exception if SCANsat's resource scanning is off, or SCANsat does not map
        /// the resource.
        /// </summary>
        /// <param name="body">The body.</param>
        /// <param name="latitude">The latitude, in degrees.</param>
        /// <param name="longitude">The longitude, in degrees.</param>
        /// <param name="resource">The name of the resource. See <see cref="Resources" />.</param>
        [KRPCProcedure (Nullable = true, GameScene = scanScenes)]
        public static double? ResourceAbundance (SpaceCenter.Services.CelestialBody body, double latitude, double longitude, string resource)
        {
            var internalBody = InternalBody (body);
            CheckResource (resource);
            var hiRes = Covered (internalBody, latitude, longitude, ScanType.ResourceHiRes);
            if (!hiRes && !Covered (internalBody, latitude, longitude, ScanType.ResourceLoRes))
                return null;
            double abundance = API.ResourceOverlay (latitude, longitude, resource, internalBody, API.Settings.BiomeLock ());
            if (!hiRes)
                abundance = Math.Floor (abundance / loResAbundanceStep) * loResAbundanceStep;
            return abundance;
        }

        static void CheckResource (string resource)
        {
            if (!API.Settings.GlobalResource ())
                throw new InvalidOperationException ("SCANsat resource scanning is off");
            if (!API.ResourceNames ().Contains (resource))
                throw new ArgumentException ("SCANsat does not map the resource " + resource, nameof (resource));
        }

        /// <summary>
        /// The anomalies on a body found by either anomaly scan.
        /// </summary>
        /// <param name="body">The body.</param>
        [KRPCProcedure (GameScene = scanScenes)]
        public static IList<Anomaly> Anomalies (SpaceCenter.Services.CelestialBody body)
        {
            var data = API.GetData (InternalBody (body));
            if (data == null)
                return new List<Anomaly> ();
            // Reading the anomalies updates their known and detail flags from the coverage
            return API.Anomaly.All (data).Cast<object> ()
                .Where (x => API.Anomaly.Known (x) || API.Anomaly.Detail (x))
                .Select (x => new Anomaly {
                    Name = API.Anomaly.Detail (x) ? API.Anomaly.Name (x) : null,
                    Latitude = API.Anomaly.Latitude (x),
                    Longitude = API.Anomaly.Longitude (x)
                }).ToList ();
        }

        /// <summary>
        /// The names of the resources SCANsat maps. Empty if SCANsat's resource scanning
        /// is off.
        /// </summary>
        [KRPCProperty]
        public static IList<string> Resources {
            get {
                CheckAPI ();
                return API.Settings.GlobalResource () ? API.ResourceNames () : new List<string> ();
            }
        }

        static object Settings {
            get {
                CheckAPI ();
                var settings = API.Settings.Instance ();
                if (settings == null)
                    throw new InvalidOperationException ("SCANsat settings are not loaded");
                return settings;
            }
        }

        /// <summary>
        /// Whether scanners scan while their vessel is not the active vessel. A setting of
        /// SCANsat.
        /// </summary>
        [KRPCProperty]
        public static bool BackgroundScanning {
            get { return API.Settings.BackgroundScanning (Settings); }
        }

        /// <summary>
        /// Whether scanners that need light stop scanning in darkness. A setting of SCANsat.
        /// </summary>
        [KRPCProperty]
        public static bool DaylightCheck {
            get { return API.Settings.DaylightCheck (Settings); }
        }

        /// <summary>
        /// Whether the stock orbital survey fills the high resolution resource scan of a body
        /// at once. A setting of SCANsat.
        /// </summary>
        [KRPCProperty]
        public static bool InstantResourceScan {
            get {
                CheckAPI ();
                return API.Settings.InstantResourceScan ();
            }
        }

        /// <summary>
        /// Whether the stock resource scanners work. A setting of SCANsat.
        /// </summary>
        [KRPCProperty]
        public static bool StockResourceScan {
            get {
                CheckAPI ();
                return API.Settings.StockResourceScan ();
            }
        }

        /// <summary>
        /// Whether resource abundances are approximate until a surface sample is taken in the
        /// biome. A setting of SCANsat.
        /// </summary>
        [KRPCProperty]
        public static bool BiomeLock {
            get {
                CheckAPI ();
                return API.Settings.BiomeLock ();
            }
        }

        /// <summary>
        /// Whether a narrow-band scanner is needed on the vessel for exact resource
        /// abundances. A setting of SCANsat.
        /// </summary>
        [KRPCProperty]
        public static bool RequireNarrowBand {
            get {
                CheckAPI ();
                return API.Settings.RequireNarrowBand ();
            }
        }

        /// <summary>
        /// The scanner on a part.
        /// Throws an exception if the part has no SCANsat scanner.
        /// </summary>
        /// <param name="part">The part.</param>
        [KRPCProcedure (GameScene = GameScene.Flight)]
        public static Scanner Scanner (SpaceCenter.Services.Parts.Part part)
        {
            CheckAPI ();
            return new Scanner (part);
        }

        /// <summary>
        /// The SCANsat scanners on a vessel.
        /// </summary>
        /// <param name="vessel">The vessel.</param>
        [KRPCProcedure (GameScene = GameScene.Flight)]
        public static IList<Scanner> Scanners (SpaceCenter.Services.Vessel vessel)
        {
            CheckAPI ();
            if (ReferenceEquals (vessel, null))
                throw new ArgumentNullException (nameof (vessel));
            return vessel.Parts.All.Where (global::KRPC.ScanSat.Scanner.Is).Select (x => new Scanner (x)).ToList ();
        }

        /// <summary>
        /// A map of a body from the scan data, as an image. The map is half as high as it is
        /// wide. Building it takes several game frames.
        /// Throws an exception if SCANsat cannot render the map, as when its visual maps are
        /// switched off.
        /// </summary>
        /// <param name="body">The body.</param>
        /// <param name="type">The type of map.</param>
        /// <param name="width">The width of the image in pixels, between 360 and 1440.</param>
        /// <param name="projection">The projection.</param>
        /// <param name="color">Whether the map is in color, or in grayscale.</param>
        /// <param name="resource">The name of a resource to overlay, or an empty string for
        /// none. See <see cref="Resources" />.</param>
        /// <param name="format">The format of the image.</param>
        /// <param name="quality">The quality of a JPEG image, from 1 to 100.</param>
        [KRPCProcedure (GameScene = scanScenes)]
        public static byte[] Map (SpaceCenter.Services.CelestialBody body, MapType type, int width = 720,
                                  MapProjection projection = MapProjection.Rectangular, bool color = true, string resource = "",
                                  ImageFormat format = ImageFormat.PNG, int quality = 75)
        {
            var internalBody = InternalBody (body);
            ImageEncoder.CheckQuality (format, quality);
            if (width < minMapWidth || width > maxMapWidth)
                throw new ArgumentOutOfRangeException (nameof (width), "The width must be between " + minMapWidth + " and " + maxMapWidth);
            var showResource = !string.IsNullOrEmpty (resource);
            if (showResource)
                CheckResource (resource);
            var map = API.Map.New (internalBody, API.Map.Source);
            try {
                API.Map.SetColor (map, color);
                API.Map.SetProjection (map, (int)projection);
                API.Map.SetBody (map, internalBody);
                API.Map.SetSize (map, width, width / 2, 2, 0, 0);
                if (showResource) {
                    var node = API.Resource.Get (resource, false);
                    API.Resource.CurrentBodyConfig (node, internalBody.bodyName);
                    API.Map.SetResource (map, node);
                }
                API.Map.Reset (map, (int)type, showResource, true);
            } catch {
                API.Map.Destroy (map);
                throw;
            }
            return BuildMap (map, format, quality);
        }

        // Builds a slice of the map each game frame, under SCANsat's own time budget
        static byte[] BuildMap (object map, ImageFormat format, int quality)
        {
            try {
                API.Map.Build (map);
                if (!API.Map.IsComplete (map))
                    throw new YieldException<Func<byte[]>> (() => BuildMap (map, format, quality));
                var texture = API.Map.Texture (map);
                if (texture == null)
                    throw new InvalidOperationException ("SCANsat did not render the map");
                var active = RenderTexture.active;
                var image = new Texture2D (texture.width, texture.height, TextureFormat.ARGB32, false);
                try {
                    RenderTexture.active = texture;
                    image.ReadPixels (new Rect (0, 0, texture.width, texture.height), 0, 0);
                    image.Apply ();
                    var encoded = ImageEncoder.Encode (image, format, quality);
                    API.Map.Destroy (map);
                    return encoded;
                } finally {
                    RenderTexture.active = active;
                    UnityEngine.Object.Destroy (image);
                }
            } catch (YieldException) {
                throw;
            } catch {
                API.Map.Destroy (map);
                throw;
            }
        }
    }
}
