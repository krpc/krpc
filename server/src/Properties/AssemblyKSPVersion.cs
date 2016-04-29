using System;

[AttributeUsage (AttributeTargets.Assembly)]
class AssemblyKSPVersionAttribute : Attribute
{
    public AssemblyKSPVersionAttribute (string version)
    {
        Version = version;
    }

    public string Version { get; private set; }

    public int Major {
        get { return Int32.Parse (Version.Split ('.') [0]); }
    }

    public int Minor {
        get { return Int32.Parse (Version.Split ('.') [1]); }
    }

    public int Patch {
        get { return Int32.Parse (Version.Split ('.') [2]); }
    }
}
