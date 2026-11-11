using System;

namespace KRPC.MechJeb
{
    /// <summary>
    /// The vessel state of the active vessel's MechJeb.
    /// </summary>
    static class VesselState
    {
        static Func<object, object> instance;
        static Value<double> time;
        static Value<double> celestialLongitude;
        static Value<double> latitude;
        static Value<double> longitude;

        internal static void Load (Type core)
        {
            instance = Reflection.Field<object> (core, "VesselState").Get;
            var type = Reflection.Type ("MuMech.VesselState");
            time = Reflection.Field<double> (type, "time");
            // Moving averages, which convert to their value
            celestialLongitude = Reflection.Field<double> (type, "celestialLongitude");
            latitude = Reflection.Field<double> (type, "latitude");
            longitude = Reflection.Field<double> (type, "longitude");
        }

        static object Instance {
            get { return instance (MechJeb.Core); }
        }

        public static double Time {
            get { return time.Get (Instance); }
        }

        public static double CelestialLongitude {
            get { return celestialLongitude.Get (Instance); }
        }

        public static double Latitude {
            get { return latitude.Get (Instance); }
        }

        public static double Longitude {
            get { return longitude.Get (Instance); }
        }
    }
}
