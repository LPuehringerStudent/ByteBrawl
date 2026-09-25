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
            Hitboxes = { new HitboxSpec { OffsetX = 14, OffsetY = -2, Radius = 6 } },
        };
        var swordHeavy = new AttackData
        {
            Id = "sword-heavy", BaseDamage = 11, BaseKnockback = 230, Scaling = 1.5f,
            Direction = new Vector2(1, -0.3f), HitstunFrames = 20, ActiveFrames = 6,
            Hitboxes = { new HitboxSpec { OffsetX = 14, OffsetY = -2, Radius = 7 } },
            Armor = { new ArmorSpec { Group = LimbGroup.Arm, Type = HurtboxType.HyperArmor } },
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
                    ActiveFrames = 4, SpawnFrame = 4,
                    Hitboxes = { new HitboxSpec { OffsetX = 16, OffsetY = -2, Radius = 6 } } },
                new AttackStage { Id = "byte-special-2", BaseDamage = 2, BaseKnockback = 55,
                    Scaling = 0, Direction = new Vector2(0.8f, -0.2f), HitstunFrames = 10,
                    ActiveFrames = 4, SpawnFrame = 12,
                    Hitboxes =
                    {
                        new HitboxSpec { OffsetX = 24, OffsetY = -2, Radius = 7 },
                        new HitboxSpec { OffsetX = 30, OffsetY = -3, Radius = 4, DamageOverride = 3 },
                    } },
                new AttackStage { Id = "byte-special-3", BaseDamage = 5, BaseKnockback = 180,
                    Scaling = 1.3f, Direction = new Vector2(0.8f, -0.8f), HitstunFrames = 16,
                    ActiveFrames = 5, SpawnFrame = 22,
                    Hitboxes =
                    {
                        new HitboxSpec { OffsetX = 34, OffsetY = -2, Radius = 8 },
                        new HitboxSpec { OffsetX = 41, OffsetY = -4, Radius = 5, DamageOverride = 7, KnockbackOverride = 210 },
                    } },
            },
        };
        var upHeavyRecovery = new AttackData
        {
            Id = "sword-upheavy-recovery",
            Recovery = new RecoveryConfig { VerticalBoost = 380 },
            Charge = new ChargeConfig
            {
                MinChargeFrames = 30, MaxChargeFrames = 180, MaxHoldFrames = 180,
                DamageGrowth = 0.2f, KnockbackGrowth = 1.0f,
            },
            Stages =
            {
                new AttackStage { Id = "recovery-1", BaseDamage = 8, BaseKnockback = 200, Scaling = 1.2f,
                    Direction = new Vector2(0.3f, -1), HitstunFrames = 18, ActiveFrames = 3, SpawnFrame = 2,
                    Hitboxes = { new HitboxSpec { OffsetX = 6, OffsetY = -10, Radius = 7 } },
                    Armor = { new ArmorSpec { Group = LimbGroup.Arm, Type = HurtboxType.HyperArmor } } },
                new AttackStage { Id = "recovery-2", BaseDamage = 2, BaseKnockback = 40, Scaling = 0,
                    Direction = new Vector2(0, -1), HitstunFrames = 8, ActiveFrames = 3, SpawnFrame = 7,
                    Hitboxes = { new HitboxSpec { OffsetX = 4, OffsetY = -8, Radius = 8 } },
                    Armor = { new ArmorSpec { Group = LimbGroup.Arm, Type = HurtboxType.HyperArmor } } },
                new AttackStage { Id = "recovery-3", BaseDamage = 2, BaseKnockback = 40, Scaling = 0,
                    Direction = new Vector2(0, -1), HitstunFrames = 8, ActiveFrames = 3, SpawnFrame = 12,
                    Hitboxes = { new HitboxSpec { OffsetX = 4, OffsetY = -8, Radius = 8 } },
                    Armor = { new ArmorSpec { Group = LimbGroup.Arm, Type = HurtboxType.HyperArmor } } },
                new AttackStage { Id = "recovery-4", BaseDamage = 5, BaseKnockback = 180, Scaling = 1.0f,
                    Direction = new Vector2(0.2f, -1), HitstunFrames = 16, ActiveFrames = 4, SpawnFrame = 17,
                    Hitboxes = { new HitboxSpec { OffsetX = 5, OffsetY = -12, Radius = 8 } } },
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
        m.Attacks[AttackSlot.UpHeavy] = upHeavyRecovery;
        foreach (var slot in new[]
            { AttackSlot.NeutralSpecial, AttackSlot.UpSpecial, AttackSlot.DownSpecial, AttackSlot.SideSpecial })
            m.Attacks[slot] = byteSpecial;
        return m;
    }
}
