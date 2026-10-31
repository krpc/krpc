using System;
using KRPC.Service.Attributes;

namespace Extending
{
    [KRPCService]
    public static class Aircraft {
        // [docs]
        /// <summary>
        /// The dynamic pressure, in pascals. It is equal to <math>\frac{1}{2} \rho v^2</math>,
        /// where <math>\rho</math> is the air density and <math>v</math> the airspeed in
        /// <unit>m/s</unit>.
        /// </summary>
        [KRPCProperty]
        public static double DynamicPressure
        {
            get { throw new NotImplementedException (); }
        }
        // [/docs]
    }
}
