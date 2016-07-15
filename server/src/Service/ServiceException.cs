using System;

namespace KRPC.Service
{
    class ServiceException : Exception
    {
        public System.Reflection.Assembly Assembly { get; private set; }

        public ServiceException ()
        {
            Assembly = Service.Scanner.Scanner.CurrentAssembly;
        }

        public ServiceException (string message) : base (message)
        {
            Assembly = Service.Scanner.Scanner.CurrentAssembly;
        }

        public ServiceException (string message, Exception innerException) : base (message, innerException)
        {
            Assembly = Service.Scanner.Scanner.CurrentAssembly;
        }
    }
}
