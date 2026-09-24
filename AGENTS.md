# ByteBrawl — Agent Guide

ByteBrawl is a browser-based, local 1v1 2D platform fighter. Two players share one keyboard and fight on a single stage until one runs out of stocks or the timer expires. The current codebase is an MVP: core mechanics work, but art, audio, and online features are placeholders or out of scope.

## Technology stack

- **Language:** TypeScript 5.4
- **Game engine:** Phaser 3.80 (Arcade Physics)
- **Build tool / dev server:** Vite 5.2
- **Test runner:** Vitest 1.5
- **Runtime target:** ES2020, modern evergreen browsers
- **Module system:** ES modules (`"type": "module"`)

No frameworks like React or Express are used. The DOM is manipulated directly for the HUD.

## Project structure

```
.
├── src/
│   ├── main.ts                     # Vite entry point; creates the Phaser.Game
│   ├── game/
│   │   ├── GameScene.ts            # Main scene; owns the update loop and wires systems
│   │   ├── GameRules.ts            # Match state, damage, knockback, stocks, timer
│   │   ├── Stage.ts                # Tilemap platform layer and blast zones
│   │   ├── UIManager.ts            # DOM-based HUD updates
│   │   ├── CameraController.ts     # Two-player camera with zoom and deadzone
│   │   ├── OffscreenIndicator.ts   # Arrows/circles for fighters off camera
│   │   └── config.ts               # Stage and fighter configs (STAGE_CONFIG, FIGHTER_P1_CONFIG, FIGHTER_P2_CONFIG)
│   ├── fighter/
│   │   ├── Fighter.ts              # Sprite + physics body wrapper; executes commands
│   │   ├── FighterStateMachine.ts  # State transitions: idle/run/jump/attack/shield/dodge/hitstun
│   │   └── HitboxManager.ts        # Spawns invisible hitbox sprites and reports overlaps
│   ├── input/
│   │   ├── PlayerInput.ts          # Normalizes keyboard state into ActionFrame
│   │   └── InputRouter.ts          # Thin wrapper around Phaser keyboard keys
│   └── shared/
│       ├── types.ts                # Shared TypeScript interfaces and FighterState union
│       ├── AssetLoader.ts          # Runtime placeholder-texture generation
│       └── AudioManager.ts         # Placeholder audio stub
├── tests/                          # Vitest unit tests
│   ├── FighterStateMachine.test.ts
│   ├── GameRules.test.ts
│   ├── MatchSimulation.test.ts
│   └── PlayerInput.test.ts
├── assets/                         # Empty directories; real assets not yet used
│   ├── audio/
│   ├── spritesheets/
│   └── tilemaps/
├── index.html                      # Host page + inline HUD CSS
├── package.json
├── tsconfig.json                   # Strict TypeScript for src/
├── tsconfig.node.json              # Project reference for vite.config.ts
└── vite.config.ts
```

## Build and run commands

```bash
# Install dependencies
npm install

# Start the Vite dev server on http://localhost:5173
npm run dev

# Production build (TypeScript check + Vite bundle into dist/)
npm run build

# Preview the production build locally
npm run preview

# Run the test suite once
npm test

# Run tests in watch mode
npm run test:watch
```

The dev server binds to `0.0.0.0` (`host: true`) on port `5173` by default. The production bundle is written to `dist/`.

## Architecture overview

`GameScene` is the single Phaser scene. Each frame it:

1. Polls both `PlayerInput` instances for an `ActionFrame`.
2. Runs both `FighterStateMachine` instances, which mutate their `Fighter` (velocity, facing, state).
3. Lets Phaser Arcade Physics resolve platform collisions.
4. Ticks `HitboxManager`, which checks active hitboxes against opponents.
5. Reports hits to `GameRules` for damage/knockback/hitstun.
6. Asks `GameRules` to check ring-outs and respawn or declare a winner.
7. Updates `UIManager` and `CameraController`.

