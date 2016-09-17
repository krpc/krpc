using System.Collections.Generic;

namespace KRPC.Service.Messages
{
    #pragma warning disable 1591
    public class Response : IMessage
    {
        public IList<ProcedureResult> Results { get; private set; }

        public bool HasError { get; private set; }

        public string Error {
            get { return error; }
            set {
                error = value;
                HasError = true;
            }
        }

        string error;

        public Response ()
        {
            Results = new List<ProcedureResult> ();
        }
    }
}
