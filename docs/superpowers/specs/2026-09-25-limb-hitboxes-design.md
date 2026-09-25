# Limb Hitboxes, Stun-Only Hits & Up-Heavy Motion — Design Spec

Date: 2026-09-25
Status: Approved (pending spec review)

## Goal

Three related combat-feel improvements:

1. **Limb-anchored hitbox chains** — attack hitboxes hug the attacking limb
   like the ultimateframedata gifs (Mario reference:
   https://ultimateframedata.com/hitboxes/mario/MarioSuperJumpPunch.gif),
   instead of hand-placed circles at fixed offsets.
2. **Stun-only hits** — a per-hitbox flag for damage + hitstun with zero
   knockback, as filler for multi-hit attacks.
3. **Grounded up-heavy motion** — the grounded charged up-heavy currently
   fires with no body motion at all; give it a reduced hop so the move reads
   as upward from both stances.

Note on the air recovery: the "recovery doesn't rise" report could not be
reproduced — the boost was verified end-to-end through the real input path
with engine physics (rises ~90px, decays with gravity). If it recurs on a
fresh build, capture a screenshot; the grounded-hop change below addresses
the one confirmed no-rise case (grounded charge release).

Decisions locked in during brainstorming:

| Topic | Decision |
|---|---|
| Hitbox authoring | Limb-anchored chains, auto-derived from limb geometry, following the pose per frame |
| Hit effects | Stun-only (`NoKnockback`) only; grab hitboxes stay specced-but-inert |
| Grounded up-heavy | `RecoveryConfig.GroundedBoost` hop (Byte: 180); air recovery unchanged (380) |

## 1. Limb-anchored hitbox chains

`HitboxSpec` gains:

```csharp
public List<string> LimbChain = new(); // e.g. { "NearUpperArm", "NearForearm", "NearHand" }
public float RadiusScale = 1f;         // radius multiplier vs. the limb-derived default
```

- When `LimbChain` is empty: current behavior (fixed offset circles,
  `OffsetX`/`OffsetY`, following the attacker).
- When set: `HitboxManager.Spawn` creates **one circle per named limb**, each:
  - positioned at the limb's capsule center, using the same geometry rule as
    hurtboxes (`center = size / 2 - pivot`, radius = half the limb thickness
    × `RadiusScale`), as a local offset in limb space;
  - carrying `Limb` + local offset + radius on the `Hitbox` node.
- Every physics frame, an anchored hitbox re-derives its world position from
  the limb's current `GlobalTransform` — circles ride the swing rotation like
  the gifs. Non-anchored hitboxes keep the existing attacker-follow behavior.
- Manual and chained specs can mix within one attack stage (chain for the
  limb + a manual circle for a shockwave).
- `DamageOverride` / `KnockbackOverride` / `NoKnockback` apply per spec and
  are shared by all circles of its chain. The sweet-spot idiom becomes a
  second spec: same chain (or just the hand) with a smaller `RadiusScale`
  and overrides.
- Refits automatically when real art replaces placeholders — same contract
  as hurtboxes.

## 2. Stun-only hits

- `HitboxSpec.NoKnockback` (default false), folded into the effective attack
  in `HitboxManager.EffectiveAttack` (same path as the damage/knockback
  overrides) so `MatchRules.ApplyHit` needs no new parameters.
- `AttackData` gains a transient `public bool NoKnockback;` (not authored
  directly on attacks). `MatchRules.ApplyHit`: when set, skip `ApplyKnockback`
  but keep damage, meter, and hitstun.
- Byte's data is corrected with it (these currently shove the victim out of
  the follow-up): recovery stages 2–3 and special stages 1–2 get
  `NoKnockback = true`.

## 3. Grounded up-heavy hop

- `RecoveryConfig` gains `public float GroundedBoost;` (Byte: 180).
- Grounded charge release of an attack with a `Recovery` config (`FireCharged`
  path): after `StartAttack`, if still grounded, `Velocity = (X, -GroundedBoost)`.
- The air selection path is unchanged (full `VerticalBoost` = 380).
- If the fighter walks off a platform during the charge and releases airborne,
  no boost of either kind is applied by this path (the air-recovery selection
  did not fire) — unchanged from current behavior.

## Testing

xUnit (`tests/ByteBrawl.Tests`, no Godot boot):

- Stun-only resolution: damage applies, hitstun applies, velocity stays zero.
- Grounded charge release of Byte's up-heavy applies `-GroundedBoost`;
  air release via the recovery path still applies `-VerticalBoost`.
- Chain geometry helper (pure function of limb `Size`/`Pivot` → local center +
  radius): segment axes, pivot handling.
- Byte data shape: recovery/special carrying hits carry `NoKnockback`; at
  least one attack stage uses a `LimbChain`.

Node-side (arena smoke + manual visual check against the gif): circles hug
the limb through the swing; tipper sits at the hand.

Verification before commit: `dotnet build`, `dotnet test` (55 + new tests),
both headless smoke scenes.

## Out of scope (this pass)

- Grab hitbox mechanics (data model stays inert; separate issue).
- Per-circle overrides within a chain (one spec = uniform effect).
- Non-limb anchor types (weapon sprites once they exist).
