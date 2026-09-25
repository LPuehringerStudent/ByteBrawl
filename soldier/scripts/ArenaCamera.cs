using Godot;
using System;

namespace UnnamedFightingGame;

/// <summary>Bounded, pixel-snapped panning around a dead zone; no texture rescaling.</summary>
public partial class ArenaCamera : Camera2D
{
    public Soldier Target { get; set; } = null!;
    public static readonly Vector2 Home = new(480,270);
    public static readonly Vector2 MinCenter = new(240,90), MaxCenter = new(720,450);
    public Vector2 Goal { get; private set; } = Home;
    private Vector2 _center = Home;

    public override void _Ready()
    {
        Zoom=Vector2.One;PositionSmoothingEnabled=false;ProcessPriority=10;
        Target.Respawned+=SnapHome;
        SnapHome();
    }

    public void SnapHome()
    {
        Goal=_center=Home;GlobalPosition=Home;ForceUpdateScroll();
    }

    public override void _Process(double delta)
    {
        Vector2 screen=Target.GlobalPosition+new Vector2(0,-54)-Goal+Home;
        Vector2 shift=new(screen.X<280?screen.X-280:screen.X>680?screen.X-680:0,
            screen.Y<150?screen.Y-150:screen.Y>390?screen.Y-390:0);
        Goal=(Goal+shift).Clamp(MinCenter,MaxCenter);
        _center=_center.Lerp(Goal,1-Mathf.Exp(-6*(float)delta));
        GlobalPosition=_center.Round();
        ForceUpdateScroll();
    }

    public override void _ExitTree() { if(Target!=null)Target.Respawned-=SnapHome; }
}
