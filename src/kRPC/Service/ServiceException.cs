using System;

namespace KRPC.Service
{
    public class ServiceException : Exception
    {
        public ServiceException (string message) :
            base (message)
        {
        }
    }
}

