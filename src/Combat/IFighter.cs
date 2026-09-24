using Godot;

namespace ByteBrawl.Combat;

public interface IFighter
{
    float Damage { get; set; }
    int Stocks { get; set; }
    int Facing { get; }
    Vector2 Position { get; set; }
    Vector2 Velocity { get; set; }
    bool IsGrounded { get; }
    int HitstunFrames { get; set; }
    int InvincibleFrames { get; set; }
    bool ShieldActive { get; set; }
    float ShieldHealth { get; set; }
    ElementalMeter Meter { get; }
    void TakeDamage(float amount);
    void ApplyKnockback(Vector2 vector);
    void EnterHitstun(int frames);
    void EnterInvincibility(int frames);
    void DamageShield(float amount);
    void DeactivateShield();
    void LoseStock();
    void Respawn(Vector2 position, int invincibilityFrames);
    void SetChargingFull(bool value);
}
