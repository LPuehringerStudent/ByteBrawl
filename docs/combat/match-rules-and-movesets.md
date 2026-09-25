# Match Rules, Damage & Moveset Data

The rules of a match and where every number lives.

## Damage & knockback

Fighters have a damage **percentage** (`IFighter.Damage`), not HP — it only ever
goes up. On a clean hit (`MatchRules.ApplyHit`):

```
preDamage        = defender.Damage                 (read BEFORE applying)
defender.Damage += attack.BaseDamage
knockback        = attack.BaseKnockback + preDamage * attack.Scaling
velocity         = (Direction.X * attacker.Facing, Direction.Y) * knockback
hitstun          = attack.HitstunFrames
```

So the same attack sends a freshly-spawned fighter barely flying and a
150%-damaged fighter off the blast zone — the core platform-fighter loop. Shield
hits use a separate branch: 3× damage to `ShieldHealth`, 0.3× knockback, no
hitstun. Armor hits skip knockback and hitstun entirely (see
`hitboxes-and-hurtboxes.md`).

## Elemental meter (gimmick hook)

`ElementalMeter` fills from combat: 0.6× of damage **dealt**, 0.4× of damage
**taken**, capped at 100. `IFighter.Frenzy` is an `IElementalFrenzy?` seam —
Elemental Frenzy (the per-element super mode) is designed but not implemented;
the meter accumulates and nothing consumes it yet.

## Ring-out, stocks, respawn, timer

`Arena._PhysicsProcess` checks the blast zone rect (`(-160,-180)` to
`(480,360)`) per fighter:

- **Training**: instant respawn at the spawn point, 0 invincibility frames.
- **Versus**: `MatchRules.CheckRingOut` — lose a stock (damage resets to 0);
  at 0 stocks the match ends (`P1Win`/`P2Win`), otherwise respawn with
  `MatchRulesConfig.RespawnInvincibilityFrames` of invincibility.

The match timer starts at 180 seconds (decremented from `MatchRules.Update` in
milliseconds). **Known quirk**: expiring timer always declares P1 the winner —
proper tie-break (stocks, then damage) is issue #7.

## Moveset data model (all balance is code)

Everything tunable lives in `src/Data/ByteMoveset.cs`, composed from plain
classes in `src/Combat/`:

```
Moveset
 └─ Stats: FighterStats { RunSpeed, JumpSpeed, Weight }
 └─ Attacks: Dictionary<AttackSlot, AttackData>   // 16 slots, all filled

AttackData                          // one attack
 ├─ Id, BaseDamage, BaseKnockback, Scaling, Direction, HitstunFrames
 ├─ ActiveFrames                   // how long each spawned hitbox lives
 ├─ Hitboxes: List<HitboxSpec>     // circles spawned for a FLAT attack
 ├─ Stages: List<AttackStage>      // OR multi-hit stages (spawned by frame)
 │    └─ AttackStage : AttackData + SpawnFrame
 ├─ Charge: ChargeConfig?          // MinChargeFrames/MaxChargeFrames/
 │                                 // MaxHoldFrames + per-frame damage/kb growth
 └─ Armor: List<ArmorSpec>         // per-limb-group armor during active frames
```

Attack slots cover ground/air × neutral/side/up/down × light/heavy/special
(`AttackSlot`), but the FSM currently only ever fires the **Neutral** slots —
directional selection is issue #2, and all slots share three base attacks for
now (light = sword-light, heavy = chargeable sword-heavy, special = 3-stage
byte-special).

Authoring examples already in `ByteMoveset.cs`:

- **Sweet spot**: stage 3 of the special spawns a base circle (5 dmg/180 kb) plus
  a smaller fist tipper with `DamageOverride = 7, KnockbackOverride = 210`.
- **Charge**: the heavy grows 0.2 damage and 1.0 knockback per charge frame, up
  to 180 frames, holdable 180 more before auto-fire.
- **Armor**: the heavy carries `Armor = [(Arm, HyperArmor)]` — both arm capsules
  go purple (damage-absorbing, no flinch) during its 6 active frames.

## Testing the rules

`tests/ByteBrawl.Tests/` runs without Godot: `FakeFighter` (records hurtbox
overrides, additive knockback) and `FakeHitboxManager` (records spawns) implement
the `IFighter`/`IHitboxManager` seams. `MatchRulesTests` covers damage, scaling,
shield, armor outcomes, wind, and ring-outs; `FighterStateMachineAttackTests`
scripts full attack sequences frame by frame. Run everything with:

```bash
dotnet build && dotnet test tests/ByteBrawl.Tests
```
