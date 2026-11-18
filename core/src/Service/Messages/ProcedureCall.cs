using System.Collections.Generic;
using KRPC.Service.Scanner;

namespace KRPC.Service.Messages
{
    #pragma warning disable 1591
    public class ProcedureCall : IMessage
    {
        public string Service { get; private set; }

        public uint ServiceId { get; private set; }

        public string Procedure { get; private set; }

        public uint ProcedureId { get; private set; }

        public IList<Argument> Arguments { get; private set; }

        internal ProcedureSignature CachedSignature { get; set; }

        /// <summary>
        /// The exception from decoding the arguments, or null if they decoded.
        /// </summary>
        internal System.Exception DecodeException { get; set; }

        /// <summary>
        /// The call as the client sent it, while an argument is left to decode on the game
        /// thread, or null once every argument is decoded.
        /// </summary>
        internal Schema.KRPC.ProcedureCall Encoded { get; set; }

        /// <summary>
        /// Whether decoding the call off the game thread failed, so the whole call decodes on
        /// the game thread, where the failure is reported.
        /// </summary>
        internal bool DecodeFailed { get; set; }

        public ProcedureCall (string service, string procedure)
        {
            Service = service;
            ServiceId = 0;
            Procedure = procedure;
            ProcedureId = 0;
            Arguments = new List<Argument> ();
        }

        public ProcedureCall (string service, uint serviceId, string procedure, uint procedureId)
        {
            Service = service;
            ServiceId = serviceId;
            Procedure = procedure;
            ProcedureId = procedureId;
            Arguments = new List<Argument> ();
        }
    }
}
