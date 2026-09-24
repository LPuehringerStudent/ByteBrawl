namespace ByteBrawl.Combat;

public interface IHitboxManager
{
    void Spawn(IFighter attacker, AttackData attack, HitboxSpec spec);
}
