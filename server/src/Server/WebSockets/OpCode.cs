using System;
using System.Diagnostics.CodeAnalysis;

namespace KRPC.Server.WebSockets
{
    [Serializable]
    [SuppressMessage ("Gendarme.Rules.Design", "UseFlagsAttributeRule")]
    enum OpCode
    {
        Continue = 0x0,
        Text = 0x1,
        Binary = 0x2,
        Close = 0x8,
        Ping = 0x9,
        Pong = 0xA
    }
}
