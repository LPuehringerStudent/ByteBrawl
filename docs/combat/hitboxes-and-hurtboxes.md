# Hitboxes & Hurtboxes

How attack hitboxes and body hurtboxes work in ByteBrawl: the data model, the
per-frame resolution flow, per-limb armor, and the debug overlay. For the design
rationale see `docs/superpowers/specs/2026-09-25-hit-hurtbox-types-design.md`.

## Two kinds of boxes

**Hitboxes** are what an attack spawns. They are **circles only** (Smash style) —
several circles per attack give capsule-like coverage along a limb (elbow circle +
fist circle) or sweet/sour spots.

**Hurtboxes** are what a body part exposes. Every limb of the 16-part rig carries a
capsule (`CapsuleShape2D`) auto-derived from the limb's sprite size/pivot, so they
refit automatically when real art replaces the placeholders. Never hand-place
hurtboxes.

## Types

### Hitbox types (`HitboxType`, `src/Combat/BoxTypes.cs`)

| Type | Behavior |
|---|---|
| `Damage` | Normal hit: damage + knockback + hitstun. Default. |
| `Wind` | Pushes the victim (`HitboxSpec.Push`, X flipped by attacker facing) every overlapping frame. No damage, hitstun, or meter. |
| `Grab` | **Specced but unbuilt.** Box renders in debug, has no contact handler. The `GrabData`/`ThrowData` model (`src/Combat/GrabData.cs`) is validated but inert until the grab subsystem lands. |
| `Search` | Detection only: fires `HitboxManager.SearchTriggered` once per box on contact. No built-in effect — intended for counters/triggers. |

### Hurtbox types (`HurtboxType`)

| Type | Semantics |
|---|---|
| `Vulnerable` | Default. Hit resolves normally. |
| `Intangible` | No contact at all — the attack whiffs through silently (no clank, no hitstop). Set on all limb groups during spot/air dodges. |
| `Invincible` | Contact registers but deals nothing. `MatchRules` raises `HitBlocked` (the debug overlay draws a white flash ring). Used for respawn. |
| `SuperArmor` | Damage applies, no knockback/hitstun/interrupt. **Breaks** if the attack's base knockback exceeds the armor's threshold. |
| `HyperArmor` | Same as super armor but never breaks. Byte's heavy uses this on its arm. |

## Limb groups

Hurtbox types are set **per limb group**, not per fighter (`LimbGroup`):
`Head` (Head), `Torso` (Torso + Pelvis), `Arm` (both arms, 6 capsules),
`Leg` (both legs, 6 capsules). The backpack has no hurtbox. The name mapping lives
in `LimbRig.GroupFor` — note it matches `arm` case-insensitively so "Forearm"
counts (this has bitten once already).

## Per-frame resolution flow

```
Fighter._PhysicsProcess
  └─ Fsm.Update(actions)                    (plain C#, src/Combat/FighterStateMachine.cs)
       └─ on stage spawn: HitboxManager.Spawn(fighter, stage, spec)
            └─ Hitbox node created (CircleShape2D, lives attack.ActiveFrames)
  └─ MoveAndSlide()                         (Godot physics moves everything)
  └─ HitboxManager._PhysicsProcess          (ticks down FramesRemaining)

Overlap (Godot Area2D signal, any physics frame):
  Hitbox.AreaEntered → Hurtbox
    ├─ defender == attacker?     → ignore
    ├─ Hurtbox.CurrentType == Intangible? → ignore (whiff)
    └─ switch HitboxSpec.Type:
         Wind   → MatchRules.ApplyWind (push, no HasHit — pushes every frame)
         Search → fire SearchTriggered once
         Damage → MatchRules.ApplyHit(attacker, defender, effectiveAttack,
                    hurtbox.CurrentType, hurtbox.ArmorBreakKb), once per box
```

`MatchRules.ApplyHit` (`src/Combat/MatchRules.cs`) then decides, in order:
match over? → intangible? → invincible (flag or type) → raise `HitBlocked` →
meter gain (0.6× dealt / 0.4× taken) → shield branch (3× shield damage, 0.3×
knockback) → armor check (damage-only vs. full hit) → damage + scaled knockback +
hitstun.

"Effective attack": a `HitboxSpec` may carry `DamageOverride`/`KnockbackOverride`
for sweet spots (e.g. Byte's special stage 3 fist tipper: 7 dmg / 210 kb vs. the
base 5/180). `HitboxManager.EffectiveAttack` folds those into the attack passed to
`ApplyHit` — so the armor break threshold compares against the *effective* base
knockback too.

## Who sets hurtbox types

The `FighterStateMachine` drives everything through one seam:
`IFighter.SetHurtboxOverride(LimbGroup, HurtboxType?, armorBreakKb)`. `Fighter`
maps groups onto its rig's `Hurtbox` nodes; the xUnit `FakeFighter` records calls
in a dictionary. `null` resets a group to `Vulnerable`.

| Source | Groups | Type | Window |
|---|---|---|---|
| Spot dodge / air dodge | all 4 | `Intangible` | dodge duration (20 frames) |
| Respawn (`MatchRules.CheckRingOut`) | — (flag) | `Invincible` via `InvincibleFrames` | `MatchRulesConfig.RespawnInvincibilityFrames` |
| `AttackData.Armor` (flat attacks) | per entry | per entry (`HyperArmor`/`SuperArmor`) | `ActiveFrames` from attack start |
| `AttackStage.Armor` (staged attacks) | per entry | per entry | stage's `ActiveFrames` from `SpawnFrame` |

Two safety nets keep state from leaking: armor expires by frame count in
`TickAttack`, and entering hitstun calls `ClearCombatOverrides()` — necessary
because armor only covers *some* limbs; a clean hit to an unarmored limb
interrupts the window. Charged attacks preserve armor because `FireCharged`
copies `Armor` into the charged `AttackData`.

The dodge's old `InvincibleFrames` flag is kept alongside `Intangible`:
`InvincibleFrames` is the damage gate (asserted by existing tests), `Intangible`
is the contact gate (what the hitbox manager checks first).

## Debug overlay (`CombatDebugDraw`, F3 or the Esc pause menu)

Smash-convention colors, near-transparent fills, drawn at `ZIndex = 100` (above
the fighters):

- Hitboxes: **red** damage (bright on early active frames, fading darker), **cyan**
  wind, **yellow** grab, **gray** search. Damage boxes also draw a yellow
  knockback-direction line.
- Hurtboxes: **pale green** vulnerable, **faint blue** intangible, **blinking
  white** invincible (also shown while `InvincibleFrames > 0`), **orange**
  super armor, **purple** hyper armor — the last one matches the Ultimate
  framedata convention (Little Mac's armored smash).

## Adding a new hitbox behavior

1. Add the enum value in `src/Combat/BoxTypes.cs`.
2. Add per-type payload to `HitboxSpec` (like `Push` for wind).
3. Add the outcome branch in `MatchRules` (keep it plain C# — write the xUnit
   test first; see `MatchRulesTests`).
4. Add the dispatch case in `HitboxManager.Spawn`'s `AreaEntered` handler.
5. Add a color to `CombatDebugDraw.HitboxFill`.
6. If it needs per-limb victim state, extend `HurtboxType` and the override
   plumbing instead of adding flags — the FSM/dodge/armor machinery reuses it.
