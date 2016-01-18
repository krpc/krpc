using System;

namespace KRPC.Service
{
    sealed class ServiceException : Exception
    {
        public System.Reflection.Assembly Assembly { get; private set; }

        internal ServiceException (string message) :
            base (message)
        {
            Assembly = Service.Scanner.Scanner.CurrentAssembly;
        }
    }
}
