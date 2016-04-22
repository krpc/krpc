namespace KRPC.Service.Messages
{
    #pragma warning disable 1591
    public class Status : IMessage
    {
        public string Version = "";
        public ulong BytesRead;
        public ulong BytesWritten;
        public float BytesReadRate;
        public float BytesWrittenRate;
        public ulong RpcsExecuted;
        public float RpcRate;
        public bool OneRpcPerUpdate;
        public uint MaxTimePerUpdate;
        public bool AdaptiveRateControl;
        public bool BlockingRecv;
        public uint RecvTimeout;
        public float TimePerRpcUpdate;
        public float PollTimePerRpcUpdate;
        public float ExecTimePerRpcUpdate;
        public uint StreamRpcs;
        public ulong StreamRpcsExecuted;
        public float StreamRpcRate;
        public float TimePerStreamUpdate;
    }
}
