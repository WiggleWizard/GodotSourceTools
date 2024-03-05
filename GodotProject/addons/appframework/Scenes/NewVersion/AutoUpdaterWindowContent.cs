using GodotAppFramework.Serializers.Github;

using Godot;

namespace GodotAppFramework;

public partial class AutoUpdaterWindowContent : Control
{
    public void InternalInitialize(JsonGithubReleaseEntry releaseInfo)
    {
        Initialize(releaseInfo);
    }
    
    public virtual void Initialize(JsonGithubReleaseEntry releaseInfo) {}
}