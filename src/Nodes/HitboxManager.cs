using ByteBrawl.Combat;
using Godot;

namespace ByteBrawl.Nodes;

public partial class HitboxManager : Node, IHitboxManager
{
    public event Action<Fighter, string>? SearchTriggered;

    // Debug overlay: positions of recently blocked (invincible) contacts.
    public readonly List<(Vector2 Pos, int Frames)> BlockedFlashes = new();

    private MatchRules? _rules;
    public MatchRules? Rules
    {
        get => _rules;
        set
        {
            if (_rules != null) _rules.HitBlocked -= OnHitBlocked;
            _rules = value;
            if (_rules != null) _rules.HitBlocked += OnHitBlocked;
        }
    }

    private void OnHitBlocked(IFighter defender) => BlockedFlashes.Add((defender.Position, 10));

    public void Spawn(IFighter attacker, AttackData attack, HitboxSpec spec)
    {
        if (attacker is not Fighter a || Rules == null) return;
        var shape = new CollisionShape2D { Shape = new CircleShape2D { Radius = spec.Radius } };
        var hb = new Hitbox
        {
            Attacker = a, Spec = spec, FramesRemaining = attack.ActiveFrames,
            Position = a.Position + new Vector2(spec.OffsetX * a.Facing, spec.OffsetY),
            Attack = EffectiveAttack(attack, spec),
        };
        hb.AddChild(shape);
        // Grab boxes are specced but unbuilt: they exist for the debug overlay only.
        if (spec.Type != HitboxType.Grab)
        {
            hb.AreaEntered += area =>
            {
                if (area is not Hurtbox hurt || hurt.OwnerFighter is not { } defender) return;
                if (defender == hb.Attacker) return;
                if (hurt.CurrentType == HurtboxType.Intangible) return;
                switch (hb.Spec.Type)
                {
                    case HitboxType.Wind:
                        Rules.ApplyWind(defender, hb.Spec, hb.Attacker.Facing);
                        break;
                    case HitboxType.Search:
                        if (!hb.HasHit)
                        {
                            SearchTriggered?.Invoke(hb.Attacker, hb.Spec.SearchId);
                            hb.HasHit = true;
                        }
                        break;
                    default:
                        if (hb.HasHit) break;
                        Rules.ApplyHit(hb.Attacker, defender, hb.Attack, hurt.CurrentType, hurt.ArmorBreakKb);
                        hb.HasHit = true;
                        break;
                }
            };
        }
        AddChild(hb);
    }

    // Sweet/sour spot support: a spec may override damage/knockback for its circle.
    private static AttackData EffectiveAttack(AttackData attack, HitboxSpec spec)
    {
        if (spec.DamageOverride is null && spec.KnockbackOverride is null)
            return attack;
        return new AttackData
        {
            Id = attack.Id,
            BaseDamage = spec.DamageOverride ?? attack.BaseDamage,
            BaseKnockback = spec.KnockbackOverride ?? attack.BaseKnockback,
            Scaling = attack.Scaling,
            Direction = attack.Direction,
            HitstunFrames = attack.HitstunFrames,
            ActiveFrames = attack.ActiveFrames,
        };
    }

    public override void _PhysicsProcess(double delta)
    {
        foreach (var child in GetChildren())
        {
            if (child is not Hitbox hb) continue;
            hb.FramesRemaining--;
            if (hb.FramesRemaining <= 0) hb.QueueFree();
        }
        for (var i = BlockedFlashes.Count - 1; i >= 0; i--)
        {
            var flash = BlockedFlashes[i];
            if (--flash.Frames <= 0) BlockedFlashes.RemoveAt(i);
            else BlockedFlashes[i] = flash;
        }
    }
}
