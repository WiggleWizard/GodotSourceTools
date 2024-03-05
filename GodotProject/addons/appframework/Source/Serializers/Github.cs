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
}

internal class RepoFileEntry
{
    public String Name { set; get; }
    public String Type { set; get; }
}