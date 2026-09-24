using ByteBrawl.Combat;
using Godot;

namespace ByteBrawl.Nodes;

public partial class HitboxManager : Node, IHitboxManager
{
    public MatchRules? Rules;

    public void Spawn(IFighter attacker, AttackData attack, float offsetX, float offsetY, float width, float height)
    {
        if (attacker is not Fighter a || Rules == null) return;
        var shape = new CollisionShape2D();
        if (attack.Shape == HitboxShape.Box)
            shape.Shape = new RectangleShape2D { Size = new Vector2(width, height) };
        else
            shape.Shape = new CircleShape2D { Radius = attack.Radius };
        var hb = new Hitbox
        {
            Attacker = a, Attack = attack, FramesRemaining = attack.ActiveFrames,
            Position = a.Position + new Vector2(offsetX * a.Facing, offsetY),
            Width = width, Height = height,
        };
        hb.AddChild(shape);
        hb.AreaEntered += area =>
        {
            if (hb.HasHit || area is not Hurtbox hurt) return;
            var defender = hurt.Owner;
            if (defender == hb.Attacker) return;
            Rules.ApplyHit(hb.Attacker, defender, hb.Attack);
            hb.HasHit = true;
        };
        AddChild(hb);
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
