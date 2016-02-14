using KRPC.Client;
using KRPC.Client.Services.SpaceCenter;
using System;
using System.Collections.Generic;
using System.Net;

class AttachmentModes
{
    public static void Main ()
    {
        var connection = new KRPC.Client.Connection ();
        var vessel = connection.SpaceCenter ().ActiveVessel;
        var root = vessel.Parts.Root;
        var stack = new Stack<KRPC.Client.Tuple<Part,int>> ();
        stack.Push (new KRPC.Client.Tuple<Part,int> (root, 0));
        while (stack.Count > 0) {
            var item = stack.Pop ();
            Part part = item.Item1;
            int depth = item.Item2;
            Console.WriteLine (new String (' ', depth) + part.Title);
            foreach (var child in part.Children)
                stack.Push (new KRPC.Client.Tuple<Part,int> (child, depth + 1));
        }
    }
}
