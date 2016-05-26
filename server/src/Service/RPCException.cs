using System;
using KRPC.Service.Scanner;

namespace KRPC.Service
{
    class RPCException : Exception
    {
        public static bool VerboseErrors { get; set; }

        readonly string message;

        public RPCException (string message)
        {
            if (VerboseErrors) {
                this.message += " " + GetType () + ": " + message;
            } else {
                this.message = message;
            }
        }

        public RPCException (ProcedureSignature procedure, string message)
        {
            if (VerboseErrors) {
                this.message = "'" + procedure.FullyQualifiedName + "' threw an exception.";
                this.message += " " + GetType () + ": " + message;
            } else {
                this.message = message;
            }
        }

        public RPCException (ProcedureSignature procedure, Exception exception)
        {
            if (VerboseErrors) {
                message = "'" + procedure.FullyQualifiedName + "' threw an exception.";
                message += " " + exception.GetType () + ": " + exception.Message + " " + exception.StackTrace;
            } else {
                message = exception.Message;
            }
        }

        public override string Message {
            get { return message; }
        }
    }
}

