using System;
using System.Diagnostics;

namespace KRPC.Utils
{
    sealed class ExponentialMovingAverage
    {
        double value;
        readonly double w;

        readonly Stopwatch timer;
        long lastUpdate;

        public ExponentialMovingAverage (double meanLifetime = 1d)
        {
            value = 0d;
            w = meanLifetime;
            timer = Stopwatch.StartNew ();
            lastUpdate = timer.ElapsedTicks;
        }

        public float Update (float newValue)
        {
            var time = timer.ElapsedTicks;
            var timeDiff = (time - lastUpdate) / (double)Stopwatch.Frequency;
            lastUpdate = time;
            var alpha = 1d - Math.Exp (-timeDiff / w);
            value = alpha * newValue + (1d - alpha) * value;
            return (float)value;
        }

        public float Value {
            get { return (float)value; }
        }
    }
}
