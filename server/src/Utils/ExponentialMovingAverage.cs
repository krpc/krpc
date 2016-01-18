using System;
using System.Diagnostics;

namespace KRPC.Utils
{
    public class ExponentialMovingAverage
    {
        double value;
        readonly double W;

        readonly Stopwatch timer;
        long lastUpdate;

        public ExponentialMovingAverage (double W = 1d)
        {
            value = 0d;
            this.W = W;
            timer = Stopwatch.StartNew ();
            lastUpdate = timer.ElapsedTicks;
        }

        public float Update (float newValue)
        {
            var time = timer.ElapsedTicks;
            var timeDiff = (time - lastUpdate) / (double)Stopwatch.Frequency;
            lastUpdate = time;
            var alpha = 1d - Math.Exp (-timeDiff / W);
            value = alpha * newValue + (1d - alpha) * value;
            return (float)value;
        }

        public float Value {
            get { return (float)value; }
        }
    }
}
