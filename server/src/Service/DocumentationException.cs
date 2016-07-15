using System;

namespace KRPC.Service
{
    sealed class DocumentationException : ServiceException
    {
        public DocumentationException ()
        {
        }

        public DocumentationException (string message) :
            base ("Documentation error: " + message)
        {
        }

        public DocumentationException (string message, Exception innerException) :
            base ("Documentation error: " + message, innerException)
        {
        }
    }
}
