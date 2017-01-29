using System;

namespace KRPC.Service
{
    class ServiceException : Exception
    {
        public System.Reflection.Assembly Assembly { get; private set; }

        public ServiceException ()
        {
            Assembly = Scanner.Scanner.CurrentAssembly;
        }

        public ServiceException (string message) : base (message)
        {
            Assembly = Scanner.Scanner.CurrentAssembly;
        }

        public ServiceException (string message, Exception innerException) : base (message, innerException)
        {
            Assembly = Scanner.Scanner.CurrentAssembly;
        }
    }
}
