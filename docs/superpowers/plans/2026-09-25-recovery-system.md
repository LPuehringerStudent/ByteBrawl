# Recovery System Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add air jumping (1 per airtime) and a once-per-airtime recovery attack fired by up-heavy in the air, with a vertical boost and a default attack lockout until grounded.

**Architecture:** Recovery is data, not a new FSM state: `AttackData` gains an optional `RecoveryConfig`, and the existing `FighterStateMachine` gains resource tracking (`_airJumpsUsed`, `_recoveryUsed`, `_attacksLocked`) that resets whenever the fighter is grounded. Byte's `UpHeavy` slot becomes a Mario Super-Jump-Punch-style recovery authored from existing attack stages.

**Tech Stack:** Godot 4.7 (C#), xUnit, .NET with `RollForward=LatestMajor`.

## Global Constraints

- Spec: `docs/superpowers/specs/2026-09-25-recovery-system-design.md` — read it first.
- `src/Combat` must not reference Godot node types (`Vector2` is fine).
- All game-feel numbers (boost, frames) are constants/data in the files shown — do not introduce config indirection.
- Existing 42 tests must stay green; commit messages are conventional (`feat:`, `fix:`).
- Verification before every commit: `dotnet build`, `dotnet test tests/ByteBrawl.Tests`.
- Full verification after the last task additionally runs: `timeout 30 godot --headless --path . res://scenes/smoke_arena.tscn` and `timeout 30 godot --headless --path . res://scenes/smoke_limb_rig.tscn` (both must print `SMOKE PASS`). Headless Godot does not rebuild C# — always `dotnet build` first.

Key current code (post shield-rebind `main`):

- Jump input, `src/Combat/FighterStateMachine.cs` (~line 117):
  `if (actions.JumpPressed && _fighter.IsGrounded) _fighter.Velocity = new Vector2(_fighter.Velocity.X, -_moveset.Stats.JumpSpeed);`
- Attack inputs (~lines 120-122): three `if (actions.AttackX && _attackCooldown == 0) { StartAttack(...); return; }` lines firing `NeutralLight`/`NeutralHeavy`/`NeutralSpecial`.
- Grounded reset point: none yet — the grounded movement branch is at the bottom of `Update`, but attack/dodge/shield states return early, so the reset must live at the top of `Update`.
- `FakeFighter` (`tests/ByteBrawl.Tests/TestDoubles.cs`) exposes `Grounded` (settable) — set `f.Grounded = false` for airborne tests.
- `Moveset.Get(slot)` is `Attacks[slot]` (throws if missing); test movesets assign `moveset.Attacks[AttackSlot.X] = attack` directly.

---

### Task 1: Air jump

**Files:**
- Modify: `src/Combat/FighterStats.cs`
- Modify: `src/Combat/FighterStateMachine.cs` (jump input line + fields + top-of-update reset)
- Test: `tests/ByteBrawl.Tests/FighterStateMachineMovementTests.cs`

**Interfaces:**
- Consumes: `Moveset.Stats`, `ActionFrame.JumpPressed`, `IFighter.IsGrounded`/`Velocity`.
- Produces:
  - `int FighterStats.AirJumps = 1;`
  - FSM behavior: airborne `JumpPressed` with jumps remaining (and not in an early-return state) sets `Velocity.Y = -Stats.JumpSpeed` and consumes one jump; the counter resets whenever `IsGrounded` is true at the top of `Update` (covers landing, grounded states, and grounded hitstun — hitstun deliberately does NOT refresh mid-air).

- [ ] **Step 1: Write the failing test**

Append to `tests/ByteBrawl.Tests/FighterStateMachineMovementTests.cs` (inside the class; it already has `NewFsm(bool grounded)` and a `Neutral()` helper):

```csharp
    [Fact] public void AirJump_AppliesUpwardVelocityAndConsumes()
    {
        var (fsm, f) = NewFsm(false);
        fsm.Update(Neutral() with { JumpPressed = true });
        Assert.Equal(-280, f.Velocity.Y, 0.01f); // JumpSpeed from ByteMoveset stats
        f.Velocity = new Vector2(0, 100); // simulate falling again
        fsm.Update(Neutral() with { JumpPressed = true }); // second press: spent
        Assert.Equal(100, f.Velocity.Y, 0.01f);
    }

    [Fact] public void Landing_ResetsAirJumpCounter()
    {
        var (fsm, f) = NewFsm(false);
        fsm.Update(Neutral() with { JumpPressed = true }); // spend the air jump
        f.Grounded = true;
        fsm.Update(Neutral()); // land: counter resets
        f.Grounded = false;
        f.Velocity = new Vector2(0, 100);
        fsm.Update(Neutral() with { JumpPressed = true });
        Assert.Equal(-280, f.Velocity.Y, 0.01f);
    }

    [Fact] public void Hitstun_DoesNotRefreshAirJump()
    {
        var (fsm, f) = NewFsm(false);
        fsm.Update(Neutral() with { JumpPressed = true }); // spend it
        f.HitstunFrames = 3; // get hit mid-air (counter must stay spent)
        fsm.Update(Neutral());
        f.HitstunFrames = 0;
        fsm.Update(Neutral());
        f.Velocity = new Vector2(0, 100);
        fsm.Update(Neutral() with { JumpPressed = true });
        Assert.Equal(100, f.Velocity.Y, 0.01f);
    }

    [Fact] public void AirJump_BlockedDuringAttack()
    {
        var (fsm, f) = NewFsm(false);
        fsm.Update(Neutral() with { AttackLight = true });
        Assert.Equal(FighterState.LightAttack, fsm.CurrentState);
        f.Velocity = new Vector2(0, 100);
        fsm.Update(Neutral() with { JumpPressed = true });
        Assert.Equal(100, f.Velocity.Y, 0.01f);
    }
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/ByteBrawl.Tests --filter "FullyQualifiedName~MovementTests"`
Expected: FAIL to compile — `FighterStats.AirJumps` does not exist.

- [ ] **Step 3: Write minimal implementation**

`src/Combat/FighterStats.cs` — add the field:

```csharp
    public float JumpSpeed;
    public int AirJumps = 1;
```

`src/Combat/FighterStateMachine.cs` — add fields next to the other private fields:

```csharp
    private int _airJumpsUsed;
```

At the top of `Update` (right after the `_attackCooldown` decrement), add the grounded reset:

```csharp
        if (_fighter.IsGrounded)
        {
            _airJumpsUsed = 0;
            _recoveryUsed = false;
            _attacksLocked = false;
        }
```

(`_recoveryUsed` / `_attacksLocked` don't exist yet — declare them too so this compiles; they are wired up in Tasks 2 and 3:

```csharp
    private bool _recoveryUsed;
    private bool _attacksLocked;
```

)

Replace the grounded-only jump line with:

```csharp
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
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet build && dotnet test tests/ByteBrawl.Tests`
Expected: PASS — 46 tests (the Task 2/3 fields compile but behave as no-ops).

- [ ] **Step 5: Commit**

```bash
git add src/Combat/FighterStats.cs src/Combat/FighterStateMachine.cs tests/ByteBrawl.Tests/FighterStateMachineMovementTests.cs
git commit -m "feat: air jump with once-per-airtime counter"
```

---

### Task 2: RecoveryConfig + air up-heavy selection

**Files:**
- Create: `src/Combat/RecoveryConfig.cs`
- Modify: `src/Combat/AttackData.cs`
- Modify: `src/Combat/FighterStateMachine.cs` (`FireCharged` copy + heavy attack branch)
- Test: `tests/ByteBrawl.Tests/FighterStateMachineAttackTests.cs`

**Interfaces:**
- Consumes: Task 1 fields; `Moveset.Get`, `AttackSlot.UpHeavy`, `FakeHitboxManager.Spawns`.
- Produces:
  - `class RecoveryConfig { public float VerticalBoost; public bool CanActAfter = false; }`
  - `AttackData.Recovery` (`RecoveryConfig?`, default null); `FireCharged` preserves it.
  - FSM: airborne + `AttackHeavy` + `MoveY < 0` → fires `Moveset.Get(AttackSlot.UpHeavy)`; if it has a `Recovery` config: `Velocity = (Velocity.X, -VerticalBoost)`, `_recoveryUsed = true`. If `_recoveryUsed` is already true, or the attack has no config-fired path, the input is consumed either way (`return` after the up-heavy branch so a spent recovery doesn't fall through to neutral heavy).

- [ ] **Step 1: Write the failing test**

Append to `tests/ByteBrawl.Tests/FighterStateMachineAttackTests.cs`:

```csharp
    private static AttackData RecoveryAttack(bool canActAfter = false) => new()
    {
        Id = "test-recovery", BaseDamage = 6, BaseKnockback = 150, Scaling = 1f,
        Direction = new Vector2(0, -1), HitstunFrames = 10, ActiveFrames = 4,
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
        for (var i = 0; i < 25; i++) fsm.Update(Neutral()); // flat attack: 20 frames total, then Fall
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
```

Note: `NewFsm()` in this file returns `(FighterStateMachine, FakeFighter, FakeHitboxManager)` and its `FakeFighter` is grounded by default — reuse it for the grounded test.

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/ByteBrawl.Tests --filter "FullyQualifiedName~AttackTests"`
Expected: FAIL to compile — `RecoveryConfig`/`AttackData.Recovery` do not exist.

- [ ] **Step 3: Write minimal implementation**

Create `src/Combat/RecoveryConfig.cs`:

```csharp
namespace ByteBrawl.Combat;

// Marks an attack as a recovery: fired in the air with up+heavy, once per
// airtime, with an initial vertical boost. CanActAfter opts out of the
// post-recovery attack lockout (combo-finisher archetype).
public class RecoveryConfig
{
    public float VerticalBoost;
    public bool CanActAfter = false;
}
```

`src/Combat/AttackData.cs` — add next to `Charge`:

```csharp
    public RecoveryConfig? Recovery;
```

`src/Combat/FighterStateMachine.cs` — in `FireCharged`, add to the charged attack initializer (next to `Armor = _chargeAttack.Armor`):

```csharp
            Recovery = _chargeAttack.Recovery,
```

Replace the neutral heavy attack line:

```csharp
        if (actions.AttackHeavy && _attackCooldown == 0) { StartAttack(FighterState.HeavyAttack, _moveset.Get(AttackSlot.NeutralHeavy)); return; }
```

with:

```csharp
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
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet build && dotnet test tests/ByteBrawl.Tests`
Expected: PASS — 51 tests.

- [ ] **Step 5: Commit**

```bash
git add src/Combat/RecoveryConfig.cs src/Combat/AttackData.cs src/Combat/FighterStateMachine.cs tests/ByteBrawl.Tests/FighterStateMachineAttackTests.cs
git commit -m "feat: recovery config and air up-heavy selection"
```

---

### Task 3: Post-recovery attack lockout

**Files:**
- Modify: `src/Combat/FighterStateMachine.cs` (attack input guard)
- Test: `tests/ByteBrawl.Tests/FighterStateMachineAttackTests.cs`

**Interfaces:**
- Consumes: `_attacksLocked` set in Task 2; resets at the top of `Update` (Task 1).
- Produces: while `_attacksLocked` is true, light/heavy/special attack inputs are ignored; grounded contact clears the flag (existing reset), so the lockout only binds in the air.

- [ ] **Step 1: Write the failing test**

Append to `tests/ByteBrawl.Tests/FighterStateMachineAttackTests.cs`:

```csharp
    [Fact] public void Lockout_BlocksFollowUpAttacksInAir()
    {
        var (fsm, f, hb) = NewRecoveryFsm();
        fsm.Update(Neutral() with { AttackHeavy = true, MoveY = -1 }); // default: CanActAfter = false
        for (var i = 0; i < 25; i++) fsm.Update(Neutral()); // attack ends airborne
        Assert.Equal(FighterState.Fall, fsm.CurrentState);
        fsm.Update(Neutral() with { AttackLight = true });
        Assert.Equal(FighterState.Fall, fsm.CurrentState); // locked
    }

    [Fact] public void Lockout_ClearsOnLanding()
    {
        var (fsm, f, hb) = NewRecoveryFsm();
        fsm.Update(Neutral() with { AttackHeavy = true, MoveY = -1 });
        for (var i = 0; i < 25; i++) fsm.Update(Neutral());
        f.Grounded = true;
        fsm.Update(Neutral()); // land: lockout clears
        fsm.Update(Neutral() with { AttackLight = true });
        Assert.Equal(FighterState.LightAttack, fsm.CurrentState);
    }

    [Fact] public void CanActAfter_SkipsLockout()
    {
        var (fsm, f, hb) = NewRecoveryFsm(canActAfter: true);
        fsm.Update(Neutral() with { AttackHeavy = true, MoveY = -1 });
        for (var i = 0; i < 25; i++) fsm.Update(Neutral());
        fsm.Update(Neutral() with { AttackLight = true });
        Assert.Equal(FighterState.LightAttack, fsm.CurrentState); // combo-finisher: free follow-up
    }
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/ByteBrawl.Tests --filter "FullyQualifiedName~AttackTests.Lockout|FullyQualifiedName~CanActAfter"`
Expected: FAIL — `Lockout_BlocksFollowUpAttacksInAir` fails (follow-up attack fires; nothing guards attack inputs yet). The other two may pass incidentally.

- [ ] **Step 3: Write minimal implementation**

`src/Combat/FighterStateMachine.cs` — wrap the three attack-input checks in a lockout guard. Replace:

```csharp
        if (actions.AttackLight && _attackCooldown == 0) { StartAttack(FighterState.LightAttack, _moveset.Get(AttackSlot.NeutralLight)); return; }
```

with:

```csharp
        if (_attacksLocked) { /* recovery lockout: no attacks until grounded */ }
        else if (actions.AttackLight && _attackCooldown == 0) { StartAttack(FighterState.LightAttack, _moveset.Get(AttackSlot.NeutralLight)); return; }
```

and demote the heavy/special `if`s to `else if` so the guard covers all three. (The heavy block from Task 2 becomes the middle `else if` branch; its body unchanged.)

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet build && dotnet test tests/ByteBrawl.Tests`
Expected: PASS — 54 tests.

- [ ] **Step 5: Commit**

```bash
git add src/Combat/FighterStateMachine.cs tests/ByteBrawl.Tests/FighterStateMachineAttackTests.cs
git commit -m "feat: post-recovery attack lockout with combo-finisher opt-out"
```

---

### Task 4: Byte's recovery data (SJP-style)

**Files:**
- Modify: `src/Data/ByteMoveset.cs`
- Test: `tests/ByteBrawl.Tests/CombatDataTests.cs`

**Interfaces:**
- Consumes: `RecoveryConfig`, `AttackStage`, `ArmorSpec` (all previous tasks).
- Produces: `Moveset.Get(AttackSlot.UpHeavy)` returns a 4-stage recovery (`Id = "sword-upheavy-recovery"`, `Recovery.VerticalBoost = 380`, `CanActAfter = false`, hyper armor on Arm during the ascent stages). The other three heavy slots keep the chargeable `swordHeavy`.

- [ ] **Step 1: Write the failing test**

Append to `tests/ByteBrawl.Tests/CombatDataTests.cs` (the file already has `using Godot;`):

```csharp
    [Fact]
    public void ByteUpHeavy_IsSjpStyleRecovery()
    {
        var upHeavy = ByteMoveset.Create().Get(AttackSlot.UpHeavy);
        Assert.NotNull(upHeavy.Recovery);
        Assert.Equal(380, upHeavy.Recovery!.VerticalBoost, 0.01f);
        Assert.False(upHeavy.Recovery.CanActAfter);
        Assert.Equal(4, upHeavy.Stages.Count);
        // strong first hit, two carrying hits (no scaling), launcher last
        Assert.True(upHeavy.Stages[0].BaseKnockback > upHeavy.Stages[1].BaseKnockback);
        Assert.Equal(0, upHeavy.Stages[1].Scaling);
        Assert.Equal(0, upHeavy.Stages[2].Scaling);
        Assert.True(upHeavy.Stages[3].BaseKnockback > upHeavy.Stages[1].BaseKnockback);
        // hyper armor on the arm while rising (stages 1-3)
        for (var i = 0; i < 3; i++)
        {
            var armor = Assert.Single(upHeavy.Stages[i].Armor);
            Assert.Equal(LimbGroup.Arm, armor.Group);
            Assert.Equal(HurtboxType.HyperArmor, armor.Type);
        }
        Assert.Empty(upHeavy.Stages[3].Armor);
    }
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/ByteBrawl.Tests --filter "FullyQualifiedName~CombatDataTests"`
Expected: FAIL — Byte's `UpHeavy` is currently the chargeable `swordHeavy` (no recovery, no stages).

- [ ] **Step 3: Write minimal implementation**

`src/Data/ByteMoveset.cs` — add the recovery attack after the `swordHeavy` declaration (it can reference `LimbGroup`/`HurtboxType` — the file already has `using ByteBrawl.Combat;`):

```csharp
        var upHeavyRecovery = new AttackData
        {
            Id = "sword-upheavy-recovery",
            Recovery = new RecoveryConfig { VerticalBoost = 380 },
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
```

Then, after the existing heavy-slot loop (`foreach (var slot in new[] { AttackSlot.NeutralHeavy, AttackSlot.UpHeavy, AttackSlot.DownHeavy, AttackSlot.SideHeavy }) m.Attacks[slot] = swordHeavy;`), override the one slot:

```csharp
        m.Attacks[AttackSlot.UpHeavy] = upHeavyRecovery;
```

(place it right after the heavy loop, before the special loop — position among the loops is cosmetic but keep it adjacent to the heavy assignments).

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet build && dotnet test tests/ByteBrawl.Tests`
Expected: PASS — 55 tests.

- [ ] **Step 5: Commit**

```bash
git add src/Data/ByteMoveset.cs tests/ByteBrawl.Tests/CombatDataTests.cs
git commit -m "feat: byte up-heavy recovery with rising multi-hit"
```

---

### Task 5: Final verification

- [ ] **Step 1: Full check**

```bash
dotnet build && dotnet test tests/ByteBrawl.Tests \
  && timeout 30 godot --headless --path . res://scenes/smoke_arena.tscn \
  && timeout 30 godot --headless --path . res://scenes/smoke_limb_rig.tscn
```

Expected: 55/55 tests, both smokes `SMOKE PASS`. Fix and amend before reporting done if anything fails.

- [ ] **Step 2: Update the technical docs**

`docs/combat/fighter-state-machine.md` — add a bullet to the state list (after `AirDodge`):

```markdown
- `HeavyAttack` (air, up) — the recovery: air + heavy + holding up fires the
  moveset's `UpHeavy` slot once per airtime with a vertical boost (`RecoveryConfig`);
  other attacks are locked until you land unless the attack opts out
  (`CanActAfter`). See `docs/superpowers/specs/2026-09-25-recovery-system-design.md`.
```

and mention the air-jump rule in the `Jump`/`Fall` bullet: `Jump` while airborne
consumes the fighter's air jumps (`FighterStats.AirJumps`, Byte: 1), reset on
landing; hitstun does not refresh it.

Commit: `git add docs/combat/fighter-state-machine.md && git commit -m "docs: recovery and air jump in state machine reference"`

- [ ] **Step 3: Manual feel check (report to user, do not automate)**

Off-stage test: knock yourself off, air up-heavy (K + hold W) should launch you
back with purple arm armor; a second up-heavy mid-air does nothing; attacks stay
locked until you land; one air jump (Space) is available independently before or
after. Numbers (boost 380, stage frames 2/7/12/17) are first guesses — tune in
`ByteMoveset.cs` / `FighterStateMachine.cs` after feel-testing.
