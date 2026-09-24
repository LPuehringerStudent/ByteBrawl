# ByteBrawl — Agent Guide

ByteBrawl is a local 1v1 2D platform fighter rebuilt in Godot 4.7 with C#. Two players share one keyboard and fight on a single stage until one runs out of stocks or the timer expires.

## Technology stack

- **Engine:** Godot 4.7 (C# / .NET flavor)
- **Language:** C# (see `ByteBrawl.csproj`)
- **Unit tests:** xUnit (`tests/ByteBrawl.Tests`)
- **Scenes:** `scenes/` (`.tscn` text format)

## Build and run commands

```bash
# Build (also required before running headless smoke tests — headless Godot does not rebuild C#)
dotnet build

# Unit tests
dotnet test tests/ByteBrawl.Tests

# Run the game
godot --path .

# Headless smoke tests (timeout guards against hangs)
timeout 30 godot --headless --path . res://scenes/smoke_arena.tscn
timeout 30 godot --headless --path . res://scenes/smoke_limb_rig.tscn
```

A passing smoke run prints `SMOKE PASS`. Note: the `godot` binary must be a .NET (mono) build — the plain GDScript build cannot run C# projects.

## Project structure

```
.
├── src/
│   ├── Combat/           # Game logic: plain C#, NO Godot node types — keep it that way
│   │   ├── FighterStateMachine.cs  # State transitions: idle/run/jump/attack/shield/dodge/hitstun
│   │   ├── MatchRules.cs           # Damage, knockback, stocks, timer, ring-outs
│   │   ├── Moveset.cs, AttackData.cs, AttackStage.cs, ChargeConfig.cs  # Data-driven attacks
│   │   ├── IFighter.cs, IHitboxManager.cs   # Seams that keep Combat free of Godot types
│   │   ├── ElementalMeter.cs, IElementalFrenzy.cs  # Elemental hooks (frenzy deferred)
│   │   ├── ActionFrame.cs          # Per-frame input snapshot (record struct)
│   │   └── FighterStats.cs, FighterState.cs, AttackSlot.cs, HitboxShape.cs, MatchRulesConfig.cs
│   ├── Data/             # Combat tuning data (movesets, configs) — tweak balance here
│   │   └── ByteMoveset.cs          # Byte's full moveset incl. multi-hit chains and charge attacks
│   └── Nodes/            # Godot presentation layer: nodes, scenes' scripts
│       ├── Arena.cs                  # Stage layout, blast zones, training mode, debug overlay wiring
│       ├── Fighter.cs                # Fighter node (implements IFighter); owns state machine
│       ├── LimbRig.cs, Limb.cs, PosePlayer.cs, PoseLibrary.cs, Pose.cs  # Segmented limb placeholder rig
│       ├── HitboxManager.cs, Hitbox.cs, Hurtbox.cs   # Hit detection
│       ├── LocalInput.cs             # Keyboard → ActionFrame
│       ├── ArenaCamera.cs            # Two-player camera
│       ├── CombatDebugDraw.cs        # F3-toggled overlay (hurtboxes, hitboxes, knockback vectors)
│       ├── Main.cs                   # Boot menu (W/S select, J confirm, K quit)
│       └── SmokeArena.cs, SmokeLimbRig.cs  # Headless smoke-test drivers
├── scenes/
│   ├── main.tscn           # Boot menu (Local Versus → arena.tscn, Training → arena_training.tscn)
│   ├── arena.tscn          # Main stage
│   ├── arena_training.tscn # Same stage with Training = true (P2 is a dummy, no timer/stocks)
│   ├── fighter.tscn        # Fighter instance scene
│   ├── smoke_arena.tscn    # Headless arena smoke test
│   └── smoke_limb_rig.tscn # Headless limb-rig smoke test
├── tests/ByteBrawl.Tests/  # xUnit unit tests (22 tests)
└── project.godot
```

## Key conventions

- **`src/Combat` stays engine-free:** game logic is plain C# against `IFighter` / `IHitboxManager` interfaces so it runs under xUnit without Godot. Do not introduce Godot types into `src/Combat`; add seams in the interfaces instead.
- **`src/Nodes` is the only place Godot node types appear.** `Fighter` implements `IFighter` and wraps the state machine.
- **Combat tuning data lives in `src/Data/`** (movesets, attack stats). Balance changes go there, not in logic code.
- **Tests use fakes/mocks:** `TestDoubles.cs` provides fake fighters and hitbox managers so `src/Combat` tests run in plain `dotnet test`.
- All per-frame combat timing is in frames at 60 fps logic (`_PhysicsProcess`).

## Current limitations

- Placeholder visuals: fighters are segmented-rectangle limb rigs with code-driven poses; no sprites, audio, or netcode.
- One stage, one character (Byte); Nibble is not yet ported.
- Elemental system: meter fill is wired into `MatchRules.ApplyHit`; frenzy behavior is deferred (hook only).

## Useful paths for common changes

- Balance attacks: `src/Data/ByteMoveset.cs` and `src/Combat/AttackData.cs`
- Fighter state transitions: `src/Combat/FighterStateMachine.cs`
- Damage/knockback/stocks/timer: `src/Combat/MatchRules.cs`
- Stage layout / blast zones / training behavior: `src/Nodes/Arena.cs`
- Controls: `src/Nodes/LocalInput.cs`
- Menu: `src/Nodes/Main.cs` + `scenes/main.tscn`
