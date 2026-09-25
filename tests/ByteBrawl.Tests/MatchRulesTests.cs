using ByteBrawl.Combat;
using Godot;
using Xunit;

namespace ByteBrawl.Tests;

public class MatchRulesTests
{
    private static readonly MatchRulesConfig Config = new();

    private static (MatchRules, FakeFighter, FakeFighter) NewMatch()
    {
        var p1 = new FakeFighter();
        var p2 = new FakeFighter();
        return (new MatchRules(Config, p1, p2), p1, p2);
    }

    private static AttackData Attack() => new()
    {
        Id = "t", BaseDamage = 10, BaseKnockback = 100, Scaling = 1.5f,
        Direction = new Vector2(1, -0.5f), HitstunFrames = 10, ActiveFrames = 4,
    };

    [Fact] public void Hit_IncreasesDefenderDamage() { var (r, p1, p2) = NewMatch(); r.ApplyHit(p1, p2, Attack()); Assert.Equal(10, p2.Damage); }
    [Fact] public void Hit_ScalesKnockbackWithDamage() { var (r, p1, p2) = NewMatch(); p2.Damage = 50; r.ApplyHit(p1, p2, Attack()); Assert.Equal(100 + 50 * 1.5f, p2.Velocity.X, 0.01f); }
    [Fact] public void RingOut_LastStockEndsMatch() { var (r, _, p2) = NewMatch(); p2.Stocks = 1; r.CheckRingOut(p2, (x, y) => true, Vector2.Zero); Assert.Equal(MatchState.P1Win, r.State); Assert.Equal(0, p2.Stocks); }
    [Fact] public void RingOut_RespawnsWithStocksLeft() { var (r, _, p2) = NewMatch(); p2.Stocks = 2; r.CheckRingOut(p2, (x, y) => true, new Vector2(50, 100)); Assert.Equal(MatchState.Active, r.State); Assert.Equal(1, p2.Stocks); Assert.Equal(new Vector2(50, 100), p2.Position); Assert.Equal(0, p2.Damage); }
    [Fact] public void Hit_IgnoredWhileInvincible() { var (r, p1, p2) = NewMatch(); p2.InvincibleFrames = 5; r.ApplyHit(p1, p2, Attack()); Assert.Equal(0, p2.Damage); Assert.Equal(Vector2.Zero, p2.Velocity); }
    [Fact] public void Hit_DamagesShieldInstead() { var (r, p1, p2) = NewMatch(); p2.ShieldActive = true; r.ApplyHit(p1, p2, Attack()); Assert.Equal(0, p2.Damage); Assert.Equal(70, p2.ShieldHealth, 0.01f); }

    [Fact] public void Hit_IntangibleHurtbox_NoContactAtAll()
    {
        var (r, p1, p2) = NewMatch();
        r.ApplyHit(p1, p2, Attack(), HurtboxType.Intangible);
        Assert.Equal(0, p2.Damage);
        Assert.Equal(0, p1.Meter.Value); // no meter gain: contact never happened
    }

    [Fact] public void Hit_InvincibleHurtbox_BlocksAndRaisesHitBlocked()
    {
        var (r, p1, p2) = NewMatch();
        IFighter? blocked = null;
        r.HitBlocked += f => blocked = f;
        r.ApplyHit(p1, p2, Attack(), HurtboxType.Invincible);
        Assert.Equal(0, p2.Damage);
        Assert.Equal(Vector2.Zero, p2.Velocity);
        Assert.Same(p2, blocked);
    }

    [Fact] public void Hit_HyperArmor_DamagesWithoutKnockbackOrHitstun()
    {
        var (r, p1, p2) = NewMatch();
        r.ApplyHit(p1, p2, Attack(), HurtboxType.HyperArmor);
        Assert.Equal(10, p2.Damage);
        Assert.Equal(Vector2.Zero, p2.Velocity);
        Assert.Equal(0, p2.HitstunFrames);
    }

    [Fact] public void Hit_SuperArmorBelowThreshold_DamagesWithoutKnockback()
    {
        var (r, p1, p2) = NewMatch(); // attack base kb is 100
        r.ApplyHit(p1, p2, Attack(), HurtboxType.SuperArmor, armorBreakKb: 200);
        Assert.Equal(10, p2.Damage);
        Assert.Equal(Vector2.Zero, p2.Velocity);
        Assert.Equal(0, p2.HitstunFrames);
    }

    [Fact] public void Hit_SuperArmorAboveThreshold_BreaksIntoFullHit()
    {
        var (r, p1, p2) = NewMatch(); // attack base kb is 100
        r.ApplyHit(p1, p2, Attack(), HurtboxType.SuperArmor, armorBreakKb: 50);
        Assert.Equal(10, p2.Damage);
        Assert.Equal(100, p2.Velocity.X, 0.01f);
        Assert.Equal(10, p2.HitstunFrames);
    }

    [Fact] public void Wind_PushesWithoutDamageOrHitstun()
    {
        var (r, _, p2) = NewMatch();
        var spec = new HitboxSpec { Type = HitboxType.Wind, Push = new Vector2(60, -30) };
        r.ApplyWind(p2, spec, facing: 1);
        Assert.Equal(new Vector2(60, -30), p2.Velocity);
        Assert.Equal(0, p2.Damage);
        Assert.Equal(0, p2.HitstunFrames);
    }

    [Fact] public void Wind_FacingFlipsHorizontalPush()
    {
        var (r, _, p2) = NewMatch();
        var spec = new HitboxSpec { Type = HitboxType.Wind, Push = new Vector2(60, -30) };
        r.ApplyWind(p2, spec, facing: -1);
        Assert.Equal(new Vector2(-60, -30), p2.Velocity);
    }
}
