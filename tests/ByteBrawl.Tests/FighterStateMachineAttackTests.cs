using ByteBrawl.Combat;
using ByteBrawl.Data;
using Godot;
using Xunit;

namespace ByteBrawl.Tests;

public class FighterStateMachineAttackTests
{
    private static ActionFrame Neutral() => new();

    private static (FighterStateMachine, FakeFighter, FakeHitboxManager) NewFsm()
    {
        var f = new FakeFighter();
        var hb = new FakeHitboxManager();
        return (new FighterStateMachine(f, hb, ByteMoveset.Create()), f, hb);
    }

    [Fact] public void LightAttack_SpawnsHitboxImmediately()
    {
        var (fsm, _, hb) = NewFsm();
        fsm.Update(Neutral() with { AttackLight = true });
        Assert.Equal(FighterState.LightAttack, fsm.CurrentState);
        Assert.Single(hb.Spawns);
    }

    [Fact] public void MultiHitSpecial_SpawnsThreeStagesInOrder()
    {
        var (fsm, _, hb) = NewFsm();
        fsm.Update(Neutral() with { AttackSpecial = true });
        for (var i = 0; i < 30; i++) fsm.Update(Neutral());
        // Stages 2 and 3 carry two hitboxes each (base circle + fist tipper).
        Assert.Equal(5, hb.Spawns.Count);
        Assert.Equal("byte-special-1", hb.Spawns[0].Attack.Id);
        Assert.Equal("byte-special-2", hb.Spawns[1].Attack.Id);
        Assert.Equal("byte-special-2", hb.Spawns[2].Attack.Id);
        Assert.Equal("byte-special-3", hb.Spawns[3].Attack.Id);
        Assert.Equal("byte-special-3", hb.Spawns[4].Attack.Id);
        Assert.Equal(24f, hb.Spawns[1].Spec.OffsetX);
        Assert.Equal(30f, hb.Spawns[2].Spec.OffsetX);
        Assert.True(hb.Spawns[2].Spec.Radius < hb.Spawns[1].Spec.Radius); // fist tip < elbow
        Assert.Equal(41f, hb.Spawns[4].Spec.OffsetX);
        Assert.Equal(7f, hb.Spawns[4].Spec.DamageOverride); // tipper hits harder
    }

    [Fact] public void ChargeableHeavy_EntersChargingWithoutSpawning()
    {
        var (fsm, _, hb) = NewFsm();
        fsm.Update(Neutral() with { AttackHeavy = true, AttackHeavyHeld = true });
        Assert.Equal(FighterState.Charging, fsm.CurrentState);
        Assert.Empty(hb.Spawns);
    }

    [Fact] public void ChargeReleasedAfterMin_FiresWithBonusDamage()
    {
        var (fsm, _, hb) = NewFsm();
        fsm.Update(Neutral() with { AttackHeavy = true, AttackHeavyHeld = true });
        for (var i = 0; i < 35; i++) fsm.Update(Neutral() with { AttackHeavyHeld = true });
        fsm.Update(Neutral() with { AttackHeavyHeld = false });
        Assert.Equal(FighterState.HeavyAttack, fsm.CurrentState);
        Assert.Single(hb.Spawns);
        Assert.True(hb.Spawns[0].Attack.BaseDamage > 11);
    }

    [Fact] public void ChargeHeldToMaxPlusHold_AutoFires()
    {
        var (fsm, _, hb) = NewFsm();
        fsm.Update(Neutral() with { AttackHeavy = true, AttackHeavyHeld = true });
        for (var i = 0; i < 365; i++) fsm.Update(Neutral() with { AttackHeavyHeld = true });
        Assert.Equal(FighterState.HeavyAttack, fsm.CurrentState);
        Assert.Single(hb.Spawns);
    }

    [Fact] public void FullCharge_SetsChargingFullOnFighter()
    {
        var hb = new FakeHitboxManager();
        var full = false;
        // FakeFighter ignores SetChargingFull; observe via a small subclass instead.
        var observing = new ObservingFighter(() => full = true);
        var fsm2 = new FighterStateMachine(observing, hb, ByteMoveset.Create());
        fsm2.Update(Neutral() with { AttackHeavy = true, AttackHeavyHeld = true });
        for (var i = 0; i < 181; i++) fsm2.Update(Neutral() with { AttackHeavyHeld = true });
        Assert.True(full);
    }

    private class ObservingFighter : FakeFighter
    {
        private readonly Action _onFull;
        public ObservingFighter(Action onFull) => _onFull = onFull;
        public override void SetChargingFull(bool value) { if (value) _onFull(); }
    }

