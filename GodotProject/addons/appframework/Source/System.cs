using System;

namespace GodotAppFramework.System;

public enum BetterPlatformID
{
    Unknown = 0,
    Windows,
    Linux,
    OSX
}

public static class Platform
{
    public static BetterPlatformID GetBetterPlatformID(this OperatingSystem os)
    {
        BetterPlatformID platformId = BetterPlatformID.Unknown;

        switch (os.Platform)
        {
            case PlatformID.Win32NT:
            case PlatformID.Win32S:
            case PlatformID.Win32Windows:
                platformId = BetterPlatformID.Windows; break;
            case PlatformID.Unix:
                platformId = BetterPlatformID.Linux; break;
            case PlatformID.MacOSX:
                platformId = BetterPlatformID.OSX;
                break;
        }

        return platformId;
    }
}