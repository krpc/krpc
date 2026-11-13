using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using KRPC.Utils;
using UnityEngine;
using Logger = KRPC.Utils.Logger;

namespace KRPC.ScanSat
{
    // Binds to the members of SCANsat 22.1 by reflection. The coverage and settings come from the
    // public API region of SCANUtil. The readouts, the sensor status and the map renderer are
    // internal members, bound the same way.
    sealed class API : ModAPI
    {
        public API () : base (ServiceInfo.Mods.SCANsat.Name, ServiceInfo.Mods.SCANsat.Assembly)
        {
        }

        public static Func<double, double, global::CelestialBody, int, bool> IsCovered;
        public static Func<int, global::CelestialBody, double> GetCoverage;
        public static Func<global::CelestialBody, object> GetData;
        // Longitude before latitude, as in SCANsat
        public static Func<global::CelestialBody, double, double, double> GetElevation;
        public static Func<double, global::CelestialBody, double, double, double, double> GetSlope;
        public static Func<global::CelestialBody, double, double, string> GetBiomeName;
        // Latitude before longitude, as in SCANsat
        public static Func<double, double, string, global::CelestialBody, bool, float> ResourceOverlay;

        public static class Settings
        {
            public static Func<bool> InstantResourceScan;
            public static Func<bool> StockResourceScan;
            public static Func<bool> BiomeLock;
            public static Func<bool> RequireNarrowBand;
            public static Func<bool> GlobalResource;
            public static Func<object> Instance;
            public static Func<object, bool> BackgroundScanning;
            public static Func<object, bool> DaylightCheck;
        }

        public static class Resource
        {
            public static Func<object> All;
            public static Func<object, string> Name;
            public static Func<string, bool, object> Get;
            public static Action<object, string> CurrentBodyConfig;
        }

        public static class Anomaly
        {
            public static Func<object, Array> All;
            public static Func<object, bool> Known;
            public static Func<object, bool> Detail;
            public static Func<object, string> Name;
            public static Func<object, double> Latitude;
            public static Func<object, double> Longitude;
        }

        public static class Sensor
        {
            public static Func<object> Controller;
            public static Func<object, global::Vessel, int, object> Status;
            public static Func<object, bool> InRange;
            public static Func<object, bool> BestRange;
            public static Func<object, bool> InDarkness;
        }

        public static class Module
        {
            public static Type Scanner;
            public static Type ResourceScanner;
            public static Func<PartModule, int> SensorType;
            public static Func<PartModule, float> FieldOfView;
            public static Func<PartModule, float> MinAltitude;
            public static Func<PartModule, float> MaxAltitude;
            public static Func<PartModule, float> BestAltitude;
            public static Func<PartModule, bool> RequireLight;
            public static Func<PartModule, bool> Scanning;
            public static Action<PartModule> StartScan;
            public static Action<PartModule> StopScan;
            public static Action<PartModule, KSPActionParam> StartResourceScan;
        }

        public static class Map
        {
            // The map source SCANsat gives each Raster Prop Monitor screen. It is made for many
            // independent maps.
            public const int Source = 3;
            public static Func<global::CelestialBody, int, object> New;
            public static Action<object, int> SetProjection;
            public static Action<object, global::CelestialBody> SetBody;
            public static Action<object, int, int, int, int, int> SetSize;
            public static Action<object, bool> SetColor;
            public static Action<object, object> SetResource;
            public static Action<object, int, bool, bool> Reset;
            public static Action<object> Build;
            public static Func<object, bool> IsComplete;
            public static Func<object, RenderTexture> Texture;
            public static Action<object> Destroy;
        }

