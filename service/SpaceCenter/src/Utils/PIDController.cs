using KRPC.SpaceCenter.ExtensionMethods;
using UnityEngine;

namespace KRPC.SpaceCenter.Utils
{
    /// <summary>
    /// Robust, 3-parameter, proportional-integral-derivative controller
    /// http://brettbeauregard.com/blog/2011/04/improving-the-beginners-pid-introduction/
    /// </summary>
    class PIDController
    {
        float kp;
        float ki;
        float kd;
        Vector3 ti;
        Vector3 lastPosition;

        public PIDController ()
        {
            ti = Vector3.zero;
            lastPosition = Vector3.zero;
            SetParameters ();
        }

        public void SetParameters (float kp = 1, float ki = 0, float kd = 0, float dt = 1)
        {
            this.kp = kp;
            this.ki = ki * dt;
            this.kd = kd / dt;
        }

        public Vector3 Update (Vector3 error, Vector3 position, float minOutput, float maxOutput)
        {
            ti += ki * error;
            ti = ti.Clamp (minOutput, maxOutput);
            var dInput = position - lastPosition;
            var output = kp * error + ti - kd * dInput;
            output = output.Clamp (minOutput, maxOutput);
            lastPosition = position;
            return output;
        }
    }
}
