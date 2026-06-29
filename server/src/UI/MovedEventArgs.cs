#pragma warning disable 1591

using System;
using UnityEngine;

namespace KRPC.UI
{
    public sealed class MovedEventArgs : EventArgs
    {
        public Rect Position { get; private set; }

        public MovedEventArgs (Rect position)
        {
            Position = position;
        }
    }
}
