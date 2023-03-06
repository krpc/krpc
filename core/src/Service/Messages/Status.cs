namespace KRPC.Service.Messages
{
    #pragma warning disable 1591
    public class Status : IMessage
    {
        public string Version { get; private set; }

        public ulong BytesRead { get; set; }

        public ulong BytesWritten { get; set; }

        public float BytesReadRate { get; set; }

        public float BytesWrittenRate { get; set; }

        public ulong RpcsExecuted { get; set; }

        public float RpcRate { get; set; }

        public bool OneRpcPerUpdate { get; set; }

        public uint MaxTimePerUpdate { get; set; }

        public bool AdaptiveRateControl { get; set; }

        public bool BlockingRecv { get; set; }

        public uint RecvTimeout { get; set; }

        public float TimePerRpcUpdate { get; set; }

        public float PollTimePerRpcUpdate { get; set; }

        public float ExecTimePerRpcUpdate { get; set; }

        public uint StreamRpcs { get; set; }

        public ulong StreamRpcsExecuted { get; set; }

        public float StreamRpcRate { get; set; }

        public float TimePerStreamUpdate { get; set; }

        public Status (string version)
        {
            Version = version;
        }
    }
}
