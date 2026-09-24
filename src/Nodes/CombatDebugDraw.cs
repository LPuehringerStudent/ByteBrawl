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

    public override void _Ready()
    {
        ZIndex = 100; // always above fighters and stage
    }

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
            var fill = new Color(0.25f, 1f, 0.5f, 0.45f);
            var outline = new Color(0.05f, 0.55f, 0.25f);
            foreach (var f in new[] { P1, P2 })
            {
                if (f == null) continue;
                var scale = Mathf.Abs(f.Rig.Scale.X);
                foreach (var hurtbox in f.Rig.Hurtboxes)
                {
                    // Capsule: thick line along the segment + cap circles, with outline.
                    var t = hurtbox.GlobalTransform;
                    var r = hurtbox.Radius * scale;
                    var half = (hurtbox.Height * 0.5f - hurtbox.Radius) * scale;
                    var a = t * new Vector2(0, -half);
                    var b = t * new Vector2(0, half);
                    DrawLine(a, b, outline, r * 2 + 1.5f);
                    DrawCircle(a, r + 0.75f, outline);
                    DrawCircle(b, r + 0.75f, outline);
                    DrawLine(a, b, fill, r * 2);
                    DrawCircle(a, r, fill);
                    DrawCircle(b, r, fill);
                }
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
