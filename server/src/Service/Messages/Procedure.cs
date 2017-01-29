using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace KRPC.Service.Messages
{
    #pragma warning disable 1591
    public class Procedure : IMessage
    {
        public string Name { get; private set; }

        public IList<Parameter> Parameters { get; private set; }

        public bool HasReturnType { get; private set; }

        [SuppressMessage ("Gendarme.Rules.Exceptions", "InstantiateArgumentExceptionCorrectlyRule")]
        public string ReturnType {
            get { return returnType; }
            set {
                if (value == null)
                    throw new ArgumentNullException (nameof (value));
                returnType = value;
                HasReturnType = value.Length > 0;
            }
        }

        public IList<string> Attributes { get; private set; }

        public string Documentation { get; set; }

        string returnType;

        public Procedure (string name)
        {
            Name = name;
            Parameters = new List<Parameter> ();
            ReturnType = string.Empty;
            Attributes = new List<string> ();
            Documentation = string.Empty;
        }
    }
}
