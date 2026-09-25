# Typed Hitboxes & Hurtboxes — Design Spec

Date: 2026-09-25
Status: Approved (pending spec review)

## Goal

Upgrade the flat "one damage circle vs. capsule" model to a typed system covering
Smash-style hitbox behaviors and per-limb hurtbox states. Debug rendering follows
the ultimateframedata.com convention so types are readable at a glance.

Decisions locked in during brainstorming:

| Topic | Decision |
|---|---|
| Scope | Armor types, intangible + invincible, wind + grab, search — all implemented as data/logic |
| Armor granularity | Per limb group per attack stage (Little Mac style: only the attacking arm is purple) |
| Grab mechanics | Full grab system **designed and specced**, mechanics **not built** this pass |
| Debug colors | Smash convention (see table below) |

## Data model (`src/Combat`, plain C#, no Godot node types)

New enums:

```csharp
public enum HitboxType { Damage, Wind, Grab, Search }
public enum HurtboxType { Vulnerable, Intangible, Invincible, SuperArmor, HyperArmor }
public enum LimbGroup { Head, Torso, Arm, Leg }
```

- `HitboxSpec` gains `Type` (default `Damage`) plus per-type payload:
  - `Wind`: `Push` (Vector2) — applied to victim velocity, no damage/hitstun.
  - `Search`: `SearchId` (string) — detection-only; fires a callback so future
    counters/triggers can subscribe. No built-in effect.
  - `Grab`: references `GrabData` (see below). Grab hitboxes render in debug but
    **immediately expire without effect** — the grab subsystem is specced, not built.
- `AttackStage` gains `Armor`: a list of `(LimbGroup, HurtboxType, float? BreakKbThreshold)`
  entries active **only during that stage's frames** (including `SpawnFrame` timing
  as today). Authoring example: Byte's charged heavy final stage arms
  `(Arm, HyperArmor, null)` so the punching arm glows purple per the reference gif.
- `HurtboxType` semantics:
  - `Vulnerable` — default; hit resolves normally.
  - `Intangible` — no contact at all; attack whiffs through, no clank/hitstop.
  - `Invincible` — contact registers (blocked flash) but zero damage/knockback/hitstun.
  - `SuperArmor` — damage applies, no knockback/hitstun/interrupt; **breaks** if the
    attack's base knockback exceeds the entry's threshold, resolving as a normal hit.
  - `HyperArmor` — damage applies, no knockback/hitstun/interrupt; never breaks.

### Limb group mapping

The 15 hurtboxes (backpack stays inert) map onto four groups:

- `Head` — Head
- `Torso` — Torso, Pelvis
- `Arm` — Near/Far UpperArm, Forearm, Hand
- `Leg` — Near/Far Thigh, Shin, Foot

Mapping is by part-name prefix/suffix table in `LimbRig.AttachHurtbox`, assigned once
at rig build time. Real art swaps in later without touching this.

## Resolution flow

- `Hurtbox` (node) gains `Group` (LimbGroup) and `CurrentType` (default Vulnerable).
- `Fighter` owns a per-group type override table the FSM writes:
  - dodges (spot/roll/air) → all groups `Intangible` during the dodge's invulnerable frames
  - respawn → all groups `Invincible` for a fixed window
  - attack stage activation → applies/clears that stage's `Armor` entries
  These replace the current hardcoded invulnerability flags in FSM/Rules.
- `HitboxManager` overlap → reads the touched hurtbox's `CurrentType`:
  - `Intangible` → skip silently.
  - `Invincible` → emit blocked event (flash only).
  - `SuperArmor` → if attack base kb > threshold: normal hit; else damage-only hit.
  - `HyperArmor` → damage-only hit.
- Damage-only hits (armor) apply damage and meter gain but no knockback, hitstun, or
  state interrupt; the victim's current state continues. Hitstop applies as normal.

## Grab system — specced, not built

Full data model lands now, validated but inert:

```csharp
public class GrabData {
    public int HoldFrames;            // victim held before throw executes
    public float PummelDamage;        // damage per pummel hit
    public int PummelInterval;        // frames between pummels
    public float MashOutPerInput;     // hold frames removed per victim mash input
    public int MashOutMax;            // cap on mash reduction
    public ThrowData ThrowUp, ThrowForward, ThrowDown;  // each: Damage, BaseKb, Scaling, Angle
}
```

Until the subsystem gets its own plan: `Grab`-typed specs spawn a debug-visible box
that expires on the first tick with no gameplay effect.

## Debug rendering (`CombatDebugDraw`)

Keeps the near-transparent fill style and `ZIndex = 100` (drawn above the model).
F3 still toggles the overlay; pause-menu toggles unchanged.

| Element | Color |
|---|---|
| Hitbox — Damage | Red, brighter on early active frames, fading darker |
| Hitbox — Wind | Cyan |
| Hitbox — Grab | Yellow |
| Hitbox — Search | Gray |
| Hurtbox — Vulnerable | Pale green (current look) |
| Hurtbox — Intangible | Faint blue |
| Hurtbox — Invincible | Blinking white |
| Hurtbox — SuperArmor | Orange |
| Hurtbox — HyperArmor | Purple |

## Testing

xUnit (`tests/ByteBrawl.Tests`), plain C# fakes, no Godot boot:

- Each hurtbox type's resolution outcome (vulnerable hit / intangible whiff /
  invincible block / super-armor damage-only / super-armor threshold break /
  hyper-armor damage-only).
- Stage armor applies on the stage's spawn frame and clears afterward.
- Wind pushes without damage or hitstun.
- Grab data model validation (non-negative frames, throw data present).
- Existing 23 tests stay green.

Verification before commit: `dotnet build`, `dotnet test`, both headless smoke
scenes (`smoke_arena.tscn`, `smoke_limb_rig.tscn`).

## Out of scope (this pass)

- Grab/throw/pummel/mash-out mechanics (data model only).
- Element/attribute types (fire, electric — visual flair).
- Transcendent hitboxes (only matter with projectile clashes; no projectiles yet).
- Reflectors / absorbers (no projectiles yet).
