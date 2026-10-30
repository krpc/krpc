namespace KRPC.Test
{
    /// <summary>
    /// Pins the time an update may spend executing calls, for a test that counts the updates its
    /// calls take. Adaptive rate control otherwise lowers the limit after each slow update, so
    /// on a loaded machine a call can be left to the next update.
    /// </summary>
    sealed class PinnedUpdateTime
    {
        readonly uint maxTimePerUpdate;
        readonly bool adaptiveRateControl;

        public PinnedUpdateTime ()
        {
            var config = Configuration.Instance;
            maxTimePerUpdate = config.MaxTimePerUpdate;
            adaptiveRateControl = config.AdaptiveRateControl;
            config.AdaptiveRateControl = false;
            // In microseconds
            config.MaxTimePerUpdate = 10000000;
        }

        public void Restore ()
        {
            var config = Configuration.Instance;
            config.MaxTimePerUpdate = maxTimePerUpdate;
            config.AdaptiveRateControl = adaptiveRateControl;
        }
    }
}
