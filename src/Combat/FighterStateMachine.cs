using Godot;

namespace ByteBrawl.Combat;

public class FighterStateMachine
{
    public const int DefaultAttackDuration = 20;
    public const int AttackRecoveryFrames = 10;
    public const int SpotDodgeFrames = 20;
    public const int SpotDodgeTapThreshold = 5;
    public const int AirDodgeFrames = 20;
    public const float AirDodgeSpeed = 220f;

    public FighterState CurrentState => _state;

    private FighterState _state = FighterState.Idle;
    private int _stateFrames;
    private int _attackCooldown;
    private readonly IFighter _fighter;
    private readonly IHitboxManager _hitboxes;
    private readonly Moveset _moveset;

    public FighterStateMachine(IFighter fighter, IHitboxManager hitboxes, Moveset moveset)
    {
        _fighter = fighter;
        _hitboxes = hitboxes;
        _moveset = moveset;
    }

    public void Update(ActionFrame actions)
    {
        if (_attackCooldown > 0) _attackCooldown--;

        if (_fighter.HitstunFrames > 0)
        {
            _fighter.DeactivateShield();
            SetState(FighterState.Hitstun);
            _stateFrames++;
            return;
        }

        if (_state == FighterState.Hitstun && _fighter.HitstunFrames == 0)
            SetState(_fighter.IsGrounded ? FighterState.Idle : FighterState.Fall);

        if (IsAttackState(_state)) { TickAttack(actions); return; }

        if (_state == FighterState.Charging) { TickCharge(actions); return; }

        if (_state == FighterState.SpotDodge)
        {
            _stateFrames++;
            _fighter.Velocity = new Vector2(0, _fighter.Velocity.Y);
            if (_stateFrames >= SpotDodgeTapThreshold && actions.ShieldHeld)
            {
                _fighter.InvincibleFrames = 0;
                _fighter.ShieldActive = true;
                SetState(FighterState.Shield);
                return;
            }
            if (_stateFrames >= SpotDodgeFrames) SetState(FighterState.Idle);
            return;
        }

        if (_state == FighterState.AirDodge)
        {
            _stateFrames++;
            if (_stateFrames >= AirDodgeFrames)
            {
                _fighter.Velocity = new Vector2(_fighter.Velocity.X, 80);
                SetState(FighterState.Fall);
            }
            return;
        }

        if (_state == FighterState.Shield)
        {
            if (!actions.ShieldHeld || _fighter.ShieldHealth <= 0)
            {
                _fighter.DeactivateShield();
                SetState(_fighter.IsGrounded ? FighterState.Idle : FighterState.Fall);
            }
            else
            {
                _fighter.Velocity = new Vector2(0, _fighter.Velocity.Y);
                _fighter.ShieldHealth -= 0.1f;
            }
            return;
        }

        if (actions.ShieldPressed)
        {
            if (_fighter.IsGrounded) { SetState(FighterState.SpotDodge); return; }
            var dirY = actions.MoveY < 0 ? -1 : actions.MoveY > 0 ? 1 : 0;
            _fighter.Velocity = new Vector2(actions.MoveX * AirDodgeSpeed, dirY * AirDodgeSpeed);
            SetState(FighterState.AirDodge);
            return;
        }

        if (actions.ShieldHeld && _fighter.IsGrounded)
        {
            _fighter.ShieldActive = true;
            SetState(FighterState.Shield);
            return;
        }

        if (actions.JumpPressed && _fighter.IsGrounded)
            _fighter.Velocity = new Vector2(_fighter.Velocity.X, -_moveset.Stats.JumpSpeed);

        if (actions.AttackLight && _attackCooldown == 0) { StartAttack(FighterState.LightAttack, _moveset.Get(AttackSlot.NeutralLight)); return; }
        if (actions.AttackHeavy && _attackCooldown == 0) { StartAttack(FighterState.HeavyAttack, _moveset.Get(AttackSlot.NeutralHeavy)); return; }
        if (actions.AttackSpecial && _attackCooldown == 0) { StartAttack(FighterState.Special, _moveset.Get(AttackSlot.NeutralSpecial)); return; }

        if (_fighter.IsGrounded)
        {
            if (actions.MoveX != 0)
            {
                SetState(FighterState.Run);
                _fighter.Velocity = new Vector2(actions.MoveX * _moveset.Stats.RunSpeed, _fighter.Velocity.Y);
            }
            else
            {
                SetState(FighterState.Idle);
                _fighter.Velocity = new Vector2(0, _fighter.Velocity.Y);
            }
        }
        else
        {
            if (actions.MoveX != 0)
                _fighter.Velocity = new Vector2(actions.MoveX * _moveset.Stats.RunSpeed, _fighter.Velocity.Y);
            SetState(_fighter.Velocity.Y < 0 ? FighterState.Jump : FighterState.Fall);
        }

        _stateFrames++;
    }

    private void TickAttack(ActionFrame actions) => throw new NotImplementedException("Task 5");
    private void TickCharge(ActionFrame actions) => throw new NotImplementedException("Task 5");

    private void StartAttack(FighterState state, AttackData attack)
    {
        _fighter.DeactivateShield();
        SetState(state);
        _attackCooldown = DefaultAttackDuration;
        _hitboxes.Spawn(_fighter, attack, 14, -2, 12, 16);
    }

    private void SetState(FighterState newState)
    {
        if (_state == newState) return;
        _state = newState;
        _stateFrames = 0;
        if (newState == FighterState.SpotDodge)
            _fighter.EnterInvincibility(SpotDodgeFrames);
    }

    private static bool IsAttackState(FighterState s) =>
        s is FighterState.LightAttack or FighterState.HeavyAttack or FighterState.Special;
}
