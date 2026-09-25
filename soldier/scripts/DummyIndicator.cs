using Godot;

namespace UnnamedFightingGame;

/// <summary>Fixed HUD marker reusing the wooden head when the dummy leaves the camera view.</summary>
public partial class DummyIndicator : Node2D
{
    public TrainingDummy Target { get; set; } = null!;
    public Vector2 ArrowDirection { get; private set; }
    private Label _percentage=null!;

    public override void _Ready()
    {
        ProcessPriority=20;TextureFilter=TextureFilterEnum.Nearest;
        var icon=new Sprite2D { Texture=new AtlasTexture {
            Atlas=GD.Load<Texture2D>("res://assets/training/dummy.png"),Region=new Rect2(27,8,26,24)
        },Centered=false,Position=new(-46,-16) };
        AddChild(icon);
        _percentage=new Label { Position=new(-14,-20),Size=new(68,24),MouseFilter=Control.MouseFilterEnum.Ignore };
        _percentage.AddThemeFontSizeOverride("font_size",17);AddChild(_percentage);
        var title=new Label { Text="DUMMY",Position=new(-14,3),MouseFilter=Control.MouseFilterEnum.Ignore };
        title.AddThemeFontSizeOverride("font_size",10);AddChild(title);
        Visible=false;
    }

    public override void _Process(double delta)
    {
        var transform=Target.GetGlobalTransformWithCanvas();
        var bounds=new Rect2(transform*new Vector2(-40,-116),new Vector2(80,120));
        Visible=!GetViewportRect().Intersects(bounds);
        if(!Visible)return;
        Vector2 target=transform*new Vector2(0,-62);
        Vector2 viewportSize=GetViewportRect().Size;
        Vector2 safeCenter=new(viewportSize.X/2,viewportSize.Y/2+30);
        Vector2 half=new(Mathf.Max(1,viewportSize.X/2-70),Mathf.Max(1,viewportSize.Y/2-100));
        Vector2 direction=target-safeCenter;
        float t=Mathf.Min(half.X/Mathf.Max(Mathf.Abs(direction.X),.001f),half.Y/Mathf.Max(Mathf.Abs(direction.Y),.001f));
        Position=(safeCenter+direction*t).Round();
        ArrowDirection=(target-Position).Normalized();
        _percentage.Text=$"{Target.Damage.Percentage:0}%";
        _percentage.AddThemeColorOverride("font_color",DamageState.Tint(Target.Damage.Percentage));
        QueueRedraw();
    }

    public override void _Draw()
    {
        DrawRect(new Rect2(-56,-22,112,48),new Color("211b20"));
        DrawRect(new Rect2(-55,-21,110,46),new Color("49302a"),false);
        var d=ArrowDirection;
        float reach=Mathf.Min(62/Mathf.Max(Mathf.Abs(d.X),.001f),30/Mathf.Max(Mathf.Abs(d.Y),.001f));
        Vector2 tip=(d*reach).Round(),back=tip-d*9,side=new(-d.Y*5,d.X*5);
        DrawColoredPolygon(new[]{tip,(back+side).Round(),(back-side).Round()},new Color("e3b97a"));
    }
}
