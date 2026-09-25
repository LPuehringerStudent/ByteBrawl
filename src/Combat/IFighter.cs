using Godot;

namespace ByteBrawl.Combat;

public interface IFighter
{
    float Damage { get; set; }
    int Stocks { get; set; }
    int Facing { get; set; }
    Vector2 Position { get; set; }
    Vector2 Velocity { get; set; }
    bool IsGrounded { get; }
    int HitstunFrames { get; set; }
    int InvincibleFrames { get; set; }
    bool ShieldActive { get; set; }
    float ShieldHealth { get; set; }
    ElementalMeter Meter { get; }
    IElementalFrenzy? Frenzy { get; set; }
    void TakeDamage(float amount);
    void ApplyKnockback(Vector2 vector);
    void EnterHitstun(int frames);
    void EnterInvincibility(int frames);
    void DamageShield(float amount);
    void DeactivateShield();
    void LoseStock();
    void Respawn(Vector2 position, int invincibilityFrames);
    void SetChargingFull(bool value);
    // Per-limb hurtbox state. null resets the group to Vulnerable. The FSM
    // drives this (dodge intangibility, attack-stage armor); Fighter maps
    // groups onto its rig's Hurtbox nodes.
    void SetHurtboxOverride(LimbGroup group, HurtboxType? type, float armorBreakKb = float.MaxValue);
}
