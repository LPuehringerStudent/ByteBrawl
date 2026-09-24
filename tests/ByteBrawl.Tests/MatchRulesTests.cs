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
}
