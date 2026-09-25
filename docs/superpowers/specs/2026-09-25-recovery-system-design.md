# Recovery System — Design Spec

Date: 2026-09-25
Status: Approved (pending spec review)

## Goal

Give fighters a way to come back to the stage when knocked off. Air **up-heavy**
is the recovery attack: primary purpose is returning to the stage; a secondary
combo-finisher role exists for specific moveset designs. Also introduces air
jumping, which the game does not have today.

Reference for the default recovery feel: Mario's Super Jump Punch — vertical
boost, strong first hit, rapid weak multi-hits during the ascent, launcher at
the top (https://ultimateframedata.com/hitboxes/mario/MarioSuperJumpPunch.gif).

Decisions locked in during brainstorming:

| Topic | Decision |
|---|---|
| Air jumps | 1 per airtime (reset on landing/respawn; being hit does NOT refresh) |
| Recovery usage | Once per airtime; air jump remains available after using recovery |
| Attacks after recovery | Locked until grounded by default; per-move opt-out (`CanActAfter`) |
| Move shape | Vertical boost + rising multi-stage hit (SJP-style), authored as data |
| Approach | A: recovery as data on the existing attack/stage system (no bespoke FSM state) |

## Data model (`src/Combat`)

- `FighterStats` gains `public int AirJumps = 1;` (per-character later; the
  counter itself lives in the FSM, not the stats).
- `AttackData` gains `public RecoveryConfig? Recovery;`:

```csharp
public class RecoveryConfig
{
    public float VerticalBoost;        // initial upward velocity on activation
    public bool CanActAfter = false;   // combo-finisher opt-out: skip attack lockout
}
```

  Null = a normal attack. Charged copies preserve it alongside `Armor`.

- Byte's `UpHeavy` slot becomes the recovery (SJP-style):
  - `Recovery = { VerticalBoost = 380, CanActAfter = false }`
  - Stage 1 (frame 2): strong first hit (high damage/knockback, launches up-forward).
  - Stages 2–4 (spaced frames during ascent): weak rapid hits, `Scaling = 0`
    (carry the opponent up with you, no launch).
  - Final stage at the top: launcher.
  - `Armor = [(Arm, HyperArmor)]` during the ascent stages, mirroring the
    grounded heavy — recovery shouldn't be trivially edge-guarded.
  - Other heavy slots stay grounded-only (see below); `NeutralHeavy` keeps its
    charge behavior.

## FSM rules (`FighterStateMachine`)

**Air jump.** `JumpPressed` while airborne, `_airJumpsUsed < Stats.AirJumps`,
and not in attack/dodge/charge/shield/hitstun → `Velocity = (Velocity.X,
-JumpSpeed)`, `_airJumpsUsed++`. Ground contact resets the counter to 0.
Hitstun does not refresh it (Smash behavior — no double-jump refresh). The
existing early-return states (attack, charge, dodge, shield, hitstun) naturally
block air jumps without extra checks.

**Recovery selection.** Airborne + `AttackHeavy` + `MoveY < 0` (holding up):
- If `_recoveryUsed` is false: fire the moveset's `UpHeavy` attack. On
  activation, `Velocity = (Velocity.X, -Recovery.VerticalBoost)` and
  `_recoveryUsed = true`. Small horizontal drift is allowed to persist; the
  boost is vertical.
- If `_recoveryUsed` is true: the input is ignored entirely (no whiff
  animation — the input eats itself, Smash-style).
- Grounded heavy behavior is unchanged (neutral chargeable heavy; full
  directional selection remains issue #2 — recovery adds only the up-in-air
  slice).

`_recoveryUsed` resets when the fighter becomes grounded. Respawn drops the
fighter above the stage, so the first ground contact after respawn clears the
flag — no separate respawn hook needed (and it stays correct even once issue
#8 adds a proper FSM reset). It survives hitstun: getting hit off-stage after
burning your recovery is punishing, by design.

**Attack lockout.** After a recovery with `CanActAfter == false`, all attack
inputs (light/heavy/special) are ignored until grounded. Recoveries with
`CanActAfter = true` skip the lockout, enabling the combo-finisher archetype.
Lockout state clears on grounded; it never applies to grounded attacks.

**Stages and motion need no special casing.** Gravity in `Fighter._PhysicsProcess`
decelerates the boost naturally; multi-stage hitboxes spawn relative to the
fighter and follow it up; armor windows use the existing per-stage `Armor`
mechanism; air drag/charge logic does not apply (recovery is not chargeable).

## Testing

xUnit on the FSM (`FakeFighter`, plain movesets):

- Air jump consumes the counter; second press in the same airtime does nothing.
- Landing resets the air-jump counter; hitstun does not.
- Air jump is unavailable during attack/dodge states.
- Air up-heavy fires the `UpHeavy` attack, applies `-VerticalBoost`, and marks
  recovery used.
- Second recovery attempt in the same airtime is ignored.
- Attack inputs after a default recovery are ignored until grounded; a
  `CanActAfter = true` recovery allows immediate follow-ups.
- Byte data: `UpHeavy` carries a `Recovery` config and the multi-stage
  ascending pattern.

Verification before commit: `dotnet build`, `dotnet test`, both headless smoke
scenes (`smoke_arena.tscn`, `smoke_limb_rig.tscn`).

## Out of scope (this pass)

- Full directional attack selection (issue #2) — recovery adds only up-heavy-in-air.
- Ledge/edge-guard interactions (no ledges in the stage yet).
- Double-jump-refresh-on-hit rules (decided: none, but revisit with playtesting).
- Per-character air-jump counts beyond the `FighterStats` field.
