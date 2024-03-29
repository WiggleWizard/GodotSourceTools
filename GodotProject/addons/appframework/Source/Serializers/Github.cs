﻿using GodotAppFramework.Extensions;

using Godot;

using System;
using System.Collections.Generic;

namespace GodotAppFramework.Serializers.Github;

public partial class JsonGithubReleaseEntryAsset : Resource
{
    public string Name { get; set; } = "";
    public string Browser_Download_Url { get; set; } = "";
    public int Size { get; set; }
}

public partial class JsonGithubReleaseEntry : Resource
{
    public string Name { get; set; } = "";
    public string Tag_Name { get; set; } = "";
    public string ZipBall_Url { get; set; } = "";
    public bool Prerelease { get; set; } = false;
    public DateTime Created_At { get; set; } = DateTime.MinValue;
    public DateTime Published_At { get; set; } = DateTime.MinValue;
    public string Body { get; set; } = "";
    public string Html_Url { get; set; } = "";
    public List<JsonGithubReleaseEntryAsset> Assets { get; set; } = new();

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
    public string Name { set; get; } = "";
    public string Type { set; get; } = "";
}