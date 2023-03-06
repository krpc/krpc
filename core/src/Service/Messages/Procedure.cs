using System;
using System.Collections.Generic;

namespace KRPC.Service.Messages
{
    #pragma warning disable 1591
    public class Procedure : IMessage
    {
        public string Name { get; private set; }

        public IList<Parameter> Parameters { get; private set; }

        public bool HasReturnType {
            get { return ReturnType != null; }
        }

        public Type ReturnType { get; set; }

        public bool ReturnIsNullable { get; set; }

        public GameScene GameScene { get; set; }

        public string Documentation { get; set; }

        public Procedure (string name)
        {
            Name = name;
            Parameters = new List<Parameter> ();
            Documentation = string.Empty;
        }
    }
}
