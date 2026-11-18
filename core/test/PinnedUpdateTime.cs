namespace KRPC.Test
{
    /// <summary>
    /// Pins the time an update may spend executing calls, for a test that counts the updates its
    /// calls take. Adaptive rate control otherwise lowers the budget after slow frames, so
    /// on a loaded machine a call can be left to the next update.
    /// </summary>
    sealed class PinnedUpdateTime
    {
        readonly uint maxTimePerFrame;
        readonly bool adaptiveRateControl;

        public PinnedUpdateTime ()
        {
            var config = Configuration.Instance;
            maxTimePerFrame = config.MaxTimePerFrame;
            adaptiveRateControl = config.AdaptiveRateControl;
            config.AdaptiveRateControl = false;
            // In microseconds, the most allowed
            config.MaxTimePerFrame = 1000000;
        }

        public void Restore ()
        {
            var config = Configuration.Instance;
            config.MaxTimePerFrame = maxTimePerFrame;
            config.AdaptiveRateControl = adaptiveRateControl;
        }
    }
}
