using KRPCSpaceCenter.ExtensionMethods;
using UnityEngine;

namespace KRPCSpaceCenter.Utils
{
    /// <summary>
    /// Robust, 3-parameter, proportional-integral-derivative controller
    /// http://brettbeauregard.com/blog/2011/04/improving-the-beginners-pid-introduction/
    /// </summary>
    class PIDController
    {
        float Kp;
        float Ki;
        float Kd;
        Vector3 Ti;
        Vector3 lastPosition;

        public PIDController ()
        {
            Ti = Vector3.zero;
            lastPosition = Vector3.zero;
            SetParameters ();
        }

        public void SetParameters (float Kp = 1, float Ki = 0, float Kd = 0, float dt = 1)
        {
            this.Kp = Kp;
            this.Ki = Ki * dt;
            this.Kd = Kd / dt;
        }

        public Vector3 Update (Vector3 error, Vector3 position, float minOutput, float maxOutput)
        {
            Ti += Ki * error;
            Ti = Ti.Clamp (minOutput, maxOutput);
            var dInput = position - lastPosition;
            var output = Kp * error + Ti - Kd * dInput;
            output = output.Clamp (minOutput, maxOutput);
            lastPosition = position;
            return output;
        }
    }
}