    private static (FighterStateMachine, FakeFighter, FakeHitboxManager) NewArmoredFsm()
    {
        var stage = new AttackStage
        {
            Id = "armored", BaseDamage = 5, BaseKnockback = 50, Scaling = 0,
            Direction = new Vector2(1, 0), HitstunFrames = 5, ActiveFrames = 4, SpawnFrame = 2,
            Hitboxes = { new HitboxSpec { OffsetX = 10, Radius = 6 } },
            Armor = { new ArmorSpec { Group = LimbGroup.Arm, Type = HurtboxType.HyperArmor } },
        };
        var attack = new AttackData { Id = "armored-atk", ActiveFrames = 4, Stages = { stage } };
        var moveset = new Moveset { Stats = new FighterStats { RunSpeed = 120, JumpSpeed = 280, Weight = 1f } };
        moveset.Attacks[AttackSlot.NeutralLight] = attack;
        var f = new FakeFighter();
        var hb = new FakeHitboxManager();
        return (new FighterStateMachine(f, hb, moveset), f, hb);
    }

    [Fact] public void StageArmor_AppliesAtSpawnFrame()
    {
        var (fsm, f, _) = NewArmoredFsm();
        fsm.Update(Neutral() with { AttackLight = true }); // frame 0: attack starts
        fsm.Update(Neutral()); // frame 1: before SpawnFrame 2
        Assert.False(f.HurtboxOverrides.ContainsKey(LimbGroup.Arm));
        fsm.Update(Neutral()); // frame 2: stage spawns
        Assert.Equal(HurtboxType.HyperArmor, f.HurtboxOverrides[LimbGroup.Arm]);
    }

    [Fact] public void StageArmor_ClearsAfterActiveFrames()
    {
        var (fsm, f, _) = NewArmoredFsm();
        fsm.Update(Neutral() with { AttackLight = true });
        for (var i = 0; i < 6; i++) fsm.Update(Neutral()); // frame 2+4: expired
        Assert.False(f.HurtboxOverrides.TryGetValue(LimbGroup.Arm, out var t) && t.HasValue);
    }

    [Fact] public void SpotDodge_SetsAllGroupsIntangibleAndClearsOnExit()
    {
        var (fsm, f, _) = NewFsm();
        f.Grounded = true;
        fsm.Update(Neutral() with { ShieldPressed = true, MoveY = 1 }); // shield+down = spot dodge
        foreach (LimbGroup g in Enum.GetValues<LimbGroup>())
            Assert.Equal(HurtboxType.Intangible, f.HurtboxOverrides[g]);
        for (var i = 0; i < 25; i++) fsm.Update(Neutral());
        Assert.Equal(FighterState.SpotDodge, fsm.CurrentState); // recovery lag still in dodge
        for (var i = 0; i < 6; i++) fsm.Update(Neutral());
        Assert.Equal(FighterState.Idle, fsm.CurrentState);
        foreach (LimbGroup g in Enum.GetValues<LimbGroup>())
            Assert.True(!f.HurtboxOverrides.TryGetValue(g, out var t) || t is null or HurtboxType.Vulnerable);
    }

    [Fact] public void AirDodge_SetsAllGroupsIntangible()
    {
        var (fsm, f, _) = NewFsm();
        f.Grounded = false;
        fsm.Update(Neutral() with { ShieldPressed = true });
        foreach (LimbGroup g in Enum.GetValues<LimbGroup>())
            Assert.Equal(HurtboxType.Intangible, f.HurtboxOverrides[g]);
    }

    private static AttackData RecoveryAttack(bool canActAfter = false) => new()
    {
        Id = "test-recovery", BaseDamage = 6, BaseKnockback = 150, Scaling = 1f,
        Direction = new Vector2(0, -1), HitstunFrames = 10, ActiveFrames = 4,
        Hitboxes = { new HitboxSpec { OffsetX = 0, Radius = 8 } },
        Recovery = new RecoveryConfig { VerticalBoost = 380, CanActAfter = canActAfter },
    };

    private static (FighterStateMachine, FakeFighter, FakeHitboxManager) NewRecoveryFsm(bool canActAfter = false)
    {
        var moveset = new Moveset { Stats = new FighterStats { RunSpeed = 120, JumpSpeed = 280, Weight = 1f } };
        moveset.Attacks[AttackSlot.UpHeavy] = RecoveryAttack(canActAfter);
        moveset.Attacks[AttackSlot.NeutralHeavy] = new AttackData
        {
            Id = "neutral-heavy", BaseDamage = 5, BaseKnockback = 100, Scaling = 1f,
            Direction = new Vector2(1, 0), HitstunFrames = 10, ActiveFrames = 4,
            Hitboxes = { new HitboxSpec { OffsetX = 0, Radius = 8 } },
        };
        moveset.Attacks[AttackSlot.NeutralLight] = new AttackData
        {
            Id = "neutral-light", BaseDamage = 3, BaseKnockback = 60, Scaling = 0.5f,
            Direction = new Vector2(1, 0), HitstunFrames = 8, ActiveFrames = 3,
            Hitboxes = { new HitboxSpec { OffsetX = 0, Radius = 6 } },
        };
        var f = new FakeFighter { Grounded = false };
        var hb = new FakeHitboxManager();
        return (new FighterStateMachine(f, hb, moveset), f, hb);
    }

