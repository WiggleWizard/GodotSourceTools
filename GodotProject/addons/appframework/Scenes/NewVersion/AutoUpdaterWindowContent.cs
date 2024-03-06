using GodotAppFramework.Serializers.Github;

using Godot;

namespace GodotAppFramework;

public partial class AutoUpdaterWindowContent : Control
{
    public void InternalInitialize(AppVersionInfo releaseInfo)
    {
        Initialize(releaseInfo);
    }
    
    public virtual void Initialize(AppVersionInfo versionInfo) {}
}