Rules and state live in plain classes, not in Phaser-specific objects, which keeps most game logic testable without a browser.

## Key conventions

- **Strict TypeScript:** `strict`, `noUnusedLocals`, `noUnusedParameters`, and `noFallthroughCasesInSwitch` are enabled. Unused variables and implicit anys are build errors.
- **Imports:** Prefer `import` over `require`. Source files use `.ts` extensions in imports.
- **Naming:** Classes are `PascalCase`, files match their primary export (`Fighter.ts` → `Fighter`). Interfaces are `PascalCase` with no `I` prefix.
- **Magic numbers for game feel live in code, not data:** Cooldowns, dodge frames, air-dodge speed, etc., are constants at the top of `FighterStateMachine.ts`.
- **Config data lives in `src/game/config.ts`:** Stage layout, spawn points, blast zones, fighter stats, and attack data are exported constants.
- **Tests use Vitest mocks:** Phaser objects are mocked as plain objects so the test suite runs in Node without a browser.

## Testing strategy

Tests are in `tests/` and run with Vitest.

- `FighterStateMachine.test.ts` — Shield, spot-dodge, air-dodge, and state-transition behavior.
- `GameRules.test.ts` — Damage, knockback scaling, invincibility, shield damage, ring-outs, and win conditions.
- `MatchSimulation.test.ts` — End-to-end scripted match scenarios.
- `PlayerInput.test.ts` — Direction normalization and one-shot action detection.

Run all tests with `npm test`. The project currently has 25 passing tests. When adding features, add focused unit tests that mock Phaser types rather than booting the full engine.

## Input controls

Hard-coded in `src/game/GameScene.ts`:

**Player 1 (Byte)**
- Move: `A` / `D`
- Up / Down: `W` / `S` (down drops through thin platforms)
- Jump: `Space`
- Light attack: `Z`
- Heavy attack: `X`
- Special attack: `V`
- Shield / dodge: `Shift`

**Player 2 (Nibble)**
- Move: `←` / `→`
- Up / Down: `↑` / `↓`
- Jump: `Numpad 5`
- Light attack: `Numpad 1`
- Heavy attack: `Numpad 2`
- Special attack: `Numpad 0`
- Shield / dodge: `Numpad 3`

## Current MVP limitations (do not assume these exist)

- **No real assets:** `AssetLoader.createPlaceholderTextures()` generates colored rectangles at runtime. `assets/` is unused.
- **No audio:** `AudioManager` is a stub with empty methods.
- **No controller / gamepad support:** Keyboard only.
- **One stage, two characters:** Configs are constants; there is no roster selection or stage picker.
- **No networking:** Online multiplayer is explicitly out of scope for the MVP.
- **No AI opponent:** A second human player is required.
- **Timer tie-breaker:** If time runs out, Player 1 is declared the winner (`matchState = 'p1Win'`).
- **Committed `dist/`:** The `dist/` directory exists but is listed in `.gitignore`; it is regenerated by `npm run build`.

## Security and deployment notes

- The game is a static client-side bundle. No backend, secrets, environment variables, or user data are involved.
- `index.html` loads `/src/main.ts` via a module script in development and a hashed bundle after `vite build`.
- Do not commit `.env` files or real API keys; they are not needed today.
- Deployment is any static host serving `dist/` after `npm run build`.

## Useful paths for common changes

- Balance fighter stats / attacks: `src/game/config.ts`
- Change controls: `src/game/GameScene.ts` (`P1_KEYS` / `P2_KEYS`)
- Tune physics / gravity: `src/main.ts` and `src/game/GameScene.ts` (`gravity.y`)
- Add real asset loading: `src/shared/AssetLoader.ts` and call it from `GameScene.create()`
- Add audio: implement `src/shared/AudioManager.ts` and invoke it from `FighterStateMachine` / `GameRules`
- Add new fighter states: extend `FighterState` in `src/shared/types.ts` and add transitions in `src/fighter/FighterStateMachine.ts`
