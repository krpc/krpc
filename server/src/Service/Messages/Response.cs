using System;

namespace KRPC.Service.Messages
{
    #pragma warning disable 1591
    public class Response : IMessage
    {
        public double Time { get; set; }

        public bool HasReturnValue { get; private set; }

        public object ReturnValue {
            get { return returnValue; }
            set {
                returnValue = value;
                HasReturnValue = true;
            }
        }

        public bool HasError { get; private set; }

        public string Error {
            get { return error; }
            set {
                error = value;
                HasError = true;
            }
        }

        object returnValue;

        string error;

        public Response ()
        {
        }
    }
}
