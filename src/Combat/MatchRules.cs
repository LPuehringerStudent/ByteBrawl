using Godot;

namespace ByteBrawl.Combat;

public enum MatchState { Active, P1Win, P2Win }

public class MatchRules
{
    public MatchState State { get; private set; } = MatchState.Active;
    public double MatchTimer { get; private set; } = 180;
    private double _elapsedMs;

    private readonly MatchRulesConfig _config;
    private readonly IFighter _player1;
    private readonly IFighter _player2;

    public MatchRules(MatchRulesConfig config, IFighter player1, IFighter player2)
    {
        _config = config;
        _player1 = player1;
        _player2 = player2;
    }

    public void ApplyHit(IFighter attacker, IFighter defender, AttackData attack)
    {
        if (State != MatchState.Active) return;
        if (defender.InvincibleFrames > 0) return;

        attacker.Meter.AddFromDealt(attack.BaseDamage);
        defender.Meter.AddFromTaken(attack.BaseDamage);

        if (defender.ShieldActive && defender.ShieldHealth > 0)
        {
            defender.DamageShield(attack.BaseDamage * 3);
            defender.ApplyKnockback(new Vector2(
                attack.Direction.X * attacker.Facing * attack.BaseKnockback * 0.3f,
                attack.Direction.Y * attack.BaseKnockback * 0.3f));
            return;
        }

        var preDamage = defender.Damage;
        defender.TakeDamage(attack.BaseDamage);
        var knockback = attack.BaseKnockback + preDamage * attack.Scaling;
        defender.ApplyKnockback(new Vector2(
            attack.Direction.X * attacker.Facing * knockback,
            attack.Direction.Y * knockback));
        defender.EnterHitstun(attack.HitstunFrames);
    }

    public void CheckRingOut(IFighter player, Func<float, float, bool> isOutOfBounds, Vector2 spawn)
    {
        if (State != MatchState.Active) return;
        if (!isOutOfBounds(player.Position.X, player.Position.Y)) return;

        player.LoseStock();
        if (player.Stocks <= 0)
            State = player == _player1 ? MatchState.P2Win : MatchState.P1Win;
        else
            player.Respawn(spawn, _config.RespawnInvincibilityFrames);
    }

    public void Update(double deltaMs)
    {
        if (State != MatchState.Active) return;
        _elapsedMs += deltaMs;
        if (_elapsedMs >= 1000)
        {
            _elapsedMs -= 1000;
            MatchTimer -= 1;
            if (MatchTimer <= 0) { MatchTimer = 0; State = MatchState.P1Win; }
        }
    }

    public void ResetTimer() { MatchTimer = 180; _elapsedMs = 0; }
}
