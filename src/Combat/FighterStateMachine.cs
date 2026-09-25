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

    public const float ChargeAirDrag = 0.99f;
    public const float ChargeGroundDrag = 0.8f;
    public const float ChargeMaxFallSpeed = 120f;

    private AttackStage[] _sequence = Array.Empty<AttackStage>();
    private int _nextStageIndex;
    private int _attackTotalFrames = DefaultAttackDuration;
    private AttackData? _chargeAttack;
    private FighterState _chargeFiredState;
    private int _armorUntilFrame = -1;
    private readonly List<LimbGroup> _armoredGroups = new();
    private bool _intangibleActive;

    public FighterState CurrentState => _state;
    public int StateFrames => _stateFrames;

    private FighterState _state = FighterState.Idle;
    private int _stateFrames;
    private int _attackCooldown;
    private int _airJumpsUsed;
    private bool _recoveryUsed;
    private bool _attacksLocked;
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

        if (_fighter.IsGrounded)
        {
            _airJumpsUsed = 0;
            _recoveryUsed = false;
            _attacksLocked = false;
        }

        if (_fighter.HitstunFrames > 0)
        {
            ClearCombatOverrides();
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
                if (_intangibleActive) SetIntangible(false);
                _fighter.ShieldActive = true;
                SetState(FighterState.Shield);
                return;
            }
            if (_stateFrames >= SpotDodgeFrames)
            {
                if (_intangibleActive) SetIntangible(false);
                SetState(FighterState.Idle);
            }
            return;
        }

        if (_state == FighterState.AirDodge)
        {
            _stateFrames++;
            if (_stateFrames >= AirDodgeFrames)
            {
                if (_intangibleActive) SetIntangible(false);
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
            if (_fighter.IsGrounded)
            {
                // Neutral shield on the ground; shield+down tap = spot dodge.
                if (actions.MoveY > 0) { SetState(FighterState.SpotDodge); return; }
                _fighter.ShieldActive = true;
                SetState(FighterState.Shield);
                return;
            }
            // Air: directional dodge, or a spot dodge in place when neutral.
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

        if (actions.JumpPressed)
        {
            var groundedJump = _fighter.IsGrounded;
            var airJump = !groundedJump && _airJumpsUsed < _moveset.Stats.AirJumps;
            if (groundedJump || airJump)
            {
                _fighter.Velocity = new Vector2(_fighter.Velocity.X, -_moveset.Stats.JumpSpeed);
                if (airJump) _airJumpsUsed++;
            }
        }

        if (actions.AttackLight && _attackCooldown == 0) { StartAttack(FighterState.LightAttack, _moveset.Get(AttackSlot.NeutralLight)); return; }
        if (actions.AttackHeavy && _attackCooldown == 0)
        {
            // Air + up: recovery. Consumes the input even when spent (Smash-style).
            if (!_fighter.IsGrounded && actions.MoveY < 0)
            {
                if (!_recoveryUsed)
                {
                    var upHeavy = _moveset.Get(AttackSlot.UpHeavy);
                    StartAttack(FighterState.HeavyAttack, upHeavy);
                    if (upHeavy.Recovery is { } rec)
                    {
                        _fighter.Velocity = new Vector2(_fighter.Velocity.X, -rec.VerticalBoost);
                        _recoveryUsed = true;
                        _attacksLocked = !rec.CanActAfter;
                    }
                }
                return;
            }
            StartAttack(FighterState.HeavyAttack, _moveset.Get(AttackSlot.NeutralHeavy));
            return;
        }
        if (actions.AttackSpecial && _attackCooldown == 0) { StartAttack(FighterState.Special, _moveset.Get(AttackSlot.NeutralSpecial)); return; }

        if (_fighter.IsGrounded)
        {
            if (actions.MoveX != 0)
            {
                SetState(FighterState.Run);
                _fighter.Facing = actions.MoveX > 0 ? 1 : -1;
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
            {
                _fighter.Facing = actions.MoveX > 0 ? 1 : -1;
                _fighter.Velocity = new Vector2(actions.MoveX * _moveset.Stats.RunSpeed, _fighter.Velocity.Y);
            }
            SetState(_fighter.Velocity.Y < 0 ? FighterState.Jump : FighterState.Fall);
        }

        _stateFrames++;
    }

    private void TickAttack(ActionFrame actions)
    {
        _stateFrames++;
        if (_armorUntilFrame >= 0 && _stateFrames >= _armorUntilFrame)
        {
            foreach (var g in _armoredGroups) _fighter.SetHurtboxOverride(g, null);
            _armoredGroups.Clear();
            _armorUntilFrame = -1;
        }
        while (_nextStageIndex < _sequence.Length &&
               _stateFrames >= _sequence[_nextStageIndex].SpawnFrame)
        {
            var stage = _sequence[_nextStageIndex++];
            ApplyArmor(stage, _stateFrames + stage.ActiveFrames);
            foreach (var spec in stage.Hitboxes)
                _hitboxes.Spawn(_fighter, stage, spec);
        }
        if (_stateFrames >= _attackTotalFrames)
            SetState(_fighter.IsGrounded ? FighterState.Idle : FighterState.Fall);
    }

    private void TickCharge(ActionFrame actions)
    {
        if (_chargeAttack?.Charge is not { } charge) return;

        var held = _chargeFiredState == FighterState.HeavyAttack
            ? actions.AttackHeavyHeld
            : actions.AttackSpecialHeld;

        var drag = _fighter.IsGrounded ? ChargeGroundDrag : ChargeAirDrag;
        var vy = _fighter.Velocity.Y * drag;
        if (!_fighter.IsGrounded && vy > ChargeMaxFallSpeed) vy = ChargeMaxFallSpeed;
        if (!_fighter.IsGrounded && vy < 0) vy *= drag;
        _fighter.Velocity = new Vector2(_fighter.Velocity.X * drag, vy);

        _stateFrames++;
        var full = _stateFrames >= charge.MaxChargeFrames;
        _fighter.SetChargingFull(full);

        var autoRelease = _stateFrames >= charge.MaxChargeFrames + charge.MaxHoldFrames;
        var released = !held;
        if ((full && released) || autoRelease)
            FireCharged(charge.MaxChargeFrames);
        else if (!full && released && _stateFrames >= charge.MinChargeFrames)
            FireCharged(_stateFrames);
    }

    private void FireCharged(int chargeFrames)
    {
        if (_chargeAttack?.Charge is not { } charge) return;
        var frames = Math.Clamp(chargeFrames, 0, charge.MaxChargeFrames);
        var charged = new AttackData
        {
            Id = $"{_chargeAttack.Id}-charged",
            BaseDamage = _chargeAttack.BaseDamage + frames * charge.DamageGrowth,
            BaseKnockback = _chargeAttack.BaseKnockback + frames * charge.KnockbackGrowth,
            Scaling = _chargeAttack.Scaling,
            Direction = _chargeAttack.Direction,
            HitstunFrames = _chargeAttack.HitstunFrames,
            ActiveFrames = _chargeAttack.ActiveFrames,
            Hitboxes = _chargeAttack.Hitboxes,
            Armor = _chargeAttack.Armor,
            Recovery = _chargeAttack.Recovery,
        };
        _fighter.SetChargingFull(false);
        _chargeAttack = null;
        StartAttack(_chargeFiredState, charged);
    }

    private void StartAttack(FighterState state, AttackData attack)
    {
        _fighter.DeactivateShield();

        if (attack.Charge is { } charge && !attack.Id.EndsWith("-charged"))
        {
            _chargeAttack = attack;
            _chargeFiredState = state;
            SetState(FighterState.Charging);
            _attackCooldown = charge.MaxChargeFrames + charge.MaxHoldFrames + DefaultAttackDuration;
            return;
        }

        SetState(state);
        _sequence = attack.Stages.Count > 0
            ? attack.Stages.OrderBy(s => s.SpawnFrame).ToArray()
            : Array.Empty<AttackStage>();
        _nextStageIndex = 0;
        var last = _sequence.LastOrDefault();
        _attackTotalFrames = last != null
            ? last.SpawnFrame + last.ActiveFrames + AttackRecoveryFrames
            : DefaultAttackDuration;
        _attackCooldown = _attackTotalFrames;

        if (_sequence.Length == 0)
        {
            foreach (var spec in attack.Hitboxes)
                _hitboxes.Spawn(_fighter, attack, spec);
            ApplyArmor(attack, attack.ActiveFrames); // flat attack starts at frame 0
        }
    }

    private void SetState(FighterState newState)
    {
        if (_state == newState) return;
        _state = newState;
        _stateFrames = 0;
        if (newState != FighterState.Charging && _chargeAttack != null)
        {
            _chargeAttack = null;
            _fighter.SetChargingFull(false);
        }
        if (newState == FighterState.SpotDodge)
        {
            _fighter.EnterInvincibility(SpotDodgeFrames);
            SetIntangible(true);
        }
        if (newState == FighterState.AirDodge) SetIntangible(true);
    }

    private void SetIntangible(bool on)
    {
        foreach (LimbGroup g in Enum.GetValues<LimbGroup>())
            _fighter.SetHurtboxOverride(g, on ? HurtboxType.Intangible : null);
        _intangibleActive = on;
    }

    private void ApplyArmor(AttackData attack, int expiryFrame)
    {
        if (attack.Armor.Count == 0) return;
        foreach (var a in attack.Armor)
        {
            _fighter.SetHurtboxOverride(a.Group, a.Type, a.BreakKbThreshold ?? float.MaxValue);
            if (!_armoredGroups.Contains(a.Group)) _armoredGroups.Add(a.Group);
        }
        _armorUntilFrame = Math.Max(_armorUntilFrame, expiryFrame);
    }

    // Hitstun can interrupt an armored/intangible window (armor covers some
    // limbs, not all) — never leave stale overrides behind.
    private void ClearCombatOverrides()
    {
        if (_intangibleActive) SetIntangible(false);
        foreach (var g in _armoredGroups) _fighter.SetHurtboxOverride(g, null);
        _armoredGroups.Clear();
        _armorUntilFrame = -1;
    }

    private static bool IsAttackState(FighterState s) =>
        s is FighterState.LightAttack or FighterState.HeavyAttack or FighterState.Special;
}
