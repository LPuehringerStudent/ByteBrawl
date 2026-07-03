# ByteBrawl MVP Design

## Goal

Build a lightweight, browser-based 2D platform fighter that two players can play locally on one keyboard. The milestone focuses on tight core mechanics—movement, attacks, knockback, and a win condition—without committing to online multiplayer or a large roster.

## Scope

### In Scope

- Local 1v1 matches on a single keyboard.
- Two distinct characters ("byte-themed brawlers") with different animations and attack data.
- One stage with platforms and blast zones.
- Phaser 3 + Arcade Physics for movement and collisions.
- Keyboard input only; no controller support yet.
- Damage-based knockback, stocks (default 3 per player), respawn with brief invincibility, and win condition.
- Basic HUD (damage %, stock icons, optional 3-minute match timer, win overlay).
- Pixel art target resolution of 320×180, scaled to browser size.

### Out of Scope

- Online multiplayer, matchmaking, or networking code.
- Controller / gamepad support.
- More than two characters or more than one stage.
- Complex AI opponents.
- Audio assets (placeholder sounds are acceptable).
- Persisted player profiles or leaderboards.

## Decisions

| Decision | Choice | Rationale |
|---|---|---|
| Physics | Phaser 3 Arcade Physics | Simpler to tune than Matter.js or a custom engine; sufficient for a first prototype. |
| Architecture | Component-based entities | Clean boundaries make it easy to add characters, stages, and later online without rewriting everything. |
| Characters / Stages | 2 characters, 1 stage | Smallest vertical slice that still shows a matchup. |
| Art style | Pixel art, 320×180 base | Scales cleanly to 1080p and keeps animation work manageable. |
| Input | Keyboard-only | Faster MVP than supporting controllers. |
| Player 1 keys | WASD + Space / Z / X | Standard platformer layout. |
| Player 2 keys | Arrow keys + NumPad 0 / 1 / 2 | Mirrored finger positions on the right side of the keyboard. |

## Architecture

```
GameScene (Phaser.Scene)
├── Stage
├── Player 1
│   ├── PlayerInput
│   ├── FighterStateMachine
│   ├── Fighter
│   └── HitboxManager
├── Player 2
│   ├── PlayerInput
│   ├── FighterStateMachine
│   ├── Fighter
│   └── HitboxManager
├── GameRules
└── UIManager

Shared Services
├── AssetLoader
├── AudioManager
└── InputRouter
```

`GameScene` owns the update loop and wires everything together. Each player is a bundle of input, state machine, fighter body, and hitbox logic. `GameRules` is the single source of truth for match state, and `UIManager` reflects that state on screen.

## Components

### GameScene
- Creates the stage, both fighters, HUD, and shared services.
- Runs the fixed-timestep update loop.
- Delegates rendering to Phaser sprites.

### Stage
- Loads one Tiled JSON tilemap.
- Defines static platforms via an Arcade physics tilemap layer.
- Defines blast-zone rectangles and notifies `GameRules` when a fighter leaves the stage.

### Fighter
- Owns the sprite and Arcade physics body.
- Exposes methods: `setVelocity`, `applyKnockback`, `faceDirection`, `takeDamage`, `enterHitstun`.
- Does not decide what to do; it executes commands from the state machine.

### FighterStateMachine
- States: `Idle`, `Run`, `Jump`, `Fall`, `LightAttack`, `HeavyAttack`, `Special`, `Hitstun`, `Recovery`.
- Transitions based on input, grounded/airborne state, and hit events.
- Requests hitboxes from `HitboxManager` during attack states.

### PlayerInput
- Reads keyboard keys for one player.
- Outputs a normalized action frame each tick: `moveX`, `jumpPressed`, `attackLight`, `attackHeavy`, `attackSpecial`.
- Decouples raw key codes from game logic.

### HitboxManager
- Spawns and disables Arcade physics overlap zones per attack animation frame.
- Detects overlaps between a player's active hitboxes and the opponent's hurtbox.
- Reports hits to `GameRules`; never modifies fighter state directly.

### GameRules
- Tracks damage percentage, stocks, respawn timers, and winner.
- Calculates knockback from `attackBaseKnockback + defenderDamage × scaling`.
- Applies knockback and hitstun to the defender.
- Checks ring-outs and declares the winner.

### UIManager
- Updates HUD text: damage percentage, stock icons, match timer.
- Shows the win overlay when a match ends.

### AssetLoader / AudioManager / InputRouter
- Shared setup utilities used by `GameScene`.
- `InputRouter` maps raw browser events to Phaser key objects.

## Data Flow

One frame:

1. `GameScene.update(dt)` starts the tick.
2. Each `PlayerInput` produces a normalized action frame.
3. Each `FighterStateMachine` picks the next state from inputs + physics flags.
4. Each `Fighter` applies velocity, facing, and animation from the state.
5. Phaser Arcade Physics resolves platform collisions.
6. `HitboxManager` checks active hitboxes against the opponent's hurtbox.
7. On overlap, `HitboxManager` sends `{ attacker, defender, attackData }` to `GameRules`.
8. `GameRules` updates damage, computes knockback, applies it to the defender, and checks for ring-out / win.
9. `UIManager` refreshes the HUD from `GameRules` state.

### Hit Resolution

```
newDamage = defender.damage + attackData.baseDamage
knockback = attackData.direction × (attackData.baseKnockback + defender.damage × attackData.scaling)
defender.applyKnockback(knockback)
defender.enterHitstun(attackData.hitstunFrames)
```

## Error Handling

- **Input conflicts:** Use `Phaser.Input.Keyboard.KeyCodes` instead of character events. Pause input when the browser tab loses focus.
- **Physics tunneling:** Keep fighter bodies at least 8–12 px tall and clamp `dt` to a maximum value (e.g., 50 ms) to prevent large time steps.
- **Asset load failures:** Show a preload scene with progress bar and a retry option if spritesheets or tilemaps fail to load.
- **Frame drops:** Fixed timestep keeps simulation consistent even if rendering stutters.

## Testing

- **Unit tests:**
  - `FighterStateMachine` transitions for all states.
  - `GameRules` damage and knockback math with a pure test harness.
- **Integration tests:**
  - One scripted full match using fake inputs to verify stocks and win condition.
- **Manual feel tests:**
  - Jump height, run speed, dash distance.
  - Hitstop duration, hitstun length.
  - Knockback scaling at low and high damage.

## Folder Sketch

```
src/
  main.ts                 # Vite entry point
  game/
    GameScene.ts
    GameRules.ts
    Stage.ts
    UIManager.ts
  fighter/
    Fighter.ts
    FighterStateMachine.ts
    HitboxManager.ts
  input/
    PlayerInput.ts
    InputRouter.ts
  shared/
    AssetLoader.ts
    AudioManager.ts
assets/
  spritesheets/
  tilemaps/
  audio/
tests/
```

## Future Work (Post-MVP)

- Controller / gamepad support.
- Additional characters and stages.
- Online multiplayer via Socket.io + WebRTC.
- Matchmaking server.
