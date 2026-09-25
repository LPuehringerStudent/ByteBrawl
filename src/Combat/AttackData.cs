using Godot;

namespace ByteBrawl.Combat;

public class AttackData
{
    public string Id = "";
    public float BaseDamage;
    public float BaseKnockback;
    public float Scaling;
    public Vector2 Direction = Vector2.Zero;
    public int HitstunFrames;
    public int ActiveFrames;
    public ChargeConfig? Charge;
    public RecoveryConfig? Recovery;
    public List<AttackStage> Stages = new();
    public List<HitboxSpec> Hitboxes = new();
    public List<ArmorSpec> Armor = new();
    // Transient: set by HitboxManager.EffectiveAttack from HitboxSpec.NoKnockback.
    // Not authored directly on attacks.
    public bool NoKnockback;
}
