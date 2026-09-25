using ByteBrawl.Combat;
using ByteBrawl.Data;
using Godot;
using Xunit;

namespace ByteBrawl.Tests;

public class FighterStateMachineMovementTests
{
    private static ActionFrame Neutral() => new()
    {
        MoveX = 0, MoveY = 0, JumpPressed = false, JumpHeld = false,
        AttackLight = false, AttackLightHeld = false, AttackHeavy = false, AttackHeavyHeld = false,
        AttackSpecial = false, AttackSpecialHeld = false, GadgetPressed = false, GadgetHeld = false,
        ShieldPressed = false, ShieldHeld = false, GrabPressed = false, GrabHeld = false,
    };

    private static (FighterStateMachine, FakeFighter) NewFsm(bool grounded = true)
    {
        var f = new FakeFighter { Grounded = grounded };
        return (new FighterStateMachine(f, new FakeHitboxManager(), ByteMoveset.Create()), f);
    }

    [Fact] public void ShieldHeldOnGround_EntersShield()
    {
        var (fsm, f) = NewFsm();
        fsm.Update(Neutral() with { ShieldHeld = true });
        Assert.True(f.ShieldActive);
        Assert.Equal(FighterState.Shield, fsm.CurrentState);
    }

    [Fact] public void ShieldDecaysWhileHeld()
    {
        var (fsm, f) = NewFsm();
        fsm.Update(Neutral() with { ShieldHeld = true });
        var before = f.ShieldHealth;
        fsm.Update(Neutral() with { ShieldHeld = true });
        Assert.True(f.ShieldHealth < before);
    }

    [Fact] public void ShieldReleased_ExitsShield()
    {
        var (fsm, f) = NewFsm();
        fsm.Update(Neutral() with { ShieldHeld = true });
        fsm.Update(Neutral());
        Assert.False(f.ShieldActive);
        Assert.Equal(FighterState.Idle, fsm.CurrentState);
    }

    [Fact] public void ShieldTapOnGround_EntersShield()
    {
        var (fsm, f) = NewFsm();
        fsm.Update(Neutral() with { ShieldPressed = true });
        Assert.Equal(FighterState.Shield, fsm.CurrentState);
        Assert.True(f.ShieldActive);
    }

    [Fact] public void ShieldDownOnGround_SpotDodges()
    {
        var (fsm, f) = NewFsm();
        fsm.Update(Neutral() with { ShieldPressed = true, MoveY = 1 });
        Assert.Equal(FighterState.SpotDodge, fsm.CurrentState);
        Assert.True(f.InvincibleFrames > 0);
    }

    [Fact] public void SpotDodgeEnds_ReturnsToIdle()
    {
        var (fsm, _) = NewFsm();
        fsm.Update(Neutral() with { ShieldPressed = true });
        for (var i = 0; i < 21; i++) fsm.Update(Neutral());
        Assert.Equal(FighterState.Idle, fsm.CurrentState);
    }

    [Fact] public void ShieldTapInAir_DirectionalAirDodge()
    {
        var (fsm, f) = NewFsm(false);
        fsm.Update(Neutral() with { ShieldPressed = true, MoveX = 1, MoveY = -1 });
        Assert.Equal(FighterState.AirDodge, fsm.CurrentState);
        Assert.Equal(220, f.Velocity.X, 0.01f);
        Assert.Equal(-220, f.Velocity.Y, 0.01f);
    }

    [Fact] public void ShieldTapInAirNeutral_SpotDodgesInPlace()
    {
        var (fsm, f) = NewFsm(false);
        fsm.Update(Neutral() with { ShieldPressed = true });
        Assert.Equal(FighterState.AirDodge, fsm.CurrentState);
        Assert.Equal(Vector2.Zero, f.Velocity);
    }

    [Fact] public void RunLeftThenNeutral_FacingPersists()
    {
        var (fsm, f) = NewFsm();
        fsm.Update(Neutral() with { MoveX = -1 });
        Assert.Equal(-1, f.Facing);
        fsm.Update(Neutral());
        Assert.Equal(FighterState.Idle, fsm.CurrentState);
        Assert.Equal(-1, f.Facing);
    }

    [Fact] public void Run_SetsHorizontalVelocity()
    {
        var (fsm, f) = NewFsm();
        fsm.Update(Neutral() with { MoveX = 1 });
        Assert.Equal(FighterState.Run, fsm.CurrentState);
        Assert.Equal(120, f.Velocity.X, 0.01f);
    }
}
