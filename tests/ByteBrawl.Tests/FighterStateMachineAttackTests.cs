using ByteBrawl.Combat;
using ByteBrawl.Data;
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
}
