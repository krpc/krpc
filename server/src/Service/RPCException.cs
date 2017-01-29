using System;
using System.Diagnostics.CodeAnalysis;
using KRPC.Service.Scanner;

namespace KRPC.Service
{
    sealed class RPCException : Exception
    {
        public static bool VerboseErrors { get; set; }

        readonly string description;

        public RPCException ()
        {
            description = string.Empty;
        }

        public RPCException (string message)
        {
            if (VerboseErrors) {
                description += " " + GetType () + ": " + message;
            } else {
                description = message;
            }
        }

        public RPCException (string message, Exception innerException)
        {
            var innerMessage = innerException.Message;
            if (VerboseErrors) {
                description += " " + GetType () + ": " + message + innerMessage + " " + innerException.StackTrace;
            } else {
                description = message + innerMessage;
            }
        }

        [SuppressMessage ("Gendarme.Rules.Portability", "NewLineLiteralRule")]
        public RPCException (ProcedureSignature procedure, string message)
        {
            if (VerboseErrors) {
                description = "'" + procedure.FullyQualifiedName + "' threw an exception.\n";
                description += GetType () + ": " + message;
            } else {
                description = message;
            }
        }

        [SuppressMessage ("Gendarme.Rules.Portability", "NewLineLiteralRule")]
        public RPCException (ProcedureSignature procedure, Exception innerException)
        {
            var innerMessage = innerException.Message;
            if (VerboseErrors) {
                description = "'" + procedure.FullyQualifiedName + "' threw an exception.\n";
                description += innerException.GetType () + ": " + innerMessage + "\n" + innerException.StackTrace;
            } else {
                description = innerMessage;
            }
        }

        public override string Message {
            get { return description; }
        }
    }
}