    [Fact] public void AirUpHeavy_FiresRecoveryWithBoost()
    {
        var (fsm, f, hb) = NewRecoveryFsm();
        fsm.Update(Neutral() with { AttackHeavy = true, MoveY = -1 });
        Assert.Equal(FighterState.HeavyAttack, fsm.CurrentState);
        Assert.Equal(-380, f.Velocity.Y, 0.01f);
        Assert.Single(hb.Spawns);
        Assert.Equal("test-recovery", hb.Spawns[0].Attack.Id);
    }

    [Fact] public void AirHeavyWithoutUp_FiresNeutralHeavy()
    {
        var (fsm, f, hb) = NewRecoveryFsm();
        fsm.Update(Neutral() with { AttackHeavy = true }); // no up: not a recovery
        Assert.Equal(FighterState.HeavyAttack, fsm.CurrentState);
        Assert.Single(hb.Spawns);
        Assert.Equal("neutral-heavy", hb.Spawns[0].Attack.Id);
        Assert.Equal(0, f.Velocity.Y, 0.01f); // no boost
    }

    [Fact] public void Recovery_OncePerAirtime_SecondAttemptIgnored()
    {
        var (fsm, f, hb) = NewRecoveryFsm();
        fsm.Update(Neutral() with { AttackHeavy = true, MoveY = -1 });
        for (var i = 0; i < 25; i++) // FakeFighter has no gravity: simulate falling so post-attack state is Fall
        {
            f.Velocity = new Vector2(f.Velocity.X, 100);
            fsm.Update(Neutral());
        }
        Assert.Equal(FighterState.Fall, fsm.CurrentState);
        fsm.Update(Neutral() with { AttackHeavy = true, MoveY = -1 }); // spent
        Assert.Single(hb.Spawns);
        Assert.Equal(FighterState.Fall, fsm.CurrentState);
    }

    [Fact] public void Landing_ResetsRecoveryAvailability()
    {
        var (fsm, f, hb) = NewRecoveryFsm();
        fsm.Update(Neutral() with { AttackHeavy = true, MoveY = -1 });
        for (var i = 0; i < 25; i++) fsm.Update(Neutral());
        f.Grounded = true;
        fsm.Update(Neutral()); // land
        f.Grounded = false;
        fsm.Update(Neutral() with { AttackHeavy = true, MoveY = -1 });
        Assert.Equal(2, hb.Spawns.Count);
    }

    [Fact] public void GroundedHeavyWithUp_StillFiresNeutralHeavy()
    {
        var (fsm, _, hb) = NewFsm(); // ByteMoveset: grounded heavy is the chargeable neutral
        fsm.Update(Neutral() with { AttackHeavy = true, MoveY = -1, AttackHeavyHeld = true });
        Assert.Equal(FighterState.Charging, fsm.CurrentState);
        Assert.Empty(hb.Spawns);
    }

    [Fact] public void Lockout_BlocksFollowUpAttacksInAir()
    {
        var (fsm, f, hb) = NewRecoveryFsm();
        fsm.Update(Neutral() with { AttackHeavy = true, MoveY = -1 }); // default: CanActAfter = false
        for (var i = 0; i < 25; i++) // FakeFighter has no gravity: simulate falling so post-attack state is Fall
        {
            f.Velocity = new Vector2(f.Velocity.X, 100);
            fsm.Update(Neutral()); // attack ends airborne
        }
        Assert.Equal(FighterState.Fall, fsm.CurrentState);
        fsm.Update(Neutral() with { AttackLight = true });
        Assert.Equal(FighterState.Fall, fsm.CurrentState); // locked
    }

    [Fact] public void Lockout_ClearsOnLanding()
    {
        var (fsm, f, hb) = NewRecoveryFsm();
        fsm.Update(Neutral() with { AttackHeavy = true, MoveY = -1 });
        for (var i = 0; i < 25; i++)
        {
            f.Velocity = new Vector2(f.Velocity.X, 100);
            fsm.Update(Neutral());
        }
        f.Grounded = true;
        fsm.Update(Neutral()); // land: lockout clears
        fsm.Update(Neutral() with { AttackLight = true });
        Assert.Equal(FighterState.LightAttack, fsm.CurrentState);
    }

    [Fact] public void CanActAfter_SkipsLockout()
    {
        var (fsm, f, hb) = NewRecoveryFsm(canActAfter: true);
        fsm.Update(Neutral() with { AttackHeavy = true, MoveY = -1 });
        for (var i = 0; i < 25; i++)
        {
            f.Velocity = new Vector2(f.Velocity.X, 100);
            fsm.Update(Neutral());
        }
        fsm.Update(Neutral() with { AttackLight = true });
        Assert.Equal(FighterState.LightAttack, fsm.CurrentState); // combo-finisher: free follow-up
    }
}
