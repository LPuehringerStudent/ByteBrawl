namespace ByteBrawl.Combat;

public class Moveset
{
    public FighterStats Stats = new();
    public Dictionary<AttackSlot, AttackData> Attacks = new();

    public AttackData Get(AttackSlot slot) => Attacks[slot];
}
