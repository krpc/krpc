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
}
