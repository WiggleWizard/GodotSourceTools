using Godot;

namespace GodotAppFramework.UI;

[Tool, GlobalClass]
public partial class MarginPanel : Container
{
    private static string _themeType = "MarginPanel";
    
    private bool _respectPanelLayering = true;
    [Export] public bool RespectPanelLayering
    {
        get => _respectPanelLayering;
        set { _respectPanelLayering = value; QueueRedraw(); }
    }

    private struct ThemeCache
    {
        public StyleBox PanelStylebox;
        public Color BoundaryColor;
        public int BoundaryWidth;

        public int MarginTop;
        public int MarginBottom;
        public int MarginLeft;
        public int MarginRight;
    }
    private ThemeCache _themeCache;

    private enum ParentLayoutDirection
    {
        Unknown, LeftToRight, TopToBottom
    }
    
    public override void _Draw()
    {
        Rect2 localRect = GetRect();
        localRect.Position = Vector2.Zero;
        
        _themeCache.PanelStylebox.Draw(GetCanvasItem(), localRect);
        
        Rect2 boundaryRect = localRect;

        // Figure out which side to draw the boundary separator
        Node parent = GetParent();
        ParentLayoutDirection parentLayoutDirection = ParentLayoutDirection.Unknown;
        if (parent is HBoxContainer)
        {
            parentLayoutDirection = ParentLayoutDirection.LeftToRight;
        }
        else if (parent is VBoxContainer)
        {
            parentLayoutDirection = ParentLayoutDirection.TopToBottom;
        }
        
        int indexInParent = GetIndex();
        int visibleControlsInParent = 0;
        foreach (Node child in parent.GetChildren())
        {
            if (child is Control control)
            {
                if (control.Visible)
                {
                    visibleControlsInParent++;
                }
            }
        }

        if (visibleControlsInParent == 1)
        {
            return;
        }
        
        // Draw separator
        if (parentLayoutDirection == ParentLayoutDirection.TopToBottom)
        {
            // Draw bottom boundary
            if (indexInParent == 0)
            {
                boundaryRect = boundaryRect.GrowSide(Side.Top, -localRect.Size.Y + _themeCache.BoundaryWidth);
            }
            // Draw top boundary
            else
            {
                boundaryRect = boundaryRect.GrowSide(Side.Bottom, -localRect.Size.Y + _themeCache.BoundaryWidth);
            }
        }
        else if (parentLayoutDirection == ParentLayoutDirection.LeftToRight)
        {
            // Draw bottom boundary
            if (indexInParent == 0)
            {
                boundaryRect = boundaryRect.GrowSide(Side.Left, -localRect.Size.X + _themeCache.BoundaryWidth);
            }
            // Draw top boundary
            else
            {
                boundaryRect = boundaryRect.GrowSide(Side.Right, -localRect.Size.X + _themeCache.BoundaryWidth);
            }
        }

        if (parentLayoutDirection != ParentLayoutDirection.Unknown)
        {
            DrawRect(boundaryRect, _themeCache.BoundaryColor);
        }
    }

    public override Vector2 _GetMinimumSize()
    {
        Vector2 max = Vector2.Zero;

        for (int i = 0; i < GetChildCount(); i++)
        {
            if (GetChild(i) is Control c)
            {
                if (c.TopLevel || !c.Visible)
                {
                    continue;
                }

                Vector2 s = c.GetCombinedMinimumSize();
                if (s.X > max.X)
                {
                    max.X = s.X;
                }

                if (s.Y > max.Y)
                {
                    max.Y = s.Y;
                }
            }
        }

        max.X += _themeCache.MarginLeft + _themeCache.MarginRight;
        max.Y += _themeCache.MarginTop + _themeCache.MarginBottom;

        return max;
    }

    public override void _Notification(int what)
    {
        if (what == NotificationThemeChanged)
        {
            _themeCache.PanelStylebox = GetThemeStylebox("panel", _themeType);
            _themeCache.BoundaryColor = GetThemeColor("boundary", _themeType);
            _themeCache.BoundaryWidth = GetThemeConstant("boundary_thickness", _themeType);

            _themeCache.MarginTop = GetThemeConstant("margin_top", _themeType);
            _themeCache.MarginBottom = GetThemeConstant("margin_bottom", _themeType);
            _themeCache.MarginLeft = GetThemeConstant("margin_left", _themeType);
            _themeCache.MarginRight = GetThemeConstant("margin_right", _themeType);
            
            UpdateMinimumSize();
        }
        else if (what == NotificationSortChildren)
        {
            Rect2 r = GetRect();

            for (int i = 0; i < GetChildCount(); i++)
            {
                if (GetChild(i) is Control c)
                {
                    if (c.TopLevel)
                    {
                        continue;
                    }

                    float w = r.Size.X - _themeCache.MarginLeft - _themeCache.MarginRight;
                    float h = r.Size.Y - _themeCache.MarginTop - _themeCache.MarginBottom;
                    FitChildInRect(c, new Rect2(_themeCache.MarginLeft, _themeCache.MarginTop, w, h));
                }
            }
        }
    }
}