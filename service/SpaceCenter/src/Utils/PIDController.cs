using System;
using System.Diagnostics;
using KRPC.SpaceCenter.ExtensionMethods;

namespace KRPC.SpaceCenter.Utils
{
    /// <summary>
    /// Robust, 3-parameter, proportional-integral-derivative controller
    /// http://brettbeauregard.com/blog/2011/04/improving-the-beginners-pid-introduction/
    /// </summary>
    class PIDController
    {
        public double Kp { get; private set; }

        public double Ki { get; private set; }

        public double Kd { get; private set; }

        public double OutputMin { get; private set; }

        public double OutputMax { get; private set; }

        double lastInput;
        double integralTerm;
        readonly Stopwatch lastUpdate = new Stopwatch ();

        public PIDController (double input, double kp = 1, double ki = 0, double kd = 0, double outputMin = -1, double outputMax = 1)
        {
            Reset (input, kp, ki, kd, outputMin, outputMax);
        }

        public void Reset (double input, double kp = 1, double ki = 0, double kd = 0, double outputMin = -1, double outputMax = 1)
        {
            lastUpdate.Reset ();
            lastUpdate.Start ();
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

        public double Update (double error, double input)
        {
            var timeChange = lastUpdate.ElapsedMilliseconds / 1000d;
            integralTerm += Ki * error * timeChange;
            integralTerm = integralTerm.Clamp (OutputMin, OutputMax);
            var derivativeInput = (input - lastInput) / timeChange;
            var output = Kp * error + integralTerm - Kd * derivativeInput;
            output = output.Clamp (OutputMin, OutputMax);
            lastInput = input;
            lastUpdate.Reset ();
            lastUpdate.Start ();
            return output;
        }

        public void ClearIntegralTerm ()
        {
            integralTerm = 0;
        }
    }
}
