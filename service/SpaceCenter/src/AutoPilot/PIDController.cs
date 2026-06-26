using KRPC.SpaceCenter.ExtensionMethods;

namespace KRPC.SpaceCenter.AutoPilot
{
    /// <summary>
    /// Robust, 3-parameter, proportional-integral-derivative controller
    /// https://brettbeauregard.com/blog/2011/04/improving-the-beginners-pid-introduction/
    /// </summary>
    sealed class PIDController
    {
        public double Kp { get; private set; }

        public double Ki { get; private set; }

        public double Kd { get; private set; }

        public double OutputMin { get; private set; }

        public double OutputMax { get; private set; }

        double lastInput;
        double integralTerm;

        public PIDController (double kp = 1, double ki = 0, double kd = 0, double outputMin = -1, double outputMax = 1)
        {
            Reset (kp, ki, kd, outputMin, outputMax);
        }

        public void Reset (double kp = 1, double ki = 0, double kd = 0, double outputMin = -1, double outputMax = 1)
        {
            ResetState ();
            SetParameters (kp, ki, kd, outputMin, outputMax);
        }

        /// <summary>
        /// Clear the dynamic state (integral term and derivative history) without
        /// touching the gains. Used when (re-)engaging the autopilot so that manually
        /// set gains survive an engage when auto-tuning is off.
        /// </summary>
        public void ResetState ()
        {
            integralTerm = 0;
            lastInput = 0;
        }

        public void SetParameters (double kp, double ki, double kd, double outputMin = -1, double outputMax = 1)
        {
            Kp = kp;
            Ki = ki;
            Kd = kd;
            OutputMin = outputMin;
            OutputMax = outputMax;
            integralTerm = integralTerm.Clamp (outputMin, outputMax);
        }

        public double Update (double setpoint, double input, double deltaTime)
        {
            var error = setpoint - input;
            var deltaIntegral = Ki * error * deltaTime;
            if (!((integralTerm >= OutputMax && deltaIntegral > 0) || (integralTerm <= OutputMin && deltaIntegral < 0)))
                integralTerm += deltaIntegral;
            integralTerm = integralTerm.Clamp (OutputMin, OutputMax);
            var derivativeInput = (input - lastInput) / deltaTime;
            var output = Kp * error + integralTerm - Kd * derivativeInput;
            output = output.Clamp (OutputMin, OutputMax);
            lastInput = input;
            return output;
        }

        public void ClearIntegralTerm ()
        {
            integralTerm = 0;
        }
    }
}
