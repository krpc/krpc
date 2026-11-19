using System;
using System.Collections.Generic;
using Brutal.Numerics;
using KRPC.KSA.Utils;
using KRPC.Service.Attributes;
using KRPC.Utils;
using KSA;

namespace KRPC.KSA.Services
{
    /// <summary>
    /// A coronal mass ejection: a cloud of plasma thrown out by a star. One that reaches a body
    /// with a magnetosphere can cause a geomagnetic storm. See
    /// <see cref="Body.RecentCoronalMassEjections"/> and
    /// <see cref="Flight.IncomingCoronalMassEjections"/>.
    /// </summary>
    [KRPCClass (Service = "KSA")]
    public class CoronalMassEjection : Equatable<CoronalMassEjection>, IGameObjectState
    {
        // The time a recent ejection stays in Body.RecentCoronalMassEjections, as the game's space
        // weather window lists them
        const double RecentSeconds = 3 * 24 * 3600;
        // The smallest shock strength the game's space weather window lists as incoming
        const double IncomingSpike = 2;

        readonly StellarBody star;
        readonly Eruption eruption;

        internal CoronalMassEjection (StellarBody star, Eruption eruption)
        {
            this.star = star ?? throw new ArgumentNullException (nameof (star));
            this.eruption = eruption;
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (CoronalMassEjection other)
        {
            return !ReferenceEquals (other, null) && ReferenceEquals (star, other.star) &&
                eruption.Index == other.eruption.Index;
        }

        /// <summary>
        /// The state of the game object.
        /// </summary>
        public GameObjectState GameObjectState {
            get { return Vehicles.StateOf (star); }
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            return star.GetHashCode () ^ eruption.Index.GetHashCode ();
        }

        /// <summary>
        /// The star that threw out the ejection.
        /// </summary>
        [KRPCProperty]
        public Body Star {
            get { return new Body (star); }
        }

        /// <summary>
        /// The universal time at which the ejection left the star, in seconds.
        /// </summary>
        [KRPCProperty]
        public double UT {
            get { return eruption.Seconds; }
        }

        /// <summary>
        /// The speed of the ejection as it leaves the star, in <unit>m/s</unit>.
        /// </summary>
        [KRPCProperty]
        public float Speed {
            get { return (float)(eruption.SpeedKilometersPerSecond * 1000); }
        }

        /// <summary>
        /// The universal time at which the shock of the ejection is forecast to reach the
        /// vehicle, in seconds. Not a number if the ejection misses the vehicle, or has
        /// already reached it.
        /// </summary>
        /// <param name="vehicle">The vehicle.</param>
        /// <remarks>
        /// The forecast the game's space weather window shows. It is uncertain by up to
        /// <see cref="ArrivalUncertainty"/>, which shrinks as the ejection nears.
        /// </remarks>
        [KRPCMethod]
        public double ArrivalTime (Vehicle vehicle)
        {
            if (vehicle == null)
                throw new ArgumentNullException (nameof (vehicle));
            return Forecast (vehicle.InternalVehicle.GetPositionEcl (), out _);
        }

        /// <summary>
        /// The uncertainty of <see cref="ArrivalTime"/>, in seconds. Not a number if the
        /// ejection misses the vehicle, or has already reached it.
        /// </summary>
        /// <param name="vehicle">The vehicle.</param>
        [KRPCMethod]
        public float ArrivalUncertainty (Vehicle vehicle)
        {
            if (vehicle == null)
                throw new ArgumentNullException (nameof (vehicle));
            Forecast (vehicle.InternalVehicle.GetPositionEcl (), out var band);
            return (float)band;
        }

        double Forecast (double3 positionEcl, out double band)
        {
            band = double.NaN;
            var seconds = Universe.GetElapsedSeconds ();
            if (!IsIncoming (star, eruption, positionEcl, seconds, out var threat))
                return double.NaN;
            return SpaceWeatherAlerts.ForecastArrival (
                star.BodyTemplate.SpaceWeather, eruption, threat.ArrivalSeconds, seconds, out band);
        }

        // Whether the ejection is heading for a position, as the game's space weather window
        // lists it: already seen, not yet arrived, and with a strong enough shock
        static bool IsIncoming (StellarBody star, in Eruption eruption, double3 positionEcl, double seconds,
                                out SpaceWeatherAlerts.Threat threat)
        {
            threat = SpaceWeatherAlerts.GetThreat (star, eruption, positionEcl);
            return threat.FlashSeconds <= seconds && threat.ArrivalSeconds > seconds &&
                threat.Spike >= IncomingSpike;
        }

        /// <summary>
        /// The ejections that left a star in the last three days.
        /// </summary>
        internal static IList<CoronalMassEjection> Recent (StellarBody star)
        {
            var result = new List<CoronalMassEjection> ();
            var weather = star.BodyTemplate.SpaceWeather;
            if (weather.Eruptions == null)
                return result;
            var seconds = Universe.GetElapsedSeconds ();
            foreach (var eruption in weather.SignificantEruptions.Update (weather, seconds)) {
                if (eruption.Seconds <= seconds && eruption.Seconds >= seconds - RecentSeconds)
                    result.Add (new CoronalMassEjection (star, eruption));
            }
            return result;
        }

        /// <summary>
        /// The ejections heading for a position, from every star.
        /// </summary>
        internal static IList<CoronalMassEjection> Incoming (double3 positionEcl)
        {
            var result = new List<CoronalMassEjection> ();
            var seconds = Universe.GetElapsedSeconds ();
            foreach (var star in Universe.Stars) {
                var weather = star.BodyTemplate.SpaceWeather;
                var distance = (positionEcl - star.GetPositionEcl ()).Length ();
                if (weather.Eruptions == null || !SpaceWeatherAlerts.IsWithinReach (distance))
                    continue;
                // The oldest ejection still in flight, at half the solar wind speed
                var oldest = seconds - distance / (0.5 * weather.WindSpeedKilometersPerSecond * 1000);
                foreach (var eruption in weather.SignificantEruptions.Update (weather, seconds)) {
                    if (eruption.Seconds <= seconds && eruption.Seconds >= oldest &&
                        IsIncoming (star, eruption, positionEcl, seconds, out _))
                        result.Add (new CoronalMassEjection (star, eruption));
                }
            }
            return result;
        }
    }
}
