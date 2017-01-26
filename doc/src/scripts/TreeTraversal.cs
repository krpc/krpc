using System;
using System.Collections.Generic;
using System.Net;
using KRPC.Client;
using KRPC.Client.Services.SpaceCenter;

class AttachmentModes
{
    public static void Main ()
    {
        var connection = new Connection ();
        var vessel = connection.SpaceCenter ().ActiveVessel;
        var root = vessel.Parts.Root;
        var stack = new Stack<Tuple<Part,int>> ();
        stack.Push (new Tuple<Part,int> (root, 0));
        while (stack.Count > 0) {
            var item = stack.Pop ();
            Part part = item.Item1;
            int depth = item.Item2;
            Console.WriteLine (new String (' ', depth) + part.Title);
            foreach (var child in part.Children)
                stack.Push (new Tuple<Part,int> (child, depth + 1));
        }
    }
}
