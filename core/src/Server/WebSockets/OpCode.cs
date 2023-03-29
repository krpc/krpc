using System;

namespace KRPC.Server.WebSockets
{
    [Serializable]
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
