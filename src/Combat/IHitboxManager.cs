namespace ByteBrawl.Combat;

public interface IHitboxManager
{
    void Spawn(IFighter attacker, AttackData attack, float offsetX, float offsetY, float width, float height);
}
