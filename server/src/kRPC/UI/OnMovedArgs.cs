using System;
using UnityEngine;

namespace KRPC.UI
{
    class MovedArgs : EventArgs
    {
        public Rect Position { get; private set; }

        public MovedArgs (Rect position)
        {
            Position = position;
        }
    }
}
