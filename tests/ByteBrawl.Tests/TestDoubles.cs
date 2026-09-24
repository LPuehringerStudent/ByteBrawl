using ByteBrawl.Combat;
using Godot;

namespace ByteBrawl.Tests;

public class FakeFighter : IFighter
{
    public float Damage { get; set; }
    public int Stocks { get; set; } = 3;
    public int Facing { get; set; } = 1;
    public Vector2 Position { get; set; }
    public Vector2 Velocity { get; set; }
    public bool Grounded { get; set; } = true;
    public bool IsGrounded => Grounded;
    public int HitstunFrames { get; set; }
    public int InvincibleFrames { get; set; }
    public bool ShieldActive { get; set; }
    public float ShieldHealth { get; set; } = 100;
    public ElementalMeter Meter { get; } = new();

    public void TakeDamage(float amount) => Damage += amount;
    public void ApplyKnockback(Vector2 v) => Velocity += v;
    public void EnterHitstun(int frames) => HitstunFrames = frames;
    public void EnterInvincibility(int frames) => InvincibleFrames = frames;
    public void DamageShield(float amount) { ShieldHealth -= amount; if (ShieldHealth <= 0) { ShieldHealth = 0; ShieldActive = false; } }
    public void DeactivateShield() => ShieldActive = false;
    public void LoseStock() { Stocks -= 1; Damage = 0; }
    public void Respawn(Vector2 position, int frames) { Position = position; Damage = 0; HitstunFrames = 0; EnterInvincibility(frames); }
    public virtual void SetChargingFull(bool value) { }
}

public class FakeHitboxManager : IHitboxManager
{
    public record Spawned(IFighter Attacker, AttackData Attack, float X, float Y, float W, float H);
    public List<Spawned> Spawns = new();
    public void Spawn(IFighter a, AttackData atk, float x, float y, float w, float h)
        => Spawns.Add(new Spawned(a, atk, x, y, w, h));
}
