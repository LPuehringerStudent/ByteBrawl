namespace ByteBrawl.Combat;

public record struct ActionFrame
{
    public int MoveX;
    public int MoveY;
    public bool JumpPressed, JumpHeld;
    public bool AttackLight, AttackLightHeld;
    public bool AttackHeavy, AttackHeavyHeld;
    public bool AttackSpecial, AttackSpecialHeld;
    public bool GadgetPressed, GadgetHeld;
    public bool ShieldPressed, ShieldHeld;
    public bool GrabPressed, GrabHeld;
}
