using System.Diagnostics.CodeAnalysis;
using KRPC.SpaceCenter.ExtensionMethods;

namespace KRPC.SpaceCenter.AutoPilot
{
    /// <summary>
    /// Robust, 3-parameter, proportional-integral-derivative controller
    /// http://brettbeauregard.com/blog/2011/04/improving-the-beginners-pid-introduction/
    /// </summary>
    [SuppressMessage ("Gendarme.Rules.Maintainability", "AvoidLackOfCohesionOfMethodsRule")]
    sealed class PIDController
    {
        public double Kp { get; private set; }

        public double Ki { get; private set; }

        public double Kd { get; private set; }

        public double OutputMin { get; private set; }

        public double OutputMax { get; private set; }

        double lastInput;
        double integralTerm;

        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidLongParameterListsRule")]
        public PIDController (double input, double kp = 1, double ki = 0, double kd = 0, double outputMin = -1, double outputMax = 1)
        {
            Reset (input, kp, ki, kd, outputMin, outputMax);
        }

        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidLongParameterListsRule")]
        public void Reset (double input, double kp = 1, double ki = 0, double kd = 0, double outputMin = -1, double outputMax = 1)
        {
            integralTerm = 0;
            lastInput = input;
            SetParameters (kp, ki, kd, outputMin, outputMax);
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
            integralTerm += Ki * error * deltaTime;
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
