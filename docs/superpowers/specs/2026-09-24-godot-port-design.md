# ByteBrawl Godot Port — Design Spec

Date: 2026-09-24
Status: Approved (pending spec review)

## Goal

Replace the Phaser/web version of ByteBrawl with a Godot 4.7 (C#) project in the **same repository**. The old web version is deleted — it serves no further use. The GitHub Project board and `docs/product-backlog.md` remain the scope reference.

Decisions locked in during brainstorming:

| Topic | Decision |
|---|---|
| Language | C# |
| Character rendering | Segmented sprite parts (puppet/cutout), rotated at joint pivots |
| Scope of first draft | Core combat only (movement, attacks, damage/knockback, stocks, training dummy) |
| Placeholder art | Segmented placeholder parts (colored rectangles per limb) |
| Differentiator | **Elementals** replace Loadouts (see below) — hooks only in this draft |
| Architecture | Data-driven fighter core (attacks/movesets as Godot Resources, code-driven poses) |

## Elementals (the new gimmick — design intent, deferred)

Loadouts are dropped. Instead, each fighter has an **Elemental meter** that fills from damage dealt and damage taken. When full, the player can activate **Elemental Frenzy**, which alters gameplay based on the element chosen pre-fight.

This draft implements **only the hooks**: the meter on `Fighter` and a frenzy override seam. No frenzy behavior, no element roster yet.

## Repo plan

- The Phaser app (`src/`, `tests/`, `index.html`, `package.json`, Vite/TS config, `node_modules`) is removed.
- Fresh Godot C# project at repo root, project name **ByteBrawl**.
- `docs/product-backlog.md` is kept; `AGENTS.md` is rewritten for the Godot stack.
- Teammate sample repo (`David-Fruehwirt/unnamed_fighting_game`) is treated as a counter-example of structure; **none of its assets are used**.

## Architecture

### Scenes

- `main.tscn` — boot + minimal menu (Local Versus / Training).
- `arena.tscn` — stage, camera, two fighters, HUD placeholder.
- `fighter.tscn` — `CharacterBody2D` + limb rig + hurtbox + hitbox spawner.
- `training_dummy.tscn` — dummy reusing the fighter rig, driven by empty input.

### Fighter & limb rig

- `Fighter.cs` — owns movement and a plain C# state machine: idle / run / jump / fall / attack (light, heavy, special) / shield / dodge / hitstun. Holds a reference to its `Moveset` resource. No game logic lives in scene files.
- `LimbRig.cs` — puppet hierarchy: `pelvis → torso → head`, `pelvis → thighs → shins → feet`, `torso → upper arms → forearms → hands`. Each limb is a Sprite2D whose texture origin sits at the joint pivot, so rotation happens at shoulder / elbow / hip / knee.
- `PosePlayer.cs` — applies pose keyframes (limb → angle per frame) from code every physics frame. Deterministic and unit-testable. Real art swaps in later by replacing textures — no logic changes.

### Combat data model

- `AttackData` (Resource): id, base damage, base knockback, knockback scaling, angle, startup / active / recovery frames, hitbox shape (circle or box) + offset, optional multi-hit `stages` (per-stage frame, shape, damage, hitstun, finisher flag), optional `charge` config (min / max charge frames, max hold frames, damage & knockback growth).
- `Moveset` (Resource, per character): stats (run speed, jump speed, weight) plus attack slots — light / heavy / special × neutral / side / up / down × ground / air. Byte's moveset from the Phaser version (including the multi-hit special and chargeable heavy) becomes the first data entry.
- `MatchRules.cs` — the GameRules port: damage accumulation, percentage-scaled knockback, hitstun, stocks, blast zones, ring-out respawn, training-mode respawn, win condition, timer tie-break.
- Elemental hooks: `Fighter` exposes an `ElementalMeter` (fills from damage dealt and taken) and an `ElementalFrenzy` override seam (interface or virtual method). Frenzy behaviors are not implemented in this draft.
- Debug: toggleable drawing of hitboxes, hurtboxes, and knockback vectors from a pause overlay, mirroring the Phaser debug renderer.

### Testing

Headless smoke checks as simple Godot test scenes/scripts runnable via `godot --headless` (same idea as the sample repo's tests, but minimal): movement, hit application, match end. Combat logic stays in plain classes so it can be exercised without booting rendering.

## Non-goals (this draft)

- Menus beyond a functional placeholder, character select, stage select.
- Elemental Frenzy behaviors and element roster.
- Real art/audio (placeholder rectangles and silence).
- Online play, AI opponent.
- Porting every Phaser feature: the charge config stays in `AttackData` but tuning is not final; wall-jump/wall-slide, menus, loadouts, and pause polish are dropped.

## Risks

- **C# tooling on the team**: requires `dotnet` SDK installed for every contributor (the sample repo hit this too).
- **Limb rig feel**: placeholder rectangles can hide whether the pose system will hold up with real proportions — poses must be validated early with one real limb set.