        protected override void Bind (ModBinder mod)
        {
            var util = mod.Type ("SCANsat.SCANUtil");
            IsCovered = util.Method<Func<double, double, global::CelestialBody, int, bool>> ("isCovered");
            GetCoverage = util.Method<Func<int, global::CelestialBody, double>> ("GetCoverage");
            GetData = util.Method<Func<global::CelestialBody, object>> ("getData");
            GetElevation = util.Method<Func<global::CelestialBody, double, double, double>> ("getElevation");
            GetSlope = util.Method<Func<double, global::CelestialBody, double, double, double, double>> ("slope");
            GetBiomeName = util.Method<Func<global::CelestialBody, double, double, string>> ("getBiomeName");
            ResourceOverlay = util.Method<Func<double, double, string, global::CelestialBody, bool, float>> ("ResourceOverlay");

            Settings.InstantResourceScan = util.Method<Func<bool>> ("instantResourceScanEnabled");
            Settings.StockResourceScan = util.Method<Func<bool>> ("stockResourceScanEnabled");
            Settings.BiomeLock = util.Method<Func<bool>> ("resourceBiomeLockEnabled");
            Settings.RequireNarrowBand = util.Method<Func<bool>> ("narrowBandResourceRestrictionEnabled");
            Settings.GlobalResource = mod.Type ("SCANsat.SCANconfigLoader").StaticGetter<bool> ("GlobalResource");
            var settings = mod.Type ("SCANsat.SCAN_Settings_Config");
            Settings.Instance = settings.StaticGetter<object> ("Instance");
            Settings.BackgroundScanning = settings.Getter<object, bool> ("BackgroundScanning");
            Settings.DaylightCheck = settings.Getter<object, bool> ("DaylightCheck");

            var controller = mod.Type ("SCANsat.SCANcontroller");
            var resource = mod.Type ("SCANsat.SCAN_Data.SCANresourceGlobal");
            Resource.All = controller.Method<Func<object>> ("resources");
            Resource.Name = resource.Getter<object, string> ("Name");
            Resource.Get = controller.Method<Func<string, bool, object>> ("getResourceNode");
            Resource.CurrentBodyConfig = resource.Method<Action<object, string>> ("CurrentBodyConfig");

            var data = mod.Type ("SCANsat.SCAN_Data.SCANdata");
            var anomaly = mod.Type ("SCANsat.SCAN_Data.SCANanomaly");
            Anomaly.All = data.Getter<object, Array> ("Anomalies");
            Anomaly.Known = anomaly.Getter<object, bool> ("Known");
            Anomaly.Detail = anomaly.Getter<object, bool> ("Detail");
            Anomaly.Name = anomaly.Getter<object, string> ("Name");
            Anomaly.Latitude = anomaly.Getter<object, double> ("Latitude");
            Anomaly.Longitude = anomaly.Getter<object, double> ("Longitude");

            var sensor = mod.Type ("SCANsat.SCANcontroller+SCANsensor");
            Sensor.Controller = controller.StaticGetter<object> ("controller");
            Sensor.Status = controller.Method<Func<object, global::Vessel, int, object>> ("getSensorStatus");
            Sensor.InRange = sensor.Getter<object, bool> ("inRange");
            Sensor.BestRange = sensor.Getter<object, bool> ("bestRange");
            Sensor.InDarkness = sensor.Getter<object, bool> ("inDarkness");

            var scanner = mod.Type ("SCANsat.SCAN_PartModules.SCANsat");
            var resourceScanner = mod.Type ("SCANsat.SCAN_PartModules.ModuleSCANresourceScanner");
            Module.Scanner = scanner.Type;
            Module.ResourceScanner = resourceScanner.Type;
            Module.SensorType = scanner.Getter<PartModule, int> ("sensorType");
            Module.FieldOfView = scanner.Getter<PartModule, float> ("fov");
            Module.MinAltitude = scanner.Getter<PartModule, float> ("min_alt");
            Module.MaxAltitude = scanner.Getter<PartModule, float> ("max_alt");
            Module.BestAltitude = scanner.Getter<PartModule, float> ("best_alt");
            Module.RequireLight = scanner.Getter<PartModule, bool> ("requireLight");
            Module.Scanning = scanner.Getter<PartModule, bool> ("scanningNow");
            Module.StartScan = scanner.Method<Action<PartModule>> ("startScan");
            Module.StopScan = scanner.Method<Action<PartModule>> ("stopScan");
            Module.StartResourceScan = resourceScanner.Method<Action<PartModule, KSPActionParam>> ("startResourceScanAction");

            var map = mod.Type ("SCANsat.SCAN_Map.SCANmap");
            Map.New = map.Constructor<Func<global::CelestialBody, int, object>> ();
            Map.SetProjection = map.Method<Action<object, int>> ("setProjection");
            Map.SetBody = map.Method<Action<object, global::CelestialBody>> ("setBody");
            Map.SetSize = map.Method<Action<object, int, int, int, int, int>> ("setSize");
            Map.SetColor = map.Setter<object, bool> ("ColorMap");
            Map.SetResource = map.Setter<object, object> ("Resource");
            Map.Reset = map.Method<Action<object, int, bool, bool>> ("resetMap");
            Map.Build = map.Method<Action<object>> ("getPartialMap");
            Map.IsComplete = map.Method<Func<object, bool>> ("isMapComplete");
            Map.Texture = map.Getter<object, RenderTexture> ("VisualRenderTexture");
            Map.Destroy = map.Method<Action<object>> ("Destroy");
        }

        /// <summary>
        /// The SCANsat scanner module of a part, or null if it has none.
        /// </summary>
        public static PartModule GetModule (global::Part part)
        {
            if (Module.Scanner == null)
                return null;
            var modules = part.Modules;
            for (int i = 0; i < modules.Count; i++) {
                if (Module.Scanner.IsInstanceOfType (modules [i]))
                    return modules [i];
            }
            return null;
        }

        /// <summary>
        /// The names of the resources SCANsat maps.
        /// </summary>
        public static IList<string> ResourceNames ()
        {
            var resources = Resource.All () as IEnumerable;
            if (resources == null)
                return new List<string> ();
            return resources.Cast<object> ().Select (Resource.Name).ToList ();
        }
    }
}
