using ByteBrawl.Combat;
using Godot;

namespace ByteBrawl.Nodes;

public partial class HitboxManager : Node, IHitboxManager
{
    public MatchRules? Rules;

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
        hb.AreaEntered += area =>
        {
            if (hb.HasHit || area is not Hurtbox hurt || hurt.OwnerFighter is not { } defender) return;
            if (defender == hb.Attacker) return;
            Rules.ApplyHit(hb.Attacker, defender, hb.Attack);
            hb.HasHit = true;
        };
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
    }
}
