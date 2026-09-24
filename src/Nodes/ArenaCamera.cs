using Godot;

namespace ByteBrawl.Nodes;

public partial class ArenaCamera : Camera2D
{
    public Fighter? P1;
    public Fighter? P2;

    public override void _Process(double delta)
    {
        if (P1 == null || P2 == null) return;
        var mid = (P1.Position + P2.Position) / 2;
        var dist = (P1.Position - P2.Position).Length();
        var zoom = Mathf.Clamp(160f / Mathf.Max(dist, 60f), 0.6f, 1.5f);
        Position = mid;
        Zoom = new Vector2(zoom, zoom);
    }
}
