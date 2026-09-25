# Typed Hitboxes & Hurtboxes Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add typed hitbox behaviors (damage/wind/grab/search) and per-limb hurtbox states (vulnerable/intangible/invincible/super armor/hyper armor), with Smash-convention debug colors, per the design spec at `docs/superpowers/specs/2026-09-25-hit-hurtbox-types-design.md`.

**Architecture:** All new resolution logic lives in plain C# under `src/Combat` (namespace `ByteBrawl.Combat`, no Godot node types — `Vector2` allowed) so xUnit covers it. `IFighter` gains one seam method `SetHurtboxOverride` so the Godot-free `FighterStateMachine` can drive per-limb states; `Fighter` (node) maps groups onto its rig's `Hurtbox` nodes. `MatchRules.ApplyHit` gains an optional hurtbox-type parameter — existing call sites and tests keep working unchanged.

**Tech Stack:** Godot 4.7 (C#), xUnit, .NET with `RollForward=LatestMajor`.

## Global Constraints

- Spec: `docs/superpowers/specs/2026-09-25-hit-hurtbox-types-design.md` — read it first.
- `src/Combat` must not reference Godot node types (`Node`, `Area2D`, `Sprite2D`, …). `Vector2`/`Color` structs are fine.
- Keep the near-transparent debug fill style and `ZIndex = 100` in `CombatDebugDraw`.
- Existing 23 tests must stay green; commit messages are conventional (`feat:`, `fix:`, `polish:`).
- Verification before every commit: `dotnet build`, `dotnet test tests/ByteBrawl.Tests`.
- Full verification after the last task additionally runs: `timeout 30 godot --headless --path . res://scenes/smoke_arena.tscn` and `timeout 30 godot --headless --path . res://scenes/smoke_limb_rig.tscn` (both must print `SMOKE PASS`). Headless Godot does NOT rebuild C# — always `dotnet build` first.
- File convention: one logical type per file under `src/Combat`, fields-as-data style (see `HitboxSpec.cs`, `AttackData.cs`), no `I` prefix on interfaces.
- Grab: data model is written and validated, mechanics are NOT built — grab hitboxes render in debug but have zero gameplay effect.

---

### Task 1: Box type taxonomy + GrabData

**Files:**
- Create: `src/Combat/BoxTypes.cs`
- Create: `src/Combat/GrabData.cs`
- Modify: `src/Combat/HitboxSpec.cs`
- Modify: `src/Combat/AttackData.cs`
- Test: `tests/ByteBrawl.Tests/BoxTypeDataTests.cs`

**Interfaces:**
- Consumes: nothing new.
- Produces (later tasks rely on these exact names):
  - `enum HitboxType { Damage, Wind, Grab, Search }`
  - `enum HurtboxType { Vulnerable, Intangible, Invincible, SuperArmor, HyperArmor }`
  - `enum LimbGroup { Head, Torso, Arm, Leg }`
  - `class ArmorSpec { public LimbGroup Group; public HurtboxType Type; public float? BreakKbThreshold; }`
  - `class ThrowData { public float Damage = 6; public float BaseKnockback = 120; public float Scaling = 1f; public float Angle; public bool IsValid(); }`
  - `class GrabData { public int HoldFrames = 30; public float PummelDamage = 2; public int PummelInterval = 20; public float MashOutPerInput = 2; public int MashOutMax = 20; public ThrowData ThrowUp = new() { Angle = -90 }; public ThrowData ThrowForward = new(); public ThrowData ThrowDown = new() { Angle = 90 }; public bool IsValid(); }`
  - `HitboxSpec.Type` (default `HitboxType.Damage`), `HitboxSpec.Push` (`Vector2`, default zero), `HitboxSpec.SearchId` (string, default `""`), `HitboxSpec.Grab` (`GrabData?`, default null)
  - `AttackData.Armor` (`List<ArmorSpec>`, inherited by `AttackStage`)

- [ ] **Step 1: Write the failing test**

Create `tests/ByteBrawl.Tests/BoxTypeDataTests.cs`:

```csharp
using ByteBrawl.Combat;
using Godot;
using Xunit;

namespace ByteBrawl.Tests;

public class BoxTypeDataTests
{
    [Fact] public void HitboxSpec_DefaultsToDamageType()
    {
        var spec = new HitboxSpec();
        Assert.Equal(HitboxType.Damage, spec.Type);
        Assert.Equal(Vector2.Zero, spec.Push);
        Assert.Null(spec.Grab);
    }

    [Fact] public void GrabData_DefaultsAreValid()
    {
        Assert.True(new GrabData().IsValid());
    }

    [Fact] public void GrabData_InvalidWhenHoldFramesNegative()
    {
        Assert.False(new GrabData { HoldFrames = -1 }.IsValid());
    }

    [Fact] public void GrabData_InvalidWhenThrowInvalid()
    {
        Assert.False(new GrabData { ThrowForward = new ThrowData { Damage = -1 } }.IsValid());
    }

    [Fact] public void AttackData_HasEmptyArmorListByDefault()
    {
        Assert.Empty(new AttackData().Armor);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/ByteBrawl.Tests --filter BoxTypeDataTests`
Expected: FAIL — `HitboxType`/`HurtboxType`/`LimbGroup`/`GrabData`/`ArmorSpec` do not exist; `HitboxSpec.Type` not found.

- [ ] **Step 3: Write minimal implementation**

Create `src/Combat/BoxTypes.cs`:

```csharp
namespace ByteBrawl.Combat;

// Typed-box taxonomy. Hitbox types describe what an attack spawns; hurtbox
// types describe the state of a body part (per-limb, set by the FSM).
// See docs/superpowers/specs/2026-09-25-hit-hurtbox-types-design.md.
public enum HitboxType { Damage, Wind, Grab, Search }
public enum HurtboxType { Vulnerable, Intangible, Invincible, SuperArmor, HyperArmor }
public enum LimbGroup { Head, Torso, Arm, Leg }

// One armor entry of an attack/stage: the given limb group takes on the
// given hurtbox type for the attack's active frames. SuperArmor uses
// BreakKbThreshold (hits with higher base knockback break it); HyperArmor
// leaves it null and never breaks.
public class ArmorSpec
{
    public LimbGroup Group;
    public HurtboxType Type;
    public float? BreakKbThreshold;
}
```

Create `src/Combat/GrabData.cs`:

```csharp
namespace ByteBrawl.Combat;

// Grab/throw data model — designed and validated, but the grab subsystem is
// NOT built yet: Grab-typed hitboxes render in debug and do nothing.
public class GrabData
{
    public int HoldFrames = 30;
    public float PummelDamage = 2;
    public int PummelInterval = 20;
    public float MashOutPerInput = 2;
    public int MashOutMax = 20;
    public ThrowData ThrowUp = new() { Angle = -90 };
    public ThrowData ThrowForward = new();
    public ThrowData ThrowDown = new() { Angle = 90 };

    public bool IsValid() =>
        HoldFrames >= 0 && PummelDamage >= 0 && PummelInterval > 0 &&
        MashOutPerInput >= 0 && MashOutMax >= 0 &&
        ThrowUp.IsValid() && ThrowForward.IsValid() && ThrowDown.IsValid();
}

public class ThrowData
{
    public float Damage = 6;
    public float BaseKnockback = 120;
    public float Scaling = 1f;
    public float Angle; // degrees, 0 = forward, negative = up

    public bool IsValid() => Damage >= 0 && BaseKnockback >= 0 && Scaling >= 0;
}
```

Modify `src/Combat/HitboxSpec.cs` — add `using Godot;` at the top and these fields inside the class:

```csharp
    public HitboxType Type = HitboxType.Damage;
    public Vector2 Push = Vector2.Zero;      // Wind: applied to victim velocity, no damage
    public string SearchId = "";             // Search: detection-only callback key
    public GrabData? Grab;                   // Grab: throw data (mechanics unbuilt)
```

Modify `src/Combat/AttackData.cs` — add inside the class:

```csharp
    public List<ArmorSpec> Armor = new();
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet build && dotnet test tests/ByteBrawl.Tests`
Expected: PASS — 28 tests (23 existing + 5 new).

- [ ] **Step 5: Commit**

```bash
git add src/Combat/BoxTypes.cs src/Combat/GrabData.cs src/Combat/HitboxSpec.cs src/Combat/AttackData.cs tests/ByteBrawl.Tests/BoxTypeDataTests.cs
git commit -m "feat: typed box taxonomy and grab data model"
```

---

### Task 2: MatchRules typed resolution + wind

**Files:**
- Modify: `src/Combat/MatchRules.cs` (the `ApplyHit` method at lines 24-48)
- Test: `tests/ByteBrawl.Tests/MatchRulesTests.cs`

**Interfaces:**
- Consumes: `HurtboxType`, `HitboxSpec`, `HitboxType` from Task 1.
- Produces:
  - `void MatchRules.ApplyHit(IFighter attacker, IFighter defender, AttackData attack, HurtboxType hurtboxType = HurtboxType.Vulnerable, float armorBreakKb = float.MaxValue)` — new optional params; existing calls/tests compile unchanged.
  - `event Action<IFighter>? MatchRules.HitBlocked` — raised on invincible contact (blocked flash seam).
  - `void MatchRules.ApplyWind(IFighter defender, HitboxSpec spec, int facing)` — adds `new Vector2(spec.Push.X * facing, spec.Push.Y)` to `defender.Velocity`; no damage/hitstun/meter.

- [ ] **Step 1: Write the failing test**

Append to `tests/ByteBrawl.Tests/MatchRulesTests.cs` (inside the class):

```csharp
    [Fact] public void Hit_IntangibleHurtbox_NoContactAtAll()
    {
        var (r, p1, p2) = NewMatch();
        r.ApplyHit(p1, p2, Attack(), HurtboxType.Intangible);
        Assert.Equal(0, p2.Damage);
        Assert.Equal(0, p1.Meter.Value); // no meter gain: contact never happened
    }

    [Fact] public void Hit_InvincibleHurtbox_BlocksAndRaisesHitBlocked()
    {
        var (r, p1, p2) = NewMatch();
        IFighter? blocked = null;
        r.HitBlocked += f => blocked = f;
        r.ApplyHit(p1, p2, Attack(), HurtboxType.Invincible);
        Assert.Equal(0, p2.Damage);
        Assert.Equal(Vector2.Zero, p2.Velocity);
        Assert.Same(p2, blocked);
    }

    [Fact] public void Hit_HyperArmor_DamagesWithoutKnockbackOrHitstun()
    {
        var (r, p1, p2) = NewMatch();
        r.ApplyHit(p1, p2, Attack(), HurtboxType.HyperArmor);
        Assert.Equal(10, p2.Damage);
        Assert.Equal(Vector2.Zero, p2.Velocity);
        Assert.Equal(0, p2.HitstunFrames);
    }

    [Fact] public void Hit_SuperArmorBelowThreshold_DamagesWithoutKnockback()
    {
        var (r, p1, p2) = NewMatch(); // attack base kb is 100
        r.ApplyHit(p1, p2, Attack(), HurtboxType.SuperArmor, armorBreakKb: 200);
        Assert.Equal(10, p2.Damage);
        Assert.Equal(Vector2.Zero, p2.Velocity);
        Assert.Equal(0, p2.HitstunFrames);
    }

    [Fact] public void Hit_SuperArmorAboveThreshold_BreaksIntoFullHit()
    {
        var (r, p1, p2) = NewMatch(); // attack base kb is 100
        r.ApplyHit(p1, p2, Attack(), HurtboxType.SuperArmor, armorBreakKb: 50);
        Assert.Equal(10, p2.Damage);
        Assert.Equal(100, p2.Velocity.X, 0.01f);
        Assert.Equal(10, p2.HitstunFrames);
    }

    [Fact] public void Wind_PushesWithoutDamageOrHitstun()
    {
        var (r, _, p2) = NewMatch();
        var spec = new HitboxSpec { Type = HitboxType.Wind, Push = new Vector2(60, -30) };
        r.ApplyWind(p2, spec, facing: 1);
        Assert.Equal(new Vector2(60, -30), p2.Velocity);
        Assert.Equal(0, p2.Damage);
        Assert.Equal(0, p2.HitstunFrames);
    }

    [Fact] public void Wind_FacingFlipsHorizontalPush()
    {
        var (r, _, p2) = NewMatch();
        var spec = new HitboxSpec { Type = HitboxType.Wind, Push = new Vector2(60, -30) };
        r.ApplyWind(p2, spec, facing: -1);
        Assert.Equal(new Vector2(-60, -30), p2.Velocity);
    }
```

Note: `ElementalMeter.Value` — check `src/Combat/ElementalMeter.cs` for the actual property name before writing this test; use whatever exposes the current meter amount. If it has no public getter, assert on `p2.Damage` only and drop the meter line.

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/ByteBrawl.Tests --filter "FullyQualifiedName~MatchRulesTests"`
Expected: FAIL — optional parameters / `HitBlocked` / `ApplyWind` do not exist (compile error).

- [ ] **Step 3: Write minimal implementation**

In `src/Combat/MatchRules.cs`:

Add the event inside the class next to `State`:

```csharp
    public event Action<IFighter>? HitBlocked; // invincible contact: flash only, no effect
```

Replace the `ApplyHit` method signature and insert the typed-resolution block so the method becomes:

```csharp
    public void ApplyHit(IFighter attacker, IFighter defender, AttackData attack,
        HurtboxType hurtboxType = HurtboxType.Vulnerable, float armorBreakKb = float.MaxValue)
    {
        if (State != MatchState.Active) return;
        if (hurtboxType == HurtboxType.Intangible) return; // no contact at all
        if (defender.InvincibleFrames > 0 || hurtboxType == HurtboxType.Invincible)
        {
            HitBlocked?.Invoke(defender);
            return;
        }

        attacker.Meter.AddFromDealt(attack.BaseDamage);
        defender.Meter.AddFromTaken(attack.BaseDamage);

        if (defender.ShieldActive && defender.ShieldHealth > 0)
        {
            defender.DamageShield(attack.BaseDamage * 3);
            defender.ApplyKnockback(new Vector2(
                attack.Direction.X * attacker.Facing * attack.BaseKnockback * 0.3f,
                attack.Direction.Y * attack.BaseKnockback * 0.3f));
            return;
        }

        // Armor: damage applies, no knockback/hitstun/interrupt. Super armor
        // breaks into a full hit when the attack's base knockback clears the
        // threshold; hyper armor never breaks.
        var armored = hurtboxType == HurtboxType.HyperArmor
            || (hurtboxType == HurtboxType.SuperArmor && attack.BaseKnockback <= armorBreakKb);
        var preDamage = defender.Damage;
        defender.TakeDamage(attack.BaseDamage);
        if (armored) return;
        var knockback = attack.BaseKnockback + preDamage * attack.Scaling;
        defender.ApplyKnockback(new Vector2(
            attack.Direction.X * attacker.Facing * knockback,
            attack.Direction.Y * knockback));
        defender.EnterHitstun(attack.HitstunFrames);
    }
```

Add `ApplyWind` right after `ApplyHit`:

```csharp
    // Wind hitboxes push without damage, hitstun, or meter.
    public void ApplyWind(IFighter defender, HitboxSpec spec, int facing)
    {
        if (State != MatchState.Active) return;
        defender.Velocity += new Vector2(spec.Push.X * facing, spec.Push.Y);
    }
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet build && dotnet test tests/ByteBrawl.Tests`
Expected: PASS — 30 tests (meter-line caveat from Step 1 applies).

- [ ] **Step 5: Commit**

```bash
git add src/Combat/MatchRules.cs tests/ByteBrawl.Tests/MatchRulesTests.cs
git commit -m "feat: typed hit resolution in match rules"
```

---

### Task 3: Hurtbox override seam — node side

**Files:**
- Modify: `src/Combat/IFighter.cs`
- Modify: `src/Nodes/Hurtbox.cs`
- Modify: `src/Nodes/LimbRig.cs`
- Modify: `src/Nodes/Fighter.cs`
- Modify: `src/Nodes/SmokeLimbRig.cs`
- Modify: `tests/ByteBrawl.Tests/TestDoubles.cs`

**Interfaces:**
- Consumes: `LimbGroup`, `HurtboxType` from Task 1.
- Produces:
  - `void IFighter.SetHurtboxOverride(LimbGroup group, HurtboxType? type, float armorBreakKb = float.MaxValue)` — the ONLY new IFighter member. `null` resets the group to `Vulnerable`.
  - `LimbGroup Hurtbox.Group`, `HurtboxType Hurtbox.CurrentType` (default `Vulnerable`), `float Hurtbox.ArmorBreakKb` (default `float.MaxValue`).
  - `static LimbGroup LimbRig.GroupFor(string partName)` — `Head`→Head; contains `Arm`/`Hand`→Arm; contains `Thigh`/`Shin`/`Foot`→Leg; everything else (Torso, Pelvis)→Torso. Backpack never gets a hurtbox.
  - `FakeFighter.HurtboxOverrides` (`Dictionary<LimbGroup, HurtboxType?>`) for FSM test assertions.

- [ ] **Step 1: Write the failing test**

In `tests/ByteBrawl.Tests/TestDoubles.cs`, add to `FakeFighter` (field near the others, method after `SetChargingFull`):

```csharp
    public readonly Dictionary<LimbGroup, HurtboxType?> HurtboxOverrides = new();
    public void SetHurtboxOverride(LimbGroup group, HurtboxType? type, float armorBreakKb = float.MaxValue)
        => HurtboxOverrides[group] = type;
```

Then the Godot smoke scene is the executable check (it fails to compile until the node side exists). Extend `src/Nodes/SmokeLimbRig.cs` — replace the `ok` expression with:

```csharp
        var ok = rig.Find("NearUpperArm").RotationDegrees == -90f
                 && rig.Find("NearForearm") != null
                 && rig.Find("NearThigh") != null
                 && rig.Find("FarFoot") != null
                 && CountLimbs(rig) == LimbRig.PartCount
                 && rig.Hurtboxes.Count == LimbRig.HurtboxCount
                 && rig.Hurtboxes.Count(h => h.Group == LimbGroup.Head) == 1
                 && rig.Hurtboxes.Count(h => h.Group == LimbGroup.Torso) == 2
                 && rig.Hurtboxes.Count(h => h.Group == LimbGroup.Arm) == 6
                 && rig.Hurtboxes.Count(h => h.Group == LimbGroup.Leg) == 6;
```

Add `using ByteBrawl.Combat;` at the top of `SmokeLimbRig.cs`.

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet build`
Expected: FAIL — `IFighter` lacks `SetHurtboxOverride`; `Hurtbox.Group` does not exist; `FakeFighter` doesn't implement the interface member (compile errors).

- [ ] **Step 3: Write minimal implementation**

`src/Combat/IFighter.cs` — add the method after `SetChargingFull`:

```csharp
    // Per-limb hurtbox state. null resets the group to Vulnerable. The FSM
    // drives this (dodge intangibility, attack-stage armor); Fighter maps
    // groups onto its rig's Hurtbox nodes.
    void SetHurtboxOverride(LimbGroup group, HurtboxType? type, float armorBreakKb = float.MaxValue);
```

`src/Nodes/Hurtbox.cs` — replace the class body fields with:

```csharp
public partial class Hurtbox : Area2D
{
    public Fighter? OwnerFighter;
    public float Radius;
    public float Height;
    public LimbGroup Group;
    public HurtboxType CurrentType = HurtboxType.Vulnerable;
    public float ArmorBreakKb = float.MaxValue; // only meaningful for SuperArmor
}
```

Add `using ByteBrawl.Combat;` at the top of `Hurtbox.cs`.

`src/Nodes/LimbRig.cs` — add the mapping helper and assign it in `AttachHurtbox`:

```csharp
    // Part-name -> group mapping; see spec. Backpack has no hurtbox.
    public static LimbGroup GroupFor(string partName) =>
        partName.Contains("Head") ? LimbGroup.Head
        : partName.Contains("Arm") || partName.Contains("Hand") ? LimbGroup.Arm
        : partName.Contains("Thigh") || partName.Contains("Shin") || partName.Contains("Foot") ? LimbGroup.Leg
        : LimbGroup.Torso; // Torso, Pelvis
```

In `AttachHurtbox`, change the `area` initializer to include `Group = GroupFor(limb.Name)`:

```csharp
        var area = new Hurtbox { Radius = radius, Height = height, Position = center, Group = GroupFor(limb.Name) };
```

`src/Nodes/Fighter.cs` — add the interface method after `SetChargingFull`:

```csharp
    public void SetHurtboxOverride(LimbGroup group, HurtboxType? type, float armorBreakKb = float.MaxValue)
    {
        foreach (var hurtbox in _rig.Hurtboxes)
        {
            if (hurtbox.Group != group) continue;
            hurtbox.CurrentType = type ?? HurtboxType.Vulnerable;
            hurtbox.ArmorBreakKb = type == HurtboxType.SuperArmor ? armorBreakKb : float.MaxValue;
        }
    }
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet build && dotnet test tests/ByteBrawl.Tests && timeout 30 godot --headless --path . res://scenes/smoke_limb_rig.tscn`
Expected: build clean, 30 tests PASS, smoke prints `SMOKE PASS`.

- [ ] **Step 5: Commit**

```bash
git add src/Combat/IFighter.cs src/Nodes/Hurtbox.cs src/Nodes/LimbRig.cs src/Nodes/Fighter.cs src/Nodes/SmokeLimbRig.cs tests/ByteBrawl.Tests/TestDoubles.cs
git commit -m "feat: per-limb hurtbox override seam"
```

---

### Task 4: FSM armor application + dodge intangibility + Byte heavy armor

**Files:**
- Modify: `src/Combat/FighterStateMachine.cs`
- Modify: `src/Data/ByteMoveset.cs`
- Test: `tests/ByteBrawl.Tests/FighterStateMachineAttackTests.cs`
- Test: `tests/ByteBrawl.Tests/CombatDataTests.cs`

**Interfaces:**
- Consumes: `IFighter.SetHurtboxOverride` (Task 3), `ArmorSpec`/`AttackData.Armor` (Task 1).
- Produces:
  - `AttackData.Armor` honored: flat attacks arm for `ActiveFrames` from attack start; stages arm for their `ActiveFrames` from their `SpawnFrame`; overrides clear when the window ends or the fighter is interrupted (hitstun).
  - Dodges make all four limb groups `Intangible` for the dodge duration (the existing `EnterInvincibility` call stays — it is the damage gate asserted by existing tests; intangible is the contact gate).
  - `ByteMoveset` heavy (`sword-heavy`) carries `Armor = { (Arm, HyperArmor, null) }`.
  - Charged attacks preserve armor: `FireCharged` copies `Armor` into the charged `AttackData`.

- [ ] **Step 1: Write the failing test**

Append to `tests/ByteBrawl.Tests/CombatDataTests.cs` (inside the class):

```csharp
    [Fact]
    public void ByteHeavy_ArmsHyperArmorDuringActiveFrames()
    {
        var heavy = ByteMoveset.Create().Get(AttackSlot.NeutralHeavy);
        var armor = Assert.Single(heavy.Armor);
        Assert.Equal(LimbGroup.Arm, armor.Group);
        Assert.Equal(HurtboxType.HyperArmor, armor.Type);
        Assert.Null(armor.BreakKbThreshold);
    }
```

Append to `tests/ByteBrawl.Tests/FighterStateMachineAttackTests.cs` (inside the class):

```csharp
    private static (FighterStateMachine, FakeFighter, FakeHitboxManager) NewArmoredFsm()
    {
        var stage = new AttackStage
        {
            Id = "armored", BaseDamage = 5, BaseKnockback = 50, Scaling = 0,
            Direction = new Vector2(1, 0), HitstunFrames = 5, ActiveFrames = 4, SpawnFrame = 2,
            Hitboxes = { new HitboxSpec { OffsetX = 10, Radius = 6 } },
            Armor = { new ArmorSpec { Group = LimbGroup.Arm, Type = HurtboxType.HyperArmor } },
        };
        var attack = new AttackData { Id = "armored-atk", ActiveFrames = 4, Stages = { stage } };
        var moveset = new Moveset { Stats = new FighterStats { RunSpeed = 120, JumpSpeed = 280, Weight = 1f } };
        moveset.Attacks[AttackSlot.NeutralLight] = attack;
        var f = new FakeFighter();
        var hb = new FakeHitboxManager();
        return (new FighterStateMachine(f, hb, moveset), f, hb);
    }

    [Fact] public void StageArmor_AppliesAtSpawnFrame()
    {
        var (fsm, f, _) = NewArmoredFsm();
        fsm.Update(Neutral() with { AttackLight = true }); // frame 0: attack starts
        fsm.Update(Neutral()); // frame 1: before SpawnFrame 2
        Assert.False(f.HurtboxOverrides.ContainsKey(LimbGroup.Arm));
        fsm.Update(Neutral()); // frame 2: stage spawns
        Assert.Equal(HurtboxType.HyperArmor, f.HurtboxOverrides[LimbGroup.Arm]);
    }

    [Fact] public void StageArmor_ClearsAfterActiveFrames()
    {
        var (fsm, f, _) = NewArmoredFsm();
        fsm.Update(Neutral() with { AttackLight = true });
        for (var i = 0; i < 6; i++) fsm.Update(Neutral()); // frame 2+4: expired
        Assert.False(f.HurtboxOverrides.TryGetValue(LimbGroup.Arm, out var t) && t.HasValue);
    }

    [Fact] public void SpotDodge_SetsAllGroupsIntangibleAndClearsOnExit()
    {
        var (fsm, f, _) = NewFsm();
        f.Grounded = true;
        fsm.Update(Neutral() with { ShieldPressed = true });
        foreach (LimbGroup g in Enum.GetValues<LimbGroup>())
            Assert.Equal(HurtboxType.Intangible, f.HurtboxOverrides[g]);
        for (var i = 0; i < 25; i++) fsm.Update(Neutral());
        Assert.Equal(FighterState.Idle, fsm.CurrentState);
        foreach (LimbGroup g in Enum.GetValues<LimbGroup>())
            Assert.True(!f.HurtboxOverrides.TryGetValue(g, out var t) || t is null or HurtboxType.Vulnerable);
    }

    [Fact] public void AirDodge_SetsAllGroupsIntangible()
    {
        var (fsm, f, _) = NewFsm();
        f.Grounded = false;
        fsm.Update(Neutral() with { ShieldPressed = true });
        foreach (LimbGroup g in Enum.GetValues<LimbGroup>())
            Assert.Equal(HurtboxType.Intangible, f.HurtboxOverrides[g]);
    }
```

Check `FighterStateMachineMovementTests.cs` for the exact `ActionFrame` shield-field spelling (`ShieldPressed` vs something else) before writing — copy whatever that file uses for the spot-dodge test input.

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/ByteBrawl.Tests --filter "FullyQualifiedName~FighterStateMachineAttackTests|FullyQualifiedName~CombatDataTests"`
Expected: FAIL — `ByteMoveset` heavy has no armor yet; FSM never calls `SetHurtboxOverride`.

- [ ] **Step 3: Write minimal implementation**

`src/Combat/FighterStateMachine.cs`:

Add fields near the other private fields:

```csharp
    private int _armorUntilFrame = -1;
    private readonly List<LimbGroup> _armoredGroups = new();
    private bool _intangibleActive;
```

Add helpers (e.g. after `SetState`):

```csharp
    private void SetIntangible(bool on)
    {
        foreach (LimbGroup g in Enum.GetValues<LimbGroup>())
            _fighter.SetHurtboxOverride(g, on ? HurtboxType.Intangible : null);
        _intangibleActive = on;
    }

    private void ApplyArmor(AttackData attack, int expiryFrame)
    {
        if (attack.Armor.Count == 0) return;
        foreach (var a in attack.Armor)
        {
            _fighter.SetHurtboxOverride(a.Group, a.Type, a.BreakKbThreshold ?? float.MaxValue);
            if (!_armoredGroups.Contains(a.Group)) _armoredGroups.Add(a.Group);
        }
        _armorUntilFrame = Math.Max(_armorUntilFrame, expiryFrame);
    }

    // Hitstun can interrupt an armored/intangible window (armor covers some
    // limbs, not all) — never leave stale overrides behind.
    private void ClearCombatOverrides()
    {
        if (_intangibleActive) SetIntangible(false);
        foreach (var g in _armoredGroups) _fighter.SetHurtboxOverride(g, null);
        _armoredGroups.Clear();
        _armorUntilFrame = -1;
    }
```

In `Update`, at the top of the hitstun branch:

```csharp
        if (_fighter.HitstunFrames > 0)
        {
            ClearCombatOverrides();
            _fighter.DeactivateShield();
            ...
```

In the `SpotDodge` block, clear intangibility at both exits:

```csharp
            if (_stateFrames >= SpotDodgeTapThreshold && actions.ShieldHeld)
            {
                _fighter.InvincibleFrames = 0;
                if (_intangibleActive) SetIntangible(false);
                _fighter.ShieldActive = true;
                SetState(FighterState.Shield);
                return;
            }
            if (_stateFrames >= SpotDodgeFrames)
            {
                if (_intangibleActive) SetIntangible(false);
                SetState(FighterState.Idle);
            }
```

In the `AirDodge` block:

```csharp
            if (_stateFrames >= AirDodgeFrames)
            {
                if (_intangibleActive) SetIntangible(false);
                _fighter.Velocity = new Vector2(_fighter.Velocity.X, 80);
                SetState(FighterState.Fall);
            }
```

In `TickAttack`, add armor expiry check before the stage-spawn loop, and apply stage armor when spawning:

```csharp
        _stateFrames++;
        if (_armorUntilFrame >= 0 && _stateFrames >= _armorUntilFrame)
        {
            foreach (var g in _armoredGroups) _fighter.SetHurtboxOverride(g, null);
            _armoredGroups.Clear();
            _armorUntilFrame = -1;
        }
        while (_nextStageIndex < _sequence.Length &&
               _stateFrames >= _sequence[_nextStageIndex].SpawnFrame)
        {
            var stage = _sequence[_nextStageIndex++];
            ApplyArmor(stage, _stateFrames + stage.ActiveFrames);
            foreach (var spec in stage.Hitboxes)
                _hitboxes.Spawn(_fighter, stage, spec);
        }
```

In `StartAttack`, flat (non-stage) attacks: after the `if (_sequence.Length == 0)` spawn block, add armor — flat attack starts at frame 0, so it expires after `ActiveFrames`:

```csharp
        if (_sequence.Length == 0)
        {
            foreach (var spec in attack.Hitboxes)
                _hitboxes.Spawn(_fighter, attack, spec);
            ApplyArmor(attack, attack.ActiveFrames);
        }
```

(If the existing flat-spawn is a single `if` without braces, wrap it as above.)

In `SetState`, dodge entry (alongside the existing `EnterInvincibility` line):

```csharp
        if (newState == FighterState.SpotDodge)
        {
            _fighter.EnterInvincibility(SpotDodgeFrames);
            SetIntangible(true);
        }
        if (newState == FighterState.AirDodge) SetIntangible(true);
```

In `FireCharged`, add armor to the charged attack initializer (next to `Hitboxes = _chargeAttack.Hitboxes`):

```csharp
            Armor = _chargeAttack.Armor,
```

`src/Data/ByteMoveset.cs` — add to the `swordHeavy` initializer:

```csharp
            Armor = { new ArmorSpec { Group = LimbGroup.Arm, Type = HurtboxType.HyperArmor } },
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet build && dotnet test tests/ByteBrawl.Tests`
Expected: PASS — 36 tests.

- [ ] **Step 5: Commit**

```bash
git add src/Combat/FighterStateMachine.cs src/Data/ByteMoveset.cs tests/ByteBrawl.Tests/FighterStateMachineAttackTests.cs tests/ByteBrawl.Tests/CombatDataTests.cs
git commit -m "feat: attack-stage armor and dodge intangibility"
```

---

### Task 5: HitboxManager typed dispatch

**Files:**
- Modify: `src/Nodes/HitboxManager.cs`

**Interfaces:**
- Consumes: `MatchRules.ApplyHit`/`ApplyWind`/`HitBlocked` (Task 2), `Hurtbox.CurrentType`/`ArmorBreakKb` (Task 3), `HitboxSpec.Type`/`Push`/`SearchId` (Task 1).
- Produces:
  - `event Action<Fighter, string>? HitboxManager.SearchTriggered` — fired once per search box on contact.
  - `HitboxManager.BlockedFlashes` (`List<(Vector2 Pos, int Frames)>`) — decaying positions for the debug blocked-flash ring; populated by subscribing to `Rules.HitBlocked` (wire `Rules` as a subscribing property so reassignment doesn't double-subscribe).
  - Dispatch: `Grab` specs spawn a debug-visible box with **no** contact handler (specced, unbuilt); `Wind` pushes every overlapping frame without consuming `HasHit`; `Search` fires once and consumes `HasHit`; `Damage` resolves via `ApplyHit` with the touched hurtbox's type/threshold.

- [ ] **Step 1: Implement**

Replace the body of `src/Nodes/HitboxManager.cs` (keep namespace/usings/class declaration and `_PhysicsProcess` freeing logic) with:

```csharp
public partial class HitboxManager : Node, IHitboxManager
{
    public event Action<Fighter, string>? SearchTriggered;

    // Debug overlay: positions of recently blocked (invincible) contacts.
    public readonly List<(Vector2 Pos, int Frames)> BlockedFlashes = new();

    private MatchRules? _rules;
    public MatchRules? Rules
    {
        get => _rules;
        set
        {
            if (_rules != null) _rules.HitBlocked -= OnHitBlocked;
            _rules = value;
            if (_rules != null) _rules.HitBlocked += OnHitBlocked;
        }
    }

    private void OnHitBlocked(IFighter defender) => BlockedFlashes.Add((defender.Position, 10));

    public void Spawn(IFighter attacker, AttackData attack, HitboxSpec spec)
    {
        if (attacker is not Fighter a || Rules == null) return;
        var shape = new CollisionShape2D { Shape = new CircleShape2D { Radius = spec.Radius } };
        var hb = new Hitbox
        {
            Attacker = a, Spec = spec, FramesRemaining = attack.ActiveFrames,
            Position = a.Position + new Vector2(spec.OffsetX * a.Facing, spec.OffsetY),
            Attack = EffectiveAttack(attack, spec),
        };
        hb.AddChild(shape);
        // Grab boxes are specced but unbuilt: they exist for the debug overlay only.
        if (spec.Type != HitboxType.Grab)
        {
            hb.AreaEntered += area =>
            {
                if (area is not Hurtbox hurt || hurt.OwnerFighter is not { } defender) return;
                if (defender == hb.Attacker) return;
                if (hurt.CurrentType == HurtboxType.Intangible) return;
                switch (hb.Spec.Type)
                {
                    case HitboxType.Wind:
                        Rules.ApplyWind(defender, hb.Spec, hb.Attacker.Facing);
                        break;
                    case HitboxType.Search:
                        if (!hb.HasHit)
                        {
                            SearchTriggered?.Invoke(hb.Attacker, hb.Spec.SearchId);
                            hb.HasHit = true;
                        }
                        break;
                    default:
                        if (hb.HasHit) break;
                        Rules.ApplyHit(hb.Attacker, defender, hb.Attack, hurt.CurrentType, hurt.ArmorBreakKb);
                        hb.HasHit = true;
                        break;
                }
            };
        }
        AddChild(hb);
    }

    // Sweet/sour spot support: a spec may override damage/knockback for its circle.
    private static AttackData EffectiveAttack(AttackData attack, HitboxSpec spec)
    {
        if (spec.DamageOverride is null && spec.KnockbackOverride is null)
            return attack;
        return new AttackData
        {
            Id = attack.Id,
            BaseDamage = spec.DamageOverride ?? attack.BaseDamage,
            BaseKnockback = spec.KnockbackOverride ?? attack.BaseKnockback,
            Scaling = attack.Scaling,
            Direction = attack.Direction,
            HitstunFrames = attack.HitstunFrames,
            ActiveFrames = attack.ActiveFrames,
        };
    }

    public override void _PhysicsProcess(double delta)
    {
        foreach (var child in GetChildren())
        {
            if (child is not Hitbox hb) continue;
            hb.FramesRemaining--;
            if (hb.FramesRemaining <= 0) hb.QueueFree();
        }
        for (var i = BlockedFlashes.Count - 1; i >= 0; i--)
        {
            var flash = BlockedFlashes[i];
            if (--flash.Frames <= 0) BlockedFlashes.RemoveAt(i);
            else BlockedFlashes[i] = flash;
        }
    }
}
```

- [ ] **Step 2: Verify build and tests**

Run: `dotnet build && dotnet test tests/ByteBrawl.Tests`
Expected: clean build, 36 tests PASS (node code has no direct unit tests; the arena smoke scene exercises it).

- [ ] **Step 3: Commit**

```bash
git add src/Nodes/HitboxManager.cs
git commit -m "feat: typed hitbox dispatch in hitbox manager"
```

---

### Task 6: Debug overlay colors

**Files:**
- Modify: `src/Nodes/CombatDebugDraw.cs`

**Interfaces:**
- Consumes: everything from Tasks 1-5 (`HitboxSpec.Type`, `Hurtbox.CurrentType`, `HitboxManager.BlockedFlashes`, `Fighter.InvincibleFrames`).
- Produces: visual only — no new APIs.

- [ ] **Step 1: Implement**

In `src/Nodes/CombatDebugDraw.cs`:

Replace the hurtbox-drawing block inside `_Draw()` (the `if (ShowHurtboxes)` section, from `var fill = ...` through the end of the hurtbox loop) with:

```csharp
            var blink = Time.GetTicksMsec() % 266 < 133;
            foreach (var f in new[] { P1, P2 })
            {
                if (f == null) continue;
                var scale = Mathf.Abs(f.Rig.Scale.X);
                var fighterInvincible = f.InvincibleFrames > 0;
                foreach (var hurtbox in f.Rig.Hurtboxes)
                {
                    var type = fighterInvincible ? HurtboxType.Invincible : hurtbox.CurrentType;
                    var (fill, outline) = HurtboxStyle(type, blink);
                    // Capsule: thick line along the segment + cap circles, with outline.
                    var t = hurtbox.GlobalTransform;
                    var r = hurtbox.Radius * scale;
                    var half = (hurtbox.Height * 0.5f - hurtbox.Radius) * scale;
                    var a = t * new Vector2(0, -half);
                    var b = t * new Vector2(0, half);
                    DrawLine(a, b, outline, r * 2 + 1.5f);
                    DrawCircle(a, r + 0.75f, outline);
                    DrawCircle(b, r + 0.75f, outline);
                    DrawLine(a, b, fill, r * 2);
                    DrawCircle(a, r, fill);
                    DrawCircle(b, r, fill);
                }
            }
```

Replace the hitbox-drawing block (the `foreach (var child in Hitboxes.GetChildren())` loop) with:

```csharp
        foreach (var child in Hitboxes.GetChildren())
        {
            if (child is not Hitbox hb) continue;
            var fill = HitboxFill(hb);
            DrawCircle(hb.Position, hb.Spec.Radius, fill);
            if (hb.Spec.Type == HitboxType.Damage)
            {
                var dir = hb.Attack.Direction * hb.Attacker.Facing * 20f;
                DrawLine(hb.Position, hb.Position + dir, new Color(1, 1, 0), 1f);
            }
        }
        foreach (var (pos, frames) in Hitboxes.BlockedFlashes)
        {
            var alpha = frames / 20f;
            DrawCircle(pos, 14f, new Color(1, 1, 1, alpha));
            DrawArc(pos, 14f, 0, Mathf.Tau, 24, new Color(1, 1, 1, alpha * 2), 1.5f);
        }
```

Add the two style helpers to the class:

```csharp
    // Smash-convention colors per spec; near-transparent fill, brighter outline.
    private static (Color Fill, Color Outline) HurtboxStyle(HurtboxType type, bool blink) => type switch
    {
        HurtboxType.Intangible => (new Color(0.4f, 0.6f, 1f, 0.10f), new Color(0.4f, 0.6f, 1f, 0.30f)),
        HurtboxType.Invincible => blink
            ? (new Color(1, 1, 1, 0.30f), new Color(1, 1, 1, 0.55f))
            : (new Color(1, 1, 1, 0.06f), new Color(1, 1, 1, 0.15f)),
        HurtboxType.SuperArmor => (new Color(1f, 0.6f, 0.1f, 0.18f), new Color(1f, 0.6f, 0.1f, 0.40f)),
        HurtboxType.HyperArmor => (new Color(0.7f, 0.3f, 1f, 0.18f), new Color(0.7f, 0.3f, 1f, 0.40f)),
        _ => (new Color(0.75f, 1f, 0.85f, 0.15f), new Color(0.75f, 1f, 0.85f, 0.35f)),
    };

    private static Color HitboxFill(Hitbox hb) => hb.Spec.Type switch
    {
        HitboxType.Wind => new Color(0.3f, 0.9f, 1f, 0.35f),
        HitboxType.Grab => new Color(1f, 0.9f, 0.2f, 0.35f),
        HitboxType.Search => new Color(0.6f, 0.6f, 0.6f, 0.30f),
        _ => DamageFill(hb),
    };

    // Damage hitboxes fade from bright (early active frames) to dark.
    private static Color DamageFill(Hitbox hb)
    {
        var age = 1f - hb.FramesRemaining / (float)Math.Max(1, hb.Attack.ActiveFrames);
        return new Color(1, 0, 0, 0.55f - 0.3f * age);
    }
```

- [ ] **Step 2: Verify build, tests, and smokes**

Run:

```bash
dotnet build && dotnet test tests/ByteBrawl.Tests \
  && timeout 30 godot --headless --path . res://scenes/smoke_arena.tscn \
  && timeout 30 godot --headless --path . res://scenes/smoke_limb_rig.tscn
```

Expected: clean build, 36 tests PASS, both smokes print `SMOKE PASS`.

- [ ] **Step 3: Commit**

```bash
git add src/Nodes/CombatDebugDraw.cs
git commit -m "feat: smash-convention debug colors for typed boxes"
```

---

### Task 7: Final verification

- [ ] **Step 1: Full check**

```bash
dotnet build && dotnet test tests/ByteBrawl.Tests \
  && timeout 30 godot --headless --path . res://scenes/smoke_arena.tscn \
  && timeout 30 godot --headless --path . res://scenes/smoke_limb_rig.tscn
```

Expected: 36/36 tests, both smokes `SMOKE PASS`. If anything fails, fix and amend the relevant commit before reporting done.

- [ ] **Step 2: Sanity-check one behavior in the live scene (manual, report to user)**

Byte's heavy is chargeable, so land the armor check by releasing a heavy: during the heavy's active frames the near+far arm capsules draw purple (hyper armor) in the F3 overlay. This is a manual visual check for the user — do not automate it.

## Self-Review Notes (for the reviewer)

- Spec deviation, intentional: grab hitboxes live for their full `ActiveFrames` (debug-visible) instead of the spec's "expire on first tick" — visibility beats literal compliance for an unbuilt mechanic. Everything else maps 1:1 to spec sections.
- The existing `InvincibleFrames` mechanism is NOT removed: it stays the damage gate (respawn + dodge, asserted by existing tests), while `Intangible` is the contact gate. Spec said "replaces hardcoded invuln flags" — implemented as: dodges set intangible *in addition*; no behavior regresses and the debug overlay shows blue dodges immediately.
- `ElementalMeter` property name is unverified (see Task 2 Step 1 note) — adjust the single meter assertion to the real API.
- `ActionFrame` shield-field spelling is unverified (see Task 4 Step 1 note) — copy from `FighterStateMachineMovementTests.cs`.
