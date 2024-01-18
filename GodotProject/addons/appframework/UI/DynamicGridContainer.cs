using Godot;

namespace GodotAppFramework.UI;

[Tool]
public partial class DynamicGridContainer : GridContainer
{
    public override void _Notification(int what)
    {
        if (what == NotificationSortChildren)
        {
            GD.Print("Hello");
        }
    }
}
