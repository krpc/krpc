using System;

[AttributeUsage (AttributeTargets.Assembly)]
sealed class AssemblyKSPVersionAttribute : Attribute
{
    public AssemblyKSPVersionAttribute (string maxVersion, string minVersion)
    {
        MaxVersion = maxVersion;
        MinVersion = minVersion;
    }

    public string MaxVersion { get; private set; }
    public string MinVersion { get; private set; }

    public int MaxMajor {
        get { return GetVersionPart (MaxVersion, 0); }
    }

    public int MaxMinor {
        get { return GetVersionPart (MaxVersion, 1); }
    }

    public int MaxPatch {
        get { return GetVersionPart (MaxVersion, 2); }
    }

    public int MinMajor {
        get { return GetVersionPart (MinVersion, 0); }
    }

    public int MinMinor {
        get { return GetVersionPart (MinVersion, 1); }
    }

    public int MinPatch {
        get { return GetVersionPart (MinVersion, 2); }
    }

    static int GetVersionPart (string version, int part)
    {
        int result;
        int.TryParse (version.Split ('.') [part], out result);
        return result;
    }
}
