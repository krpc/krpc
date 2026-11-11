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
    static class API
    {
        const string assemblyName = "SCANsat";

        static readonly List<string> missing = new List<string> ();

        public static bool IsAvailable { get; private set; }

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

        public static void Load ()
        {
            IsAvailable = false;
            var loaded = AssemblyLoader.loadedAssemblies.FirstOrDefault (a => a.assembly.GetName ().Name == assemblyName);
            if (loaded == null) {
                Logger.WriteLine ("Load API: " + assemblyName + " not found; skipping");
                return;
            }
            var assembly = loaded.assembly;
            missing.Clear ();

            var util = assembly.GetType ("SCANsat.SCANUtil");
            IsCovered = Method<Func<double, double, global::CelestialBody, int, bool>> (util, "isCovered");
            GetCoverage = Method<Func<int, global::CelestialBody, double>> (util, "GetCoverage");
            GetData = Method<Func<global::CelestialBody, object>> (util, "getData");
            GetElevation = Method<Func<global::CelestialBody, double, double, double>> (util, "getElevation");
            GetSlope = Method<Func<double, global::CelestialBody, double, double, double, double>> (util, "slope");
            GetBiomeName = Method<Func<global::CelestialBody, double, double, string>> (util, "getBiomeName");
            ResourceOverlay = Method<Func<double, double, string, global::CelestialBody, bool, float>> (util, "ResourceOverlay");

            Settings.InstantResourceScan = Method<Func<bool>> (util, "instantResourceScanEnabled");
            Settings.StockResourceScan = Method<Func<bool>> (util, "stockResourceScanEnabled");
            Settings.BiomeLock = Method<Func<bool>> (util, "resourceBiomeLockEnabled");
            Settings.RequireNarrowBand = Method<Func<bool>> (util, "narrowBandResourceRestrictionEnabled");
            Settings.GlobalResource = Static<bool> (assembly.GetType ("SCANsat.SCANconfigLoader"), "GlobalResource");
            var settings = assembly.GetType ("SCANsat.SCAN_Settings_Config");
            Settings.Instance = Static<object> (settings, "Instance");
            Settings.BackgroundScanning = Getter<bool> (settings, "BackgroundScanning");
            Settings.DaylightCheck = Getter<bool> (settings, "DaylightCheck");

            var controller = assembly.GetType ("SCANsat.SCANcontroller");
            var resource = assembly.GetType ("SCANsat.SCAN_Data.SCANresourceGlobal");
            Resource.All = Method<Func<object>> (controller, "resources");
            Resource.Name = Getter<string> (resource, "Name");
            Resource.Get = Method<Func<string, bool, object>> (controller, "getResourceNode");
            Resource.CurrentBodyConfig = Method<Action<object, string>> (resource, "CurrentBodyConfig");

            var data = assembly.GetType ("SCANsat.SCAN_Data.SCANdata");
            var anomaly = assembly.GetType ("SCANsat.SCAN_Data.SCANanomaly");
            Anomaly.All = Getter<Array> (data, "Anomalies");
            Anomaly.Known = Getter<bool> (anomaly, "Known");
            Anomaly.Detail = Getter<bool> (anomaly, "Detail");
            Anomaly.Name = Getter<string> (anomaly, "Name");
            Anomaly.Latitude = Getter<double> (anomaly, "Latitude");
            Anomaly.Longitude = Getter<double> (anomaly, "Longitude");

            var sensor = assembly.GetType ("SCANsat.SCANcontroller+SCANsensor");
            Sensor.Controller = Static<object> (controller, "controller");
            Sensor.Status = Method<Func<object, global::Vessel, int, object>> (controller, "getSensorStatus");
            Sensor.InRange = Getter<bool> (sensor, "inRange");
            Sensor.BestRange = Getter<bool> (sensor, "bestRange");
            Sensor.InDarkness = Getter<bool> (sensor, "inDarkness");

            var scanner = assembly.GetType ("SCANsat.SCAN_PartModules.SCANsat");
            Module.Scanner = scanner;
            Module.ResourceScanner = assembly.GetType ("SCANsat.SCAN_PartModules.ModuleSCANresourceScanner");
            if (scanner == null || Module.ResourceScanner == null)
                missing.Add ("part modules");
            Module.SensorType = ModuleGetter<int> (scanner, "sensorType");
            Module.FieldOfView = ModuleGetter<float> (scanner, "fov");
            Module.MinAltitude = ModuleGetter<float> (scanner, "min_alt");
            Module.MaxAltitude = ModuleGetter<float> (scanner, "max_alt");
            Module.BestAltitude = ModuleGetter<float> (scanner, "best_alt");
            Module.RequireLight = ModuleGetter<bool> (scanner, "requireLight");
            Module.Scanning = ModuleGetter<bool> (scanner, "scanningNow");
            Module.StartScan = Method<Action<PartModule>> (scanner, "startScan");
            Module.StopScan = Method<Action<PartModule>> (scanner, "stopScan");
            Module.StartResourceScan = Method<Action<PartModule, KSPActionParam>> (Module.ResourceScanner, "startResourceScanAction");

            var map = assembly.GetType ("SCANsat.SCAN_Map.SCANmap");
            Map.New = map == null ? null : Accessor.Constructor<Func<global::CelestialBody, int, object>> (map);
            if (Map.New == null)
                missing.Add ("SCANmap constructor");
            Map.SetProjection = Method<Action<object, int>> (map, "setProjection");
            Map.SetBody = Method<Action<object, global::CelestialBody>> (map, "setBody");
            Map.SetSize = Method<Action<object, int, int, int, int, int>> (map, "setSize");
            Map.SetColor = Setter<bool> (map, "ColorMap");
            Map.SetResource = Setter<object> (map, "Resource");
            Map.Reset = Method<Action<object, int, bool, bool>> (map, "resetMap");
            Map.Build = Method<Action<object>> (map, "getPartialMap");
            Map.IsComplete = Method<Func<object, bool>> (map, "isMapComplete");
            Map.Texture = Getter<RenderTexture> (map, "VisualRenderTexture");
            Map.Destroy = Method<Action<object>> (map, "Destroy");

            if (missing.Count > 0) {
                Logger.WriteLine ("Load API: " + assemblyName + " members not found: " +
                                  string.Join (", ", missing.ToArray ()) + "; skipping", Logger.Severity.Error);
                return;
            }
            IsAvailable = true;
            Logger.WriteLine ("Load API: Successfully loaded " + assemblyName + " version " + assembly.GetName ().Version);
        }

        static string Name (Type type, string name)
        {
            return (type == null ? "?" : type.Name) + "." + name;
        }

        static TDelegate Method<TDelegate> (Type type, string name) where TDelegate : class
        {
            var method = type == null ? null : Accessor.Method<TDelegate> (type, name);
            if (method == null)
                missing.Add (Name (type, name));
            return method;
        }

        static Func<T> Static<T> (Type type, string name)
        {
            var getter = type == null ? null : Accessor.StaticGetter<T> (type, name);
            if (getter == null)
                missing.Add (Name (type, name));
            return getter;
        }

        static Func<object, T> Getter<T> (Type type, string name)
        {
            var getter = type == null ? null : Accessor.Getter<object, T> (type, name);
            if (getter == null)
                missing.Add (Name (type, name));
            return getter;
        }

        static Func<PartModule, T> ModuleGetter<T> (Type type, string name)
        {
            var getter = type == null ? null : Accessor.Getter<PartModule, T> (type, name);
            if (getter == null)
                missing.Add (Name (type, name));
            return getter;
        }

        static Action<object, T> Setter<T> (Type type, string name)
        {
            var setter = type == null ? null : Accessor.Setter<object, T> (type, name);
            if (setter == null)
                missing.Add (Name (type, name));
            return setter;
        }

        /// <summary>
        /// The SCANsat scanner module of a part, or null if it has none.
        /// </summary>
        public static PartModule GetModule (global::Part part)
        {
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
