using ByteBrawl.Combat;
using Godot;

namespace ByteBrawl.Nodes;

public partial class CombatDebugDraw : Node2D
{
    public Fighter? P1;
    public Fighter? P2;
    public HitboxManager? Hitboxes;
    public bool ShowHitboxes;
    public bool ShowHurtboxes;

    private bool _f3Prev;

    public override void _Process(double delta)
    {
        var f3 = Input.IsPhysicalKeyPressed(Key.F3);
        if (f3 && !_f3Prev)
        {
            // F3 flips everything on/off; the pause menu toggles each layer.
            var any = ShowHitboxes || ShowHurtboxes;
            ShowHitboxes = ShowHurtboxes = !any;
        }
        _f3Prev = f3;
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (ShowHurtboxes)
        {
            foreach (var f in new[] { P1, P2 })
            {
                if (f == null) continue;
                DrawRect(new Rect2(f.Position - new Vector2(6, 10), new Vector2(12, 20)),
                    new Color(0, 1, 0, 0.4f), true);
            }
        }
        if (!ShowHitboxes || Hitboxes == null) return;
        foreach (var child in Hitboxes.GetChildren())
        {
            if (child is not Hitbox hb) continue;
            DrawCircle(hb.Position, hb.Spec.Radius, new Color(1, 0, 0, 0.4f));
            var dir = hb.Attack.Direction * hb.Attacker.Facing * 20f;
            DrawLine(hb.Position, hb.Position + dir, new Color(1, 1, 0), 1f);
        }
    }
}
