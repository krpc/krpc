using System;
using UnityEngine;

class MovedArgs : EventArgs
{
    public Rect Position { get; private set; }

    public MovedArgs(Rect position)
    {
        Position = position;
    }
}
