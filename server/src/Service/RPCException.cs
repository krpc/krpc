using System;
using System.Diagnostics.CodeAnalysis;
using KRPC.Service.Scanner;

namespace KRPC.Service
{
    sealed class RPCException : Exception
    {
        public RPCException ()
        {
        }

        public RPCException (string message)
        : base(message)
        {
        }

        public RPCException (Exception innerException)
        : base(String.Empty, innerException)
        {
        }

        public RPCException (string message, Exception innerException)
        : base(message, innerException)
        {
        }
    }
}
