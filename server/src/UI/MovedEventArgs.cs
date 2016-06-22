using System;
using UnityEngine;

namespace KRPC.UI
{
    sealed class MovedEventArgs : EventArgs
    {
        public Rect Position { get; private set; }

        public MovedEventArgs (Rect position)
        {
            Position = position;
        }
    }
}
