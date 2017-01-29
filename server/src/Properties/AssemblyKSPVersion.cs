using System;

[AttributeUsage (AttributeTargets.Assembly)]
sealed class AssemblyKSPVersionAttribute : Attribute
{
    public AssemblyKSPVersionAttribute (string version)
    {
        Version = version;
    }

    public string Version { get; private set; }

    public int Major {
        get { return GetVersionPart (0); }
    }

    public int Minor {
        get { return GetVersionPart (1); }
    }

    public int Patch {
        get { return GetVersionPart (2); }
    }

    int GetVersionPart (int part)
    {
        int result;
        int.TryParse (Version.Split ('.') [part], out result);
        return result;
    }
}
