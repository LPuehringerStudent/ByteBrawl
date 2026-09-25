# Fighter, State Machine & the Limb Rig

How a fighter is put together: the node wrapper, the plain-C# state machine, the
16-part puppet rig, and how poses animate it.

## The split

A fighter is two layers:

- **`Fighter` (`src/Nodes/Fighter.cs`)** — the Godot `CharacterBody2D`. Owns the
  physics body (12×20 collision rectangle — the thing platforms actually block),
  the limb rig, gravity, and everything `IFighter` exposes. Also the only class
  that touches `Velocity` directly.
- **`FighterStateMachine` (`src/Combat/FighterStateMachine.cs`)** — plain C#.
  Decides *what the fighter wants to do* each frame from the `ActionFrame` input
  snapshot; expresses it through `IFighter` members (velocity, facing, hitstun,
  hurtbox overrides). It never touches a Godot node type, which is why the xUnit
  suite can drive entire scripted matches through `FakeFighter`.

`Fighter._PhysicsProcess` order matters: gravity → timers → `Fsm.Update` →
`MoveAndSlide()` → mirror the rig by facing (`_rig.Scale = (Facing*0.65, 0.65)`)
→ play the pose.

## States

`FighterState` values and the important transitions (see `Update`/`SetState`):

- `Idle` / `Run` — grounded movement; facing follows input.
- `Jump` / `Fall` — airborne; `Jump` while rising, `Fall` otherwise. `Jump`
  while airborne consumes the fighter's air jumps (`FighterStats.AirJumps`, Byte:
  1), reset on landing; hitstun does not refresh it.
- `LightAttack` / `HeavyAttack` / `Special` — one-shot attack states. Entered via
  `StartAttack`, which builds the stage sequence, sets the cooldown, and spawns
  hitboxes (flat attacks immediately, staged attacks via `TickAttack`).
- `Charging` — entered instead of an attack when the `AttackData` has a
  `ChargeConfig` **and the fighter is grounded** (charging in the air is not a
  thing; a chargeable attack fired airborne, like the recovery, goes off
  immediately, uncharged). Applies heavy air/ground drag (charging fighters
  fall slowly and lose horizontal momentum). Releases into the attack when the
  button is let go past `MinChargeFrames`, or auto-fires after
  `MaxChargeFrames + MaxHoldFrames`. `SetChargingFull` drives the
  blink-when-full placeholder effect. Staged attacks keep their stages on
  release, with damage/knockback scaled by the charge.
- `Shield` — shield button on the ground with no direction held. Drains
  `ShieldHealth` (0.1/frame). At 0 the shield just drops today (shield-break
  stun is backlog, issue #4).
- `SpotDodge` — shield **+ down** tapped on the ground (neutral shield tap
  raises the shield instead). 30 frames total: intangible for the first 18,
  then ~12 frames of vulnerable recovery lag where you still can't act.
  Keeping shield held past frame 5 converts into `Shield`. While intangible,
  all limb groups are `Intangible`.
- `AirDodge` — shield tapped in the air; a burst of velocity (stick × 220)
  that **decays** at 0.9×/frame — including straight up when the stick is
  neutral (a spot dodge in place: intangibility with no drift).
- `HeavyAttack` (air, up) — the recovery: air + heavy + holding up fires the
  moveset's `UpHeavy` slot once per airtime with a vertical boost (`RecoveryConfig`);
  other attacks are locked until you land unless the attack opts out
  (`CanActAfter`). See `docs/superpowers/specs/2026-09-25-recovery-system-design.md`.
- `Hitstun` — not entered directly; any frame `HitstunFrames > 0` forces this
  state (checked first in `Update`) and clears combat overrides (armor windows,
  intangibility) so interrupted attacks can't leave stale hurtbox state behind.

Game-feel constants live at the top of `FighterStateMachine.cs`
(`SpotDodgeFrames = 20`, `AirDodgeSpeed = 220`, charge drag values, …) — tune
there, not inline.

## The limb rig (`src/Nodes/LimbRig.cs`)

A 16-part puppet, hierarchy modeled on a reference limb system:

```
Pelvis ─┬─ FarThigh → FarShin → FarFoot        (behind, darkened, z-1)
        ├─ NearThigh → NearShin → NearFoot      (front, bright, z+1)
        └─ Torso ─┬─ Backpack                   (no hurtbox)
                    ├─ FarUpperArm → FarForearm → FarHand
                    ├─ Head
                    └─ NearUpperArm → NearForearm → NearHand
```

Each `Limb` is a `Node2D` whose **sprite origin sits at the joint pivot**
(`Sprite2D.Centered = false`, `Offset = -pivot`), so rotating the node bends the
segment at the shoulder/elbow/hip/knee. Segments extend downward along +Y from
their pivot — this is the convention real art must follow (see README → art
pipeline). The far side renders darker and one z-layer behind for depth.

Every limb except the backpack gets a `Hurtbox` capsule derived from its own
sprite geometry (`AttachHurtbox`): radius = half the limb thickness, height along
the long axis with slight joint overlap. Real art swaps in with zero hurtbox
work.

## Poses (`PoseLibrary` + `PosePlayer`)

Animation is code, not keyframes: every physics frame, `PosePlayer.Play(state,
stateFrames)` asks `PoseLibrary.For(state, stateFrames)` for a map of
`limbName → rotationDegrees` and applies it. Angles missing from a pose keep
whatever value they had — poses are sparse deltas over the rig's rest pose, so
additively combining base + detail angles works naturally. `Run` is procedural
(`Mathf.Sin(stateFrames * 0.3)` swing); everything else is a static pose per
state. Adding a new pose = one `case` in `PoseLibrary.For` plus (usually) a new
`FighterState` value and its transitions in the FSM.
