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
            var blink = Time.GetTicksMsec() % 266 < 133;
            foreach (var f in new[] { P1, P2 })
            {
                if (f == null) continue;
                var scale = Mathf.Abs(f.Rig.Scale.X);
                var fighterInvincible = f.InvincibleFrames > 0;
                foreach (var hurtbox in f.Rig.Hurtboxes)
                {
                    var type = fighterInvincible ? HurtboxType.Invincible : hurtbox.CurrentType;
                    var (fill, outline) = HurtboxStyle(type, blink);
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
            DrawCircle(hb.Position, hb.ResolvedRadius, HitboxFill(hb));
            if (hb.Spec.Type == HitboxType.Damage)
            {
                // Knockback direction: thin white line inside the circle (framedata style).
                var dir = hb.Attack.Direction * hb.Attacker.Facing * hb.ResolvedRadius;
                DrawLine(hb.Position, hb.Position + dir, new Color(1, 1, 1, 0.9f), 1f);
            }
        }
        foreach (var (pos, frames) in Hitboxes.BlockedFlashes)
        {
            var alpha = frames / 20f;
            DrawCircle(pos, 14f, new Color(1, 1, 1, alpha));
            DrawArc(pos, 14f, 0, Mathf.Tau, 24, new Color(1, 1, 1, alpha * 2), 1.5f);
        }
    }

    // Smash-convention colors per spec; near-transparent fill, brighter outline.
    private static (Color Fill, Color Outline) HurtboxStyle(HurtboxType type, bool blink) => type switch
    {
        HurtboxType.Intangible => (new Color(0.4f, 0.6f, 1f, 0.10f), new Color(0.4f, 0.6f, 1f, 0.30f)),
        HurtboxType.Invincible => blink
            ? (new Color(1, 1, 1, 0.30f), new Color(1, 1, 1, 0.55f))
            : (new Color(1, 1, 1, 0.06f), new Color(1, 1, 1, 0.15f)),
        HurtboxType.SuperArmor => (new Color(1f, 0.6f, 0.1f, 0.18f), new Color(1f, 0.6f, 0.1f, 0.40f)),
        HurtboxType.HyperArmor => (new Color(0.7f, 0.3f, 1f, 0.18f), new Color(0.7f, 0.3f, 1f, 0.40f)),
        _ => (new Color(0.75f, 1f, 0.85f, 0.15f), new Color(0.75f, 1f, 0.85f, 0.35f)),
    };

    private static Color HitboxFill(Hitbox hb) => hb.Spec.Type switch
    {
        HitboxType.Wind => new Color(0.3f, 0.9f, 1f, 0.35f),
        HitboxType.Grab => new Color(1f, 0.9f, 0.2f, 0.35f),
        HitboxType.Search => new Color(0.6f, 0.6f, 0.6f, 0.30f),
        _ => DamageFill(hb),
    };

    // Damage hitboxes fade from bright (early active frames) to dark.
    private static Color DamageFill(Hitbox hb)
    {
        var age = 1f - hb.FramesRemaining / (float)Math.Max(1, hb.Attack.ActiveFrames);
        return new Color(1, 0, 0, 0.55f - 0.3f * age);
    }
}
