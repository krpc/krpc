using System;
using System.Collections.Generic;

namespace KRPC.Service
{
    /// <summary>
    /// Use to invoke the method that implement an RPC
    /// </summary>
    interface IProcedureHandler
    {
        object Invoke (params object[] parameters);

        IEnumerable<ProcedureParameter> Parameters { get; }

        Type ReturnType { get; }
    }
}

