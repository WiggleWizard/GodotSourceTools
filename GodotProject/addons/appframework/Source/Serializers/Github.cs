using Godot;

using System;

namespace GodotAppFramework.Serializers.Github;

public partial class JsonGithubReleaseEntry : Resource
{
    public string Name { get; set; }
    public string Tag_Name { get; set; }
    public string ZipBall_Url { get; set; }
    public bool Prerelease { get; set; }
    public DateTime Created_At { get; set; }
    public DateTime Published_At { get; set; }
    public string Body { get; set; }
    public string Html_Url { get; set; }

    public string GetVersionStr()
    {
        return Tag_Name;
    }

    public Version GetVersion()
    {
        var versionStr = GetVersionStr();
        return versionStr.ToVersion();
    }

    public AppVersionInfo ToAppVersionInfo()
    {
        AppVersionInfo appVersionInfo = new();
        appVersionInfo.Ver = Tag_Name.ToVersion();
        appVersionInfo.ZipUrl = ZipBall_Url;
        appVersionInfo.Time = Published_At;
        appVersionInfo.ChangeLog = Body;
        appVersionInfo.LinkToDownloadPage = Html_Url;

        return appVersionInfo;
    }
}

internal class RepoFileEntry
{
    public String Name { set; get; }
    public String Type { set; get; }
}