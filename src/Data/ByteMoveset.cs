using ByteBrawl.Combat;
using Godot;

namespace ByteBrawl.Data;

public static class ByteMoveset
{
    public static Moveset Create()
    {
        var swordLight = new AttackData
        {
            Id = "sword-light", BaseDamage = 5, BaseKnockback = 150, Scaling = 1.2f,
            Direction = new Vector2(1, -0.4f), HitstunFrames = 12, ActiveFrames = 4,
        };
        var swordHeavy = new AttackData
        {
            Id = "sword-heavy", BaseDamage = 11, BaseKnockback = 230, Scaling = 1.5f,
            Direction = new Vector2(1, -0.3f), HitstunFrames = 20, ActiveFrames = 6,
            Charge = new ChargeConfig
            {
                MinChargeFrames = 30, MaxChargeFrames = 180, MaxHoldFrames = 180,
                DamageGrowth = 0.2f, KnockbackGrowth = 1.0f,
            },
        };
        var byteSpecial = new AttackData
        {
            Id = "byte-special", BaseDamage = 5, BaseKnockback = 180, Scaling = 1.3f,
            Direction = new Vector2(0.8f, -0.8f), HitstunFrames = 16, ActiveFrames = 5,
            Stages =
            {
                new AttackStage { Id = "byte-special-1", BaseDamage = 2, BaseKnockback = 45,
                    Scaling = 0, Direction = new Vector2(0.8f, -0.2f), HitstunFrames = 10,
                    ActiveFrames = 4, SpawnFrame = 4, OffsetX = 16, OffsetY = -2, Width = 16, Height = 18,
                    Shape = HitboxShape.Box },
                new AttackStage { Id = "byte-special-2", BaseDamage = 2, BaseKnockback = 55,
                    Scaling = 0, Direction = new Vector2(0.8f, -0.2f), HitstunFrames = 10,
                    ActiveFrames = 4, SpawnFrame = 12, OffsetX = 24, OffsetY = -2, Width = 18, Height = 18,
                    Shape = HitboxShape.Box },
                new AttackStage { Id = "byte-special-3", BaseDamage = 5, BaseKnockback = 180,
                    Scaling = 1.3f, Direction = new Vector2(0.8f, -0.8f), HitstunFrames = 16,
                    ActiveFrames = 5, SpawnFrame = 22, OffsetX = 34, OffsetY = -2, Width = 20, Height = 20,
                    Shape = HitboxShape.Box },
            },
        };

        var m = new Moveset { Stats = new FighterStats { RunSpeed = 120, JumpSpeed = 280, Weight = 1f } };
        foreach (var slot in new[]
        {
            AttackSlot.NeutralLight, AttackSlot.SideLight, AttackSlot.UpTilt, AttackSlot.DownTilt,
            AttackSlot.NeutralAir, AttackSlot.SideAir, AttackSlot.DownAir, AttackSlot.UpAir,
        })
            m.Attacks[slot] = swordLight;
        foreach (var slot in new[]
            { AttackSlot.NeutralHeavy, AttackSlot.UpHeavy, AttackSlot.DownHeavy, AttackSlot.SideHeavy })
            m.Attacks[slot] = swordHeavy;
        foreach (var slot in new[]
            { AttackSlot.NeutralSpecial, AttackSlot.UpSpecial, AttackSlot.DownSpecial, AttackSlot.SideSpecial })
            m.Attacks[slot] = byteSpecial;
        return m;
    }
}
