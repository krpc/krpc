using System.Collections.Generic;

namespace KRPC.Service.Messages
{
    #pragma warning disable 1591
    public class Request : IMessage
    {
        public ulong Id { get; set; }

        public IList<ProcedureCall> Calls { get; private set; }

        /// <summary>
        /// The <see cref="System.Diagnostics.Stopwatch.GetTimestamp"/> at which the request's
        /// bytes arrived, or zero if the transport does not record it.
        /// </summary>
        internal long Arrived { get; set; }

        public Request ()
        {
            Calls = new List<ProcedureCall> ();
        }
    }
}
