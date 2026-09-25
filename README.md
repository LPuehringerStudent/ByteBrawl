# ByteBrawl

A local 1v1 2D platform fighter for the browser… no wait, for the desktop — it's a
**Godot 4.7 (C#)** game now. Two players share one keyboard, fight on one stage,
until someone runs out of stocks or the timer expires. Current state: playable MVP
core (movement, three attack buttons with charge, shield/dodges, damage/knockback,
stocks, training mode) with placeholder box-art characters.

## Requirements

- **Godot 4.7+ with .NET support** (the `mono` build). This is the #1 onboarding
  trap: the plain (non-.NET) Godot build opens the project but fails to load every
  C# script with `No loader found for resource: res://src/Nodes/Main.cs`. If you see
  that error, you have the wrong Godot build.
- **.NET SDK 8 or newer** (`dotnet` on PATH). The test project sets
  `RollForward=LatestMajor`, so a newer SDK/runtime works.

## Quickstart

```bash
git clone https://github.com/LPuehringerStudent/ByteBrawl.git
cd ByteBrawl

# Run the game (opens the boot menu: Local Versus / Training)
godot --path .

# Or open the editor
godot --path . --editor
```

### Verify your setup

```bash
dotnet build                      # compile check
dotnet test tests/ByteBrawl.Tests # 40 unit tests, no Godot needed

# Headless smoke scenes (require a successful dotnet build FIRST —
# headless Godot does not rebuild C# on its own):
timeout 30 godot --headless --path . res://scenes/smoke_arena.tscn
timeout 30 godot --headless --path . res://scenes/smoke_limb_rig.tscn
# each must print SMOKE PASS
```

### Controls (two players, one keyboard)

| Action | Player 1 | Player 2 |
|---|---|---|
| Move | A / D | ← / → |
| Up / Down | W / S | ↑ / ↓ |
| Jump | Space | Numpad 5 |
| Light attack | J | Numpad 1 |
| Heavy attack (hold to charge) | K | Numpad 2 |
| Special attack (multi-hit) | H | Numpad 0 |
| Gadget (placeholder) | C | Numpad 4 |
| Grab (unbuilt mechanic) | L | Numpad 6 |
| Shield / dodge | Shift | Numpad 3 |
| Menu navigate / confirm / back | W/S, J, K | — |
| Debug overlay (hitboxes/hurtboxes) | F3 | F3 |
| Pause menu | Esc | Esc |

## Project layout

```
├── project.godot / ByteBrawl.csproj   # Godot project + .NET build
├── scenes/                            # Main.tscn, Arena.tscn + smoke test scenes
├── src/
│   ├── Combat/            # PLAIN C# game logic — no Godot node types.
│   │   │                  # Everything here is unit-testable without booting Godot.
│   │   ├── FighterStateMachine.cs   # states: idle/run/jump/attack/shield/dodge/
│   │   │                            # hitstun/charge; drives attacks & armor windows
│   │   ├── MatchRules.cs            # damage, scaling knockback, hitstun, shield,
│   │   │                            # typed hit resolution (armor, wind, block)
│   │   ├── AttackData.cs / AttackStage.cs / HitboxSpec.cs / ChargeConfig.cs
│   │   ├── BoxTypes.cs              # HitboxType, HurtboxType, LimbGroup, ArmorSpec
│   │   ├── GrabData.cs              # grab/throw data model (mechanics unbuilt)
│   │   ├── Moveset.cs / AttackSlot.cs / FighterStats.cs
│   │   └── IFighter.cs / IHitboxManager.cs   # the seams the logic talks through
│   ├── Nodes/             # Godot presentation layer.
│   │   ├── Fighter.cs               # CharacterBody2D implementing IFighter
│   │   ├── LimbRig.cs / Limb.cs     # 16-part puppet rig, auto-derived hurtboxes
│   │   ├── Hurtbox.cs               # per-limb capsule with a HurtboxType
│   │   ├── HitboxManager.cs / Hitbox.cs  # spawns attack circles, resolves contact
│   │   ├── CombatDebugDraw.cs       # F3 debug overlay (Smash-convention colors)
│   │   ├── PoseLibrary.cs / PosePlayer.cs  # code-driven stick-figure poses
│   │   ├── Arena.cs / ArenaCamera.cs / Main.cs / PauseMenu.cs / LocalInput.cs
│   │   └── SmokeArena.cs / SmokeLimbRig.cs # self-checking headless test scenes
│   └── Data/
│       └── ByteMoveset.cs           # ALL balance data for Byte (attacks, charge,
│                                    # multi-hit stages, armor) — edit this to tune
├── tests/ByteBrawl.Tests/ # xUnit; Phaser-free fakes for IFighter/IHitboxManager
├── docs/
│   ├── product-backlog.md           # what the game will become (scope reference)
│   └── superpowers/                 # design specs + implementation plans
└── AGENTS.md                          # the same guide, written for AI agents
```

## Architecture in one paragraph

`src/Combat` is deliberately **free of Godot node types** — only `Vector2` and
friends. The per-frame flow is: `Fighter` (node) captures input →
`FighterStateMachine` (plain C#) mutates velocities/state and asks the
`IHitboxManager` to spawn hitboxes → `HitboxManager` (node) detects overlaps
against the opponent's `Hurtbox` nodes → `MatchRules.ApplyHit` (plain C#) decides
what the hit does, now including typed outcomes (armor absorbs knockback,
intangible whiffs, wind pushes). Because all rules live behind `IFighter`, the
xUnit suite exercises real game logic with fakes — no engine boot required.

Two things worth internalizing before your first change:

1. **The limb rig is the hurtbox.** Every body part is a `Limb` (Sprite2D at a
   joint pivot) with a `Hurtbox` capsule auto-derived from the sprite's size.
   When real art replaces the placeholder rectangles, hurtboxes refit
   automatically — don't hand-place them. Part names map to `LimbGroup`s
   (`LimbRig.GroupFor`): Head / Torso+Pelvis / both arms / both legs.
2. **Balance data is code.** All numbers — damage, knockback, charge frames,
   attack stages, armor entries — live in `src/Data/ByteMoveset.cs`. Game-feel
   constants live at the top of `FighterStateMachine.cs`.

## Common tasks

| You want to… | Touch |
|---|---|
| Tune an attack / add a moveset entry | `src/Data/ByteMoveset.cs` |
| Change game feel (dodge frames, charge, gravity) | constants in `src/Combat/FighterStateMachine.cs`, `src/Nodes/Fighter.cs` |
| Add a fighter state | `FighterState` in `src/Combat/`, transitions in `FighterStateMachine.cs`, pose in `PoseLibrary.cs` |
| Change controls | `src/Nodes/LocalInput.cs` |
| Add a hitbox type / hurtbox behavior | `src/Combat/BoxTypes.cs` + resolution in `MatchRules.cs`/`HitboxManager.cs` |
| Add a character | new data file next to `ByteMoveset.cs`; the rig/debug overlay pick it up via the same 16 part names |

## Conventions

- **Strict C#**: treat warnings as signal; unused locals/params fail the build.
- **TDD**: combat logic changes come with an xUnit test first. Fakes live in
  `tests/ByteBrawl.Tests/TestDoubles.cs`.
- **Commits**: conventional prefixes (`feat:`, `fix:`, `docs:`, `polish:`).
- **Verify before pushing**: `dotnet build`, `dotnet test`, both smoke scenes.
- Design docs for larger features go in `docs/superpowers/specs/` with the
  matching plan in `docs/superpowers/plans/` before implementation.

## Teammate note: art pipeline

Real sprites drop in by replacing the placeholder textures per limb — **one sprite
per the 16 part names in `LimbRig.CreatePlaceholder`** (`Pelvis`, `Torso`,
`Backpack`, `Head`, `Near/Far UpperArm`, `Near/Far Forearm`, `Near/Far Hand`,
`Near/Far Thigh`, `Near/Far Shin`, `Near/Far Foot`), origin at the joint pivot
(top of an arm segment, hip for legs), extending downward along +Y.
