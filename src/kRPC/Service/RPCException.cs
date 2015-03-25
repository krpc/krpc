using System;
using KRPC.Service.Scanner;

namespace KRPC.Service
{
    class RPCException : Exception
    {
        public static bool VerboseErrors { get; set; }

        public RPCException (string message) :
            base (message)
        {
        }

        public RPCException (ProcedureSignature procedure, Exception exception) :
            base (GenerateMessage (procedure, exception))
        {
        }

        static string GenerateMessage (ProcedureSignature procedure, Exception exception)
        {
            if (VerboseErrors) {
                string message = "'" + procedure.FullyQualifiedName + "' threw an exception.";
                message += " " + exception.GetType () + ": " + exception.Message + "\n";
                message += exception.StackTrace;
                return message;
            } else {
                return exception.Message;
            }
        }
    }
}

