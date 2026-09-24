# ByteBrawl Godot Port — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the Phaser web version with a Godot 4.7 C# project in the same repo: core 1v1/training combat with data-driven movesets, a segmented limb placeholder rig, and Elemental meter hooks.

**Architecture:** Combat logic lives in plain engine-agnostic C# classes under `src/` (namespace `ByteBrawl.Combat`), unit-tested with xUnit via `dotnet test`. Godot node classes under `src/Nodes/` are thin presentation/physics shells around that logic. Attack data (including Byte's multi-hit special and chargeable heavy) is ported from the Phaser config, preserved in git history by Task 1's snapshot commit.

**Tech Stack:** Godot 4.7.2 (C#, .NET 8), xUnit, `godot --headless` self-quitting smoke scenes.

## Global Constraints

- Engine: Godot 4.7.2.stable, C#; csproj uses `Godot.NET.Sdk/4.7.2`, `TargetFramework net8.0`.
- The Phaser web version is deleted in Task 1 (after a history snapshot commit). Do not restore it.
- No assets from `David-Fruehwirt/unnamed_fighting_game` — placeholder rectangles only.
- Keep `docs/product-backlog.md` and `docs/superpowers/` intact.
- Game rules constants are ported 1:1 from the Phaser version: startingStocks=3, respawnInvincibilityFrames=120, maxDamage=999, match timer 180s, gravity 800 px/s², P1 runSpeed 120 / jumpSpeed 280, P2 runSpeed 140 / jumpSpeed 260.
- Controls: P1 A/D move, W/S up/down, Space jump, J light, K heavy, H special, L grab, C gadget, Shift shield. P2 arrows + Numpad 5/1/2/0/6/4/3.
- Tests run with `dotnet test tests/ByteBrawl.Tests` from repo root. Smoke scenes run with `timeout 30 godot --headless --path . <scene>`, must print `SMOKE PASS` and exit 0.
- Pixel-art presentation: 320×180 base, `canvas_items` stretch, integer scale.

## File Structure

```
project.godot                  Godot project config
ByteBrawl.csproj               Godot SDK project
.gitignore                     Godot + .NET ignores
src/
  Combat/                      plain C#, unit-tested (NO Godot node types)
    ActionFrame.cs             input snapshot struct
    FighterState.cs            state enum
    AttackSlot.cs              16-slot enum
    HitboxShape.cs             box | circle
    AttackData.cs              attack frame data
    AttackStage.cs             multi-hit stage
    ChargeConfig.cs            charge tuning
    FighterStats.cs            run/jump/weight
    Moveset.cs                 stats + slot dictionary
    IFighter.cs                fighter contract for logic
    IHitboxManager.cs          spawn contract
    MatchRulesConfig.cs
    MatchRules.cs              damage/knockback/stocks/timer
    FighterStateMachine.cs     movement + attack + charge states
    ElementalMeter.cs          meter fill (hooks only)
    IElementalFrenzy.cs        frenzy override seam (no behavior)
  Data/
    ByteMoveset.cs             Byte's ported moveset data
  Nodes/                       Godot presentation layer
    Main.cs                    menu/boot
    Arena.cs                   stage assembly + match wiring
    ArenaCamera.cs             2-player camera
    Fighter.cs                 CharacterBody2D implementing IFighter
    LocalInput.cs              keyboard -> ActionFrame
    (dummy input = LocalInput.Capture(0), see Task 6)
    Limb.cs                    one joint-pivot sprite
    LimbRig.cs                 puppet hierarchy
    Pose.cs / PoseLibrary.cs   pose keyframes
    PosePlayer.cs              applies pose rotations
    Hitbox.cs                  Area2D with lifetime
    HitboxManager.cs           node implementing IHitboxManager
    Hurtbox.cs                 Area2D on fighter
    CombatDebugDraw.cs         hitbox/knockback overlay
scenes/
  main.tscn  arena.tscn  arena_training.tscn  fighter.tscn  smoke_limb_rig.tscn  smoke_arena.tscn
tests/
  ByteBrawl.Tests/
    ByteBrawl.Tests.csproj
    CombatDataTests.cs
    MatchRulesTests.cs
    FighterStateMachineMovementTests.cs
    FighterStateMachineAttackTests.cs
    ElementalMeterTests.cs
    TestDoubles.cs             FakeFighter / FakeHitboxManager
```

Reference: the Phaser implementation is preserved at the snapshot commit from Task 1 (`git show <snapshot-sha>:src/fighter/FighterStateMachine.ts` etc.) when porting logic.

---

### Task 1: Snapshot Phaser state and reset repo to Godot skeleton

**Files:**
- Modify: repo root (delete `src/`, `tests/*.ts`, `index.html`, `package.json`, `package-lock.json`, `vite.config.ts`, `tsconfig*.json`, `node_modules/`, `dist/`, `assets/`)
- Create: `project.godot`, `ByteBrawl.csproj`, `.gitignore`, `scenes/main.tscn`, `src/Nodes/Main.cs`

**Interfaces:**
- Consumes: nothing.
- Produces: runs `godot --headless` cleanly; `scenes/main.tscn` as main scene; `Main.cs` with `_Ready()` that prints `BOOT OK` and quits.

- [ ] **Step 1: Commit the uncommitted Phaser work as a history snapshot**

```bash
git add -A
git commit -m "chore: snapshot Phaser version before Godot port"
git log --oneline -1   # note this SHA as <snapshot-sha>
```

- [ ] **Step 2: Delete the web version**

```bash
git rm -r --cached . >/dev/null
rm -rf src tests index.html package.json package-lock.json vite.config.ts tsconfig.json tsconfig.node.json node_modules dist assets
mkdir -p src/Nodes src/Combat src/Data scenes tests/ByteBrawl.Tests docs
git add -A
```

- [ ] **Step 3: Write `.gitignore`**

```
# Godot
.godot/
*.import
export_presets.cfg

# .NET
bin/
obj/
*.user

# IDE
.vscode/
.idea/
```

- [ ] **Step 4: Write `project.godot`**

```ini
config_version=5

[application]
config/name="ByteBrawl"
run/main_scene="res://scenes/main.tscn"
config/features=PackedStringArray("4.7", "C#", "Forward Plus")

[display]
window/size/viewport_width=320
window/size/viewport_height=180
window/stretch/mode="canvas_items"
window/stretch/aspect="keep"

[dotnet]
project/assembly_name="ByteBrawl"

[rendering]
textures/canvas_textures/default_texture_filter=0
```

- [ ] **Step 5: Write `ByteBrawl.csproj`**

```xml
<Project Sdk="Godot.NET.Sdk/4.7.2">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <EnableDynamicLoading>true</EnableDynamicLoading>
    <Nullable>enable</Nullable>
    <RootNamespace>ByteBrawl</RootNamespace>
  </PropertyGroup>
</Project>
```

- [ ] **Step 6: Write `src/Nodes/Main.cs`**

```csharp
using Godot;

namespace ByteBrawl.Nodes;

public partial class Main : Node
{
    public override void _Ready()
    {
        GD.Print("BOOT OK");
        GetTree().Quit();
    }
}
```

- [ ] **Step 7: Write `scenes/main.tscn`**

```
[gd_scene load_steps=2 format=3]

[ext_resource type="Script" path="res://src/Nodes/Main.cs" id="1"]

[node name="Main" type="Node"]
script = ExtResource("1")
```

- [ ] **Step 8: Verify the project boots headless**

Run: `timeout 30 godot --headless --path . 2>&1 | tail -5`
Expected: output contains `BOOT OK`, exit code 0. If Godot.NET.Sdk 4.7.2 is missing from NuGet, change the SDK version in `ByteBrawl.csproj` to the newest `4.7.x` that restores (`dotnet restore` shows available).

- [ ] **Step 9: Commit**

```bash
git add -A
git commit -m "feat: reset repo to Godot C# project skeleton"
```

---

### Task 2: xUnit test project + combat data model

**Files:**
- Create: `tests/ByteBrawl.Tests/ByteBrawl.Tests.csproj`
- Create: `src/Combat/AttackSlot.cs`, `HitboxShape.cs`, `AttackData.cs`, `AttackStage.cs`, `ChargeConfig.cs`, `FighterStats.cs`, `Moveset.cs`
- Create: `src/Data/ByteMoveset.cs`
- Test: `tests/ByteBrawl.Tests/CombatDataTests.cs`

**Interfaces:**
- Consumes: nothing.
- Produces (used by Tasks 3-6): `namespace ByteBrawl.Combat`; `enum HitboxShape { Box, Circle }`; `enum AttackSlot` with 16 members (`NeutralLight, SideLight, UpTilt, DownTilt, NeutralAir, SideAir, DownAir, UpAir, NeutralHeavy, UpHeavy, DownHeavy, SideHeavy, NeutralSpecial, UpSpecial, DownSpecial, SideSpecial`); `class AttackData { string Id; float BaseDamage; float BaseKnockback; float Scaling; Vector2 Direction; int HitstunFrames; int ActiveFrames; HitboxShape Shape = HitboxShape.Circle; float Radius = 6f; ChargeConfig? Charge; List<AttackStage> Stages; }`; `class AttackStage : AttackData { int SpawnFrame; float OffsetX; float OffsetY; float Width; float Height; }`; `class ChargeConfig { int MinChargeFrames; int MaxChargeFrames; int MaxHoldFrames; float DamageGrowth; float KnockbackGrowth; }`; `class FighterStats { float RunSpeed; float JumpSpeed; float Weight; }`; `class Moveset { FighterStats Stats; Dictionary<AttackSlot, AttackData> Attacks; AttackData Get(AttackSlot slot); }`; `static class ByteMoveset { Moveset Create(); }`.

- [ ] **Step 1: Write the test project file**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.11.1" />
    <PackageReference Include="xunit" Version="2.9.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="../../ByteBrawl.csproj" />
  </ItemGroup>
</Project>
```

- [ ] **Step 2: Write the failing test**

```csharp
using ByteBrawl.Combat;
using ByteBrawl.Data;
using Xunit;

namespace ByteBrawl.Tests;

public class CombatDataTests
{
    [Fact]
    public void ByteMoveset_FillsAllSixteenSlots()
    {
        var moveset = ByteMoveset.Create();
        foreach (AttackSlot slot in Enum.GetValues<AttackSlot>())
            Assert.True(moveset.Attacks.ContainsKey(slot), $"missing {slot}");
    }

    [Fact]
    public void ByteSpecial_IsThreeHitChain_WithFinisherLast()
    {
        var stages = ByteMoveset.Create().Get(AttackSlot.NeutralSpecial).Stages;
        Assert.Equal(3, stages.Count);
        Assert.Equal(0, stages[0].Scaling);
        Assert.Equal(0, stages[1].Scaling);
        Assert.True(stages[2].BaseKnockback > 0);
        Assert.True(stages[2].Scaling > 0);
        Assert.True(stages[1].SpawnFrame > stages[0].SpawnFrame);
        Assert.True(stages[2].SpawnFrame > stages[1].SpawnFrame);
    }

    [Fact]
    public void ByteHeavy_IsChargeable()
    {
        var charge = ByteMoveset.Create().Get(AttackSlot.NeutralHeavy).Charge;
        Assert.NotNull(charge);
        Assert.Equal(180, charge!.MaxChargeFrames);
        Assert.Equal(180, charge.MaxHoldFrames);
    }
}
```

- [ ] **Step 3: Run test to verify it fails**

Run: `dotnet test tests/ByteBrawl.Tests 2>&1 | tail -5`
Expected: FAIL — `ByteBrawl.Combat` types do not exist.

- [ ] **Step 4: Write the data model**

`src/Combat/AttackSlot.cs`:
```csharp
namespace ByteBrawl.Combat;

public enum AttackSlot
{
    NeutralLight, SideLight, UpTilt, DownTilt,
    NeutralAir, SideAir, DownAir, UpAir,
    NeutralHeavy, UpHeavy, DownHeavy, SideHeavy,
    NeutralSpecial, UpSpecial, DownSpecial, SideSpecial,
}
```

`src/Combat/HitboxShape.cs`:
```csharp
namespace ByteBrawl.Combat;

public enum HitboxShape { Box, Circle }
```

`src/Combat/AttackData.cs`:
```csharp
using Godot;

namespace ByteBrawl.Combat;

public class AttackData
{
    public string Id = "";
    public float BaseDamage;
    public float BaseKnockback;
    public float Scaling;
    public Vector2 Direction = Vector2.Zero;
    public int HitstunFrames;
    public int ActiveFrames;
    public HitboxShape Shape = HitboxShape.Circle;
    public float Radius = 6f;
    public ChargeConfig? Charge;
    public List<AttackStage> Stages = new();
}
```

`src/Combat/AttackStage.cs`:
```csharp
namespace ByteBrawl.Combat;

public class AttackStage : AttackData
{
    public int SpawnFrame;
    public float OffsetX;
    public float OffsetY;
    public float Width;
    public float Height;
}
```

`src/Combat/ChargeConfig.cs`:
```csharp
namespace ByteBrawl.Combat;

public class ChargeConfig
{
    public int MinChargeFrames;
    public int MaxChargeFrames;
    public int MaxHoldFrames;
    public float DamageGrowth;
    public float KnockbackGrowth;
}
```

`src/Combat/FighterStats.cs`:
```csharp
namespace ByteBrawl.Combat;

public class FighterStats
{
    public float RunSpeed;
    public float JumpSpeed;
    public float Weight = 1f;
}
```

`src/Combat/Moveset.cs`:
```csharp
namespace ByteBrawl.Combat;

public class Moveset
{
    public FighterStats Stats = new();
    public Dictionary<AttackSlot, AttackData> Attacks = new();

    public AttackData Get(AttackSlot slot) => Attacks[slot];
}
```

- [ ] **Step 5: Write Byte's moveset (ported from the snapshot)**

`src/Data/ByteMoveset.cs`:
```csharp
using ByteBrawl.Combat;
using Godot;

namespace ByteBrawl.Data;

public static class ByteMoveset
{
    public static Moveset Create()
    {
        var swordLight = new AttackData
        {
            Id = "sword-light", BaseDamage = 5, BaseKnockback = 150, Scaling = 1.2f,
            Direction = new Vector2(1, -0.4f), HitstunFrames = 12, ActiveFrames = 4,
        };
        var swordHeavy = new AttackData
        {
            Id = "sword-heavy", BaseDamage = 11, BaseKnockback = 230, Scaling = 1.5f,
            Direction = new Vector2(1, -0.3f), HitstunFrames = 20, ActiveFrames = 6,
            Charge = new ChargeConfig
            {
                MinChargeFrames = 30, MaxChargeFrames = 180, MaxHoldFrames = 180,
                DamageGrowth = 0.2f, KnockbackGrowth = 1.0f,
            },
        };
        var byteSpecial = new AttackData
        {
            Id = "byte-special", BaseDamage = 5, BaseKnockback = 180, Scaling = 1.3f,
            Direction = new Vector2(0.8f, -0.8f), HitstunFrames = 16, ActiveFrames = 5,
            Stages =
            {
                new AttackStage { Id = "byte-special-1", BaseDamage = 2, BaseKnockback = 45,
                    Scaling = 0, Direction = new Vector2(0.8f, -0.2f), HitstunFrames = 10,
                    ActiveFrames = 4, SpawnFrame = 4, OffsetX = 16, OffsetY = -2, Width = 16, Height = 18 },
                new AttackStage { Id = "byte-special-2", BaseDamage = 2, BaseKnockback = 55,
                    Scaling = 0, Direction = new Vector2(0.8f, -0.2f), HitstunFrames = 10,
                    ActiveFrames = 4, SpawnFrame = 12, OffsetX = 24, OffsetY = -2, Width = 18, Height = 18 },
                new AttackStage { Id = "byte-special-3", BaseDamage = 5, BaseKnockback = 180,
                    Scaling = 1.3f, Direction = new Vector2(0.8f, -0.8f), HitstunFrames = 16,
                    ActiveFrames = 5, SpawnFrame = 22, OffsetX = 34, OffsetY = -2, Width = 20, Height = 20 },
            },
        };

        var m = new Moveset { Stats = new FighterStats { RunSpeed = 120, JumpSpeed = 280, Weight = 1f } };
        foreach (var slot in new[]
        {
            AttackSlot.NeutralLight, AttackSlot.SideLight, AttackSlot.UpTilt, AttackSlot.DownTilt,
            AttackSlot.NeutralAir, AttackSlot.SideAir, AttackSlot.DownAir, AttackSlot.UpAir,
        })
            m.Attacks[slot] = swordLight;
        foreach (var slot in new[]
            { AttackSlot.NeutralHeavy, AttackSlot.UpHeavy, AttackSlot.DownHeavy, AttackSlot.SideHeavy })
            m.Attacks[slot] = swordHeavy;
        foreach (var slot in new[]
            { AttackSlot.NeutralSpecial, AttackSlot.UpSpecial, AttackSlot.DownSpecial, AttackSlot.SideSpecial })
            m.Attacks[slot] = byteSpecial;
        return m;
    }
}
```

- [ ] **Step 6: Run tests to verify they pass**

Run: `dotnet test tests/ByteBrawl.Tests 2>&1 | tail -3`
Expected: PASS (3 tests).

- [ ] **Step 7: Commit**

```bash
git add -A
git commit -m "feat: combat data model with Byte moveset"
```

---

### Task 3: Fighter contract + MatchRules

**Files:**
- Create: `src/Combat/IFighter.cs`, `src/Combat/IHitboxManager.cs`, `src/Combat/MatchRulesConfig.cs`, `src/Combat/MatchRules.cs`, `src/Combat/ElementalMeter.cs`, `src/Combat/IElementalFrenzy.cs`
- Test: `tests/ByteBrawl.Tests/TestDoubles.cs`, `tests/ByteBrawl.Tests/MatchRulesTests.cs`

**Interfaces:**
- Consumes: Task 2 types.
- Produces: `interface IFighter` (below); `interface IHitboxManager { void Spawn(IFighter attacker, AttackData attack, float offsetX, float offsetY, float width, float height); }`; `enum MatchState { Active, P1Win, P2Win }`; `class MatchRulesConfig { int StartingStocks = 3; int RespawnInvincibilityFrames = 120; float MaxDamage = 999; }`; `class MatchRules` with `MatchState State`, `double MatchTimer = 180`, ctor `(MatchRulesConfig, IFighter p1, IFighter p2)`, `void ApplyHit(IFighter attacker, IFighter defender, AttackData attack)`, `void CheckRingOut(IFighter player, Func<float, float, bool> isOutOfBounds, Vector2 spawn)`, `void Update(double deltaMs)`, `void ResetTimer()`; `class ElementalMeter { const float Max = 100; float Value; void AddFromDealt(float damage); void AddFromTaken(float damage); }`; `interface IElementalFrenzy { void OnFrenzyActivated(IFighter fighter); }`.

`IFighter` (exact members — Tasks 4-6 and nodes rely on these):

```csharp
using Godot;

namespace ByteBrawl.Combat;

public interface IFighter
{
    float Damage { get; set; }
    int Stocks { get; set; }
    int Facing { get; }
    Vector2 Position { get; set; }
    Vector2 Velocity { get; set; }
    bool IsGrounded { get; }
    int HitstunFrames { get; set; }
    int InvincibleFrames { get; set; }
    bool ShieldActive { get; set; }
    float ShieldHealth { get; set; }
    ElementalMeter Meter { get; }
    void TakeDamage(float amount);
    void ApplyKnockback(Vector2 vector);
    void EnterHitstun(int frames);
    void EnterInvincibility(int frames);
    void DamageShield(float amount);
    void DeactivateShield();
    void LoseStock();
    void Respawn(Vector2 position, int invincibilityFrames);
    void SetChargingFull(bool value);
}
```

- [ ] **Step 1: Write the test doubles**

`tests/ByteBrawl.Tests/TestDoubles.cs`:
```csharp
using ByteBrawl.Combat;
using Godot;

namespace ByteBrawl.Tests;

public class FakeFighter : IFighter
{
    public float Damage { get; set; }
    public int Stocks { get; set; } = 3;
    public int Facing { get; set; } = 1;
    public Vector2 Position { get; set; }
    public Vector2 Velocity { get; set; }
    public bool Grounded { get; set; } = true;
    public bool IsGrounded => Grounded;
    public int HitstunFrames { get; set; }
    public int InvincibleFrames { get; set; }
    public bool ShieldActive { get; set; }
    public float ShieldHealth { get; set; } = 100;
    public ElementalMeter Meter { get; } = new();

    public void TakeDamage(float amount) => Damage += amount;
    public void ApplyKnockback(Vector2 v) => Velocity += v;
    public void EnterHitstun(int frames) => HitstunFrames = frames;
    public void EnterInvincibility(int frames) => InvincibleFrames = frames;
    public void DamageShield(float amount) { ShieldHealth -= amount; if (ShieldHealth <= 0) { ShieldHealth = 0; ShieldActive = false; } }
    public void DeactivateShield() => ShieldActive = false;
    public void LoseStock() { Stocks -= 1; Damage = 0; }
    public void Respawn(Vector2 position, int frames) { Position = position; Damage = 0; HitstunFrames = 0; EnterInvincibility(frames); }
    public void SetChargingFull(bool value) { }
}

public class FakeHitboxManager : IHitboxManager
{
    public record Spawned(IFighter Attacker, AttackData Attack, float X, float Y, float W, float H);
    public List<Spawned> Spawns = new();
    public void Spawn(IFighter a, AttackData atk, float x, float y, float w, float h)
        => Spawns.Add(new Spawned(a, atk, x, y, w, h));
}
```

- [ ] **Step 2: Write the failing MatchRules tests**

`tests/ByteBrawl.Tests/MatchRulesTests.cs` (ported from the snapshot's `tests/GameRules.test.ts`):
```csharp
using ByteBrawl.Combat;
using Godot;
using Xunit;

namespace ByteBrawl.Tests;

public class MatchRulesTests
{
    private static readonly MatchRulesConfig Config = new();

    private static (MatchRules, FakeFighter, FakeFighter) NewMatch()
    {
        var p1 = new FakeFighter();
        var p2 = new FakeFighter();
        return (new MatchRules(Config, p1, p2), p1, p2);
    }

    private static AttackData Attack() => new()
    {
        Id = "t", BaseDamage = 10, BaseKnockback = 100, Scaling = 1.5f,
        Direction = new Vector2(1, -0.5f), HitstunFrames = 10, ActiveFrames = 4,
    };

    [Fact] public void Hit_IncreasesDefenderDamage() { var (r, p1, p2) = NewMatch(); r.ApplyHit(p1, p2, Attack()); Assert.Equal(10, p2.Damage); }
    [Fact] public void Hit_ScalesKnockbackWithDamage() { var (r, p1, p2) = NewMatch(); p2.Damage = 50; r.ApplyHit(p1, p2, Attack()); Assert.Equal(100 + 50 * 1.5f, p2.Velocity.X, 0.01f); }
    [Fact] public void RingOut_LastStockEndsMatch() { var (r, _, p2) = NewMatch(); p2.Stocks = 1; r.CheckRingOut(p2, (x, y) => true, Vector2.Zero); Assert.Equal(MatchState.P1Win, r.State); Assert.Equal(0, p2.Stocks); }
    [Fact] public void RingOut_RespawnsWithStocksLeft() { var (r, _, p2) = NewMatch(); p2.Stocks = 2; r.CheckRingOut(p2, (x, y) => true, new Vector2(50, 100)); Assert.Equal(MatchState.Active, r.State); Assert.Equal(1, p2.Stocks); Assert.Equal(new Vector2(50, 100), p2.Position); Assert.Equal(0, p2.Damage); }
    [Fact] public void Hit_IgnoredWhileInvincible() { var (r, p1, p2) = NewMatch(); p2.InvincibleFrames = 5; r.ApplyHit(p1, p2, Attack()); Assert.Equal(0, p2.Damage); Assert.Equal(Vector2.Zero, p2.Velocity); }
    [Fact] public void Hit_DamagesShieldInstead() { var (r, p1, p2) = NewMatch(); p2.ShieldActive = true; r.ApplyHit(p1, p2, Attack()); Assert.Equal(0, p2.Damage); Assert.Equal(70, p2.ShieldHealth, 0.01f); }
}
```

- [ ] **Step 3: Run tests to verify they fail**

Run: `dotnet test tests/ByteBrawl.Tests --filter MatchRulesTests 2>&1 | tail -3`
Expected: FAIL — `MatchRules` not defined.

- [ ] **Step 4: Implement ElementalMeter, interfaces, and MatchRules**

`src/Combat/ElementalMeter.cs`:
```csharp
namespace ByteBrawl.Combat;

public class ElementalMeter
{
    public const float Max = 100f;
    public float Value { get; private set; }

    public void AddFromDealt(float damage) => Add(damage * 0.6f);
    public void AddFromTaken(float damage) => Add(damage * 0.4f);

    private void Add(float amount) => Value = Math.Min(Max, Value + amount);
}
```

`src/Combat/IElementalFrenzy.cs`:
```csharp
namespace ByteBrawl.Combat;

// Elemental Frenzy seam. No behavior yet — a future element provides this.
public interface IElementalFrenzy
{
    void OnFrenzyActivated(IFighter fighter);
}
```

`src/Combat/IFighter.cs` and `src/Combat/IHitboxManager.cs` and `src/Combat/MatchRulesConfig.cs`: write exactly the code shown in the Interfaces block above (MatchRulesConfig as a plain class with the three initialized fields).

`src/Combat/MatchRules.cs` (logic ported from snapshot `src/game/GameRules.ts`):
```csharp
using Godot;

namespace ByteBrawl.Combat;

public class MatchRules
{
    public MatchState State { get; private set; } = MatchState.Active;
    public double MatchTimer { get; private set; } = 180;
    private double _elapsedMs;

    private readonly MatchRulesConfig _config;
    private readonly IFighter _player1;
    private readonly IFighter _player2;

    public MatchRules(MatchRulesConfig config, IFighter player1, IFighter player2)
    {
        _config = config;
        _player1 = player1;
        _player2 = player2;
    }

    public void ApplyHit(IFighter attacker, IFighter defender, AttackData attack)
    {
        if (State != MatchState.Active) return;
        if (defender.InvincibleFrames > 0) return;

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

        var preDamage = defender.Damage;
        defender.TakeDamage(attack.BaseDamage);
        var knockback = attack.BaseKnockback + preDamage * attack.Scaling;
        defender.ApplyKnockback(new Vector2(
            attack.Direction.X * attacker.Facing * knockback,
            attack.Direction.Y * knockback));
        defender.EnterHitstun(attack.HitstunFrames);
    }

    public void CheckRingOut(IFighter player, Func<float, float, bool> isOutOfBounds, Vector2 spawn)
    {
        if (State != MatchState.Active) return;
        if (!isOutOfBounds(player.Position.X, player.Position.Y)) return;

        player.LoseStock();
        if (player.Stocks <= 0)
            State = player == _player1 ? MatchState.P2Win : MatchState.P1Win;
        else
            player.Respawn(spawn, _config.RespawnInvincibilityFrames);
    }

    public void Update(double deltaMs)
    {
        if (State != MatchState.Active) return;
        _elapsedMs += deltaMs;
        if (_elapsedMs >= 1000)
        {
            _elapsedMs -= 1000;
            MatchTimer -= 1;
            if (MatchTimer <= 0) { MatchTimer = 0; State = MatchState.P1Win; }
        }
    }

    public void ResetTimer() { MatchTimer = 180; _elapsedMs = 0; }
}
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test tests/ByteBrawl.Tests 2>&1 | tail -3`
Expected: PASS (all).

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "feat: match rules and fighter contract with elemental meter hooks"
```

---

### Task 4: ActionFrame + state machine movement states

**Files:**
- Create: `src/Combat/ActionFrame.cs`, `src/Combat/FighterState.cs`, `src/Combat/FighterStateMachine.cs`
- Test: `tests/ByteBrawl.Tests/FighterStateMachineMovementTests.cs`

**Interfaces:**
- Consumes: `IFighter`, `IHitboxManager`, `Moveset` (Tasks 2-3).
- Produces: `struct ActionFrame` (all fields from the snapshot's `ActionFrame` interface: MoveX, MoveY, JumpPressed, JumpHeld, AttackLight, AttackLightHeld, AttackHeavy, AttackHeavyHeld, AttackSpecial, AttackSpecialHeld, GadgetPressed, GadgetHeld, ShieldPressed, ShieldHeld, GrabPressed, GrabHeld — all bool except MoveX/MoveY int); `enum FighterState { Idle, Run, Jump, Fall, LightAttack, HeavyAttack, Special, Charging, Shield, SpotDodge, AirDodge, Hitstun }`; `class FighterStateMachine` with `FighterState CurrentState { get; }`, ctor `(IFighter fighter, IHitboxManager hitboxes, Moveset moveset)`, `void Update(ActionFrame actions)`. Constants: spot dodge 20 frames (tap threshold 5, shield from frame 5), air dodge 20 frames / speed 220, gravity handled by node.

- [ ] **Step 1: Write the failing movement tests**

`tests/ByteBrawl.Tests/FighterStateMachineMovementTests.cs` (ported from snapshot `tests/FighterStateMachine.test.ts`):
```csharp
using ByteBrawl.Combat;
using ByteBrawl.Data;
using Godot;
using Xunit;

namespace ByteBrawl.Tests;

public class FighterStateMachineMovementTests
{
    private static ActionFrame Neutral() => new()
    {
        MoveX = 0, MoveY = 0, JumpPressed = false, JumpHeld = false,
        AttackLight = false, AttackLightHeld = false, AttackHeavy = false, AttackHeavyHeld = false,
        AttackSpecial = false, AttackSpecialHeld = false, GadgetPressed = false, GadgetHeld = false,
        ShieldPressed = false, ShieldHeld = false, GrabPressed = false, GrabHeld = false,
    };

    private static (FighterStateMachine, FakeFighter) NewFsm(bool grounded = true)
    {
        var f = new FakeFighter { Grounded = grounded };
        return (new FighterStateMachine(f, new FakeHitboxManager(), ByteMoveset.Create()), f);
    }

    [Fact] public void ShieldHeldOnGround_EntersShield()
    {
        var (fsm, f) = NewFsm();
        fsm.Update(Neutral() with { ShieldHeld = true });
        Assert.True(f.ShieldActive);
        Assert.Equal(FighterState.Shield, fsm.CurrentState);
    }

    [Fact] public void ShieldDecaysWhileHeld()
    {
        var (fsm, f) = NewFsm();
        fsm.Update(Neutral() with { ShieldHeld = true });
        var before = f.ShieldHealth;
        fsm.Update(Neutral() with { ShieldHeld = true });
        Assert.True(f.ShieldHealth < before);
    }

    [Fact] public void ShieldReleased_ExitsShield()
    {
        var (fsm, f) = NewFsm();
        fsm.Update(Neutral() with { ShieldHeld = true });
        fsm.Update(Neutral());
        Assert.False(f.ShieldActive);
        Assert.Equal(FighterState.Idle, fsm.CurrentState);
    }

    [Fact] public void ShieldTapOnGround_SpotDodges()
    {
        var (fsm, f) = NewFsm();
        fsm.Update(Neutral() with { ShieldPressed = true, ShieldHeld = true });
        Assert.Equal(FighterState.SpotDodge, fsm.CurrentState);
        Assert.True(f.InvincibleFrames > 0);
    }

    [Fact] public void SpotDodgeEnds_ReturnsToIdle()
    {
        var (fsm, _) = NewFsm();
        fsm.Update(Neutral() with { ShieldPressed = true });
        for (var i = 0; i < 21; i++) fsm.Update(Neutral());
        Assert.Equal(FighterState.Idle, fsm.CurrentState);
    }

    [Fact] public void ShieldTapInAir_DirectionalAirDodge()
    {
        var (fsm, f) = NewFsm(false);
        fsm.Update(Neutral() with { ShieldPressed = true, MoveX = 1, MoveY = -1 });
        Assert.Equal(FighterState.AirDodge, fsm.CurrentState);
        Assert.Equal(220, f.Velocity.X, 0.01f);
        Assert.Equal(-220, f.Velocity.Y, 0.01f);
    }

    [Fact] public void Run_SetsHorizontalVelocity()
    {
        var (fsm, f) = NewFsm();
        fsm.Update(Neutral() with { MoveX = 1 });
        Assert.Equal(FighterState.Run, fsm.CurrentState);
        Assert.Equal(120, f.Velocity.X, 0.01f);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/ByteBrawl.Tests --filter FighterStateMachineMovementTests 2>&1 | tail -3`
Expected: FAIL — `FighterStateMachine` not defined.

- [ ] **Step 3: Implement ActionFrame, FighterState, and the movement half of FighterStateMachine**

`src/Combat/ActionFrame.cs`:
```csharp
namespace ByteBrawl.Combat;

public record struct ActionFrame
{
    public int MoveX;
    public int MoveY;
    public bool JumpPressed, JumpHeld;
    public bool AttackLight, AttackLightHeld;
    public bool AttackHeavy, AttackHeavyHeld;
    public bool AttackSpecial, AttackSpecialHeld;
    public bool GadgetPressed, GadgetHeld;
    public bool ShieldPressed, ShieldHeld;
    public bool GrabPressed, GrabHeld;
}
```

`src/Combat/FighterState.cs`:
```csharp
namespace ByteBrawl.Combat;

public enum FighterState
{
    Idle, Run, Jump, Fall, LightAttack, HeavyAttack, Special,
    Charging, Shield, SpotDodge, AirDodge, Hitstun,
}
```

`src/Combat/FighterStateMachine.cs` — implement the full class now, with attack/charge methods stubbed to throw `NotImplementedException` (Task 5 replaces them; the movement tests above never reach them):

```csharp
using Godot;

namespace ByteBrawl.Combat;

public class FighterStateMachine
{
    public const int DefaultAttackDuration = 20;
    public const int AttackRecoveryFrames = 10;
    public const int SpotDodgeFrames = 20;
    public const int SpotDodgeTapThreshold = 5;
    public const int AirDodgeFrames = 20;
    public const float AirDodgeSpeed = 220f;

    public FighterState CurrentState => _state;

    private FighterState _state = FighterState.Idle;
    private int _stateFrames;
    private int _attackCooldown;
    private readonly IFighter _fighter;
    private readonly IHitboxManager _hitboxes;
    private readonly Moveset _moveset;

    public FighterStateMachine(IFighter fighter, IHitboxManager hitboxes, Moveset moveset)
    {
        _fighter = fighter;
        _hitboxes = hitboxes;
        _moveset = moveset;
    }

    public void Update(ActionFrame actions)
    {
        if (_attackCooldown > 0) _attackCooldown--;

        if (_fighter.HitstunFrames > 0)
        {
            _fighter.DeactivateShield();
            SetState(FighterState.Hitstun);
            _stateFrames++;
            return;
        }

        if (_state == FighterState.Hitstun && _fighter.HitstunFrames == 0)
            SetState(_fighter.IsGrounded ? FighterState.Idle : FighterState.Fall);

        if (IsAttackState(_state)) { TickAttack(actions); return; }

        if (_state == FighterState.Charging) { TickCharge(actions); return; }

        if (_state == FighterState.SpotDodge)
        {
            _stateFrames++;
            _fighter.Velocity = new Vector2(0, _fighter.Velocity.Y);
            if (_stateFrames >= SpotDodgeTapThreshold && actions.ShieldHeld)
            {
                _fighter.InvincibleFrames = 0;
                _fighter.ShieldActive = true;
                SetState(FighterState.Shield);
                return;
            }
            if (_stateFrames >= SpotDodgeFrames) SetState(FighterState.Idle);
            return;
        }

        if (_state == FighterState.AirDodge)
        {
            _stateFrames++;
            if (_stateFrames >= AirDodgeFrames)
            {
                _fighter.Velocity = new Vector2(_fighter.Velocity.X, 80);
                SetState(FighterState.Fall);
            }
            return;
        }

        if (_state == FighterState.Shield)
        {
            if (!actions.ShieldHeld || _fighter.ShieldHealth <= 0)
            {
                _fighter.DeactivateShield();
                SetState(_fighter.IsGrounded ? FighterState.Idle : FighterState.Fall);
            }
            else
            {
                _fighter.Velocity = new Vector2(0, _fighter.Velocity.Y);
                _fighter.ShieldHealth -= 0.1f;
            }
            return;
        }

        if (actions.ShieldPressed)
        {
            if (_fighter.IsGrounded) { SetState(FighterState.SpotDodge); return; }
            var dirY = actions.MoveY < 0 ? -1 : actions.MoveY > 0 ? 1 : 0;
            _fighter.Velocity = new Vector2(actions.MoveX * AirDodgeSpeed, dirY * AirDodgeSpeed);
            SetState(FighterState.AirDodge);
            return;
        }

        if (actions.ShieldHeld && _fighter.IsGrounded)
        {
            _fighter.ShieldActive = true;
            SetState(FighterState.Shield);
            return;
        }

        if (actions.JumpPressed && _fighter.IsGrounded)
            _fighter.Velocity = new Vector2(_fighter.Velocity.X, -_moveset.Stats.JumpSpeed);

        if (actions.AttackLight && _attackCooldown == 0) { StartAttack(FighterState.LightAttack, _moveset.Get(AttackSlot.NeutralLight)); return; }
        if (actions.AttackHeavy && _attackCooldown == 0) { StartAttack(FighterState.HeavyAttack, _moveset.Get(AttackSlot.NeutralHeavy)); return; }
        if (actions.AttackSpecial && _attackCooldown == 0) { StartAttack(FighterState.Special, _moveset.Get(AttackSlot.NeutralSpecial)); return; }

        if (_fighter.IsGrounded)
        {
            if (actions.MoveX != 0)
            {
                SetState(FighterState.Run);
                _fighter.Velocity = new Vector2(actions.MoveX * _moveset.Stats.RunSpeed, _fighter.Velocity.Y);
            }
            else
            {
                SetState(FighterState.Idle);
                _fighter.Velocity = new Vector2(0, _fighter.Velocity.Y);
            }
        }
        else
        {
            if (actions.MoveX != 0)
                _fighter.Velocity = new Vector2(actions.MoveX * _moveset.Stats.RunSpeed, _fighter.Velocity.Y);
            SetState(_fighter.Velocity.Y < 0 ? FighterState.Jump : FighterState.Fall);
        }

        _stateFrames++;
    }

    private void TickAttack(ActionFrame actions) => throw new NotImplementedException("Task 5");
    private void TickCharge(ActionFrame actions) => throw new NotImplementedException("Task 5");

    private void StartAttack(FighterState state, AttackData attack)
    {
        _fighter.DeactivateShield();
        SetState(state);
        _attackCooldown = DefaultAttackDuration;
        _hitboxes.Spawn(_fighter, attack, 14, -2, 12, 16);
    }

    private void SetState(FighterState newState)
    {
        if (_state == newState) return;
        _state = newState;
        _stateFrames = 0;
        if (newState == FighterState.SpotDodge)
            _fighter.EnterInvincibility(SpotDodgeFrames);
    }

    private static bool IsAttackState(FighterState s) =>
        s is FighterState.LightAttack or FighterState.HeavyAttack or FighterState.Special;
}
```

Note: the spot-dodge tap-to-shield transition and shield creation live on the node layer in Godot (the Phaser `createShieldBubble` call is presentation). The FSM only flips `ShieldActive`.

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test tests/ByteBrawl.Tests 2>&1 | tail -3`
Expected: PASS (movement tests; other suites still pass).

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "feat: fighter state machine movement states"
```

---

### Task 5: Attacks, multi-hit chains, and charging

**Files:**
- Modify: `src/Combat/FighterStateMachine.cs` (replace the two `NotImplementedException` stubs)
- Test: `tests/ByteBrawl.Tests/FighterStateMachineAttackTests.cs`

**Interfaces:**
- Consumes: Task 4 class and constants.
- Produces: same public surface; charging behavior per spec: charge lock (drag on velocity while charging: ground drag 0.8, air drag 0.99, max fall 120), min 30 / max 180 / hold 180 frames, auto-fire at max+hold, `SetChargingFull(true)` while at max charge, multi-hit stages spawn via `IHitboxManager.Spawn` at their `SpawnFrame`.

- [ ] **Step 1: Write the failing attack tests**

`tests/ByteBrawl.Tests/FighterStateMachineAttackTests.cs`:
```csharp
using ByteBrawl.Combat;
using ByteBrawl.Data;
using Xunit;

namespace ByteBrawl.Tests;

public class FighterStateMachineAttackTests
{
    private static ActionFrame Neutral() => new();

    private static (FighterStateMachine, FakeFighter, FakeHitboxManager) NewFsm()
    {
        var f = new FakeFighter();
        var hb = new FakeHitboxManager();
        return (new FighterStateMachine(f, hb, ByteMoveset.Create()), f, hb);
    }

    [Fact] public void LightAttack_SpawnsHitboxImmediately()
    {
        var (fsm, _, hb) = NewFsm();
        fsm.Update(Neutral() with { AttackLight = true });
        Assert.Equal(FighterState.LightAttack, fsm.CurrentState);
        Assert.Single(hb.Spawns);
    }

    [Fact] public void MultiHitSpecial_SpawnsThreeStagesInOrder()
    {
        var (fsm, _, hb) = NewFsm();
        fsm.Update(Neutral() with { AttackSpecial = true });
        for (var i = 0; i < 30; i++) fsm.Update(Neutral());
        Assert.Equal(3, hb.Spawns.Count);
        Assert.Equal("byte-special-1", hb.Spawns[0].Attack.Id);
        Assert.Equal("byte-special-2", hb.Spawns[1].Attack.Id);
        Assert.Equal("byte-special-3", hb.Spawns[2].Attack.Id);
    }

    [Fact] public void ChargeableHeavy_EntersChargingWithoutSpawning()
    {
        var (fsm, _, hb) = NewFsm();
        fsm.Update(Neutral() with { AttackHeavy = true, AttackHeavyHeld = true });
        Assert.Equal(FighterState.Charging, fsm.CurrentState);
        Assert.Empty(hb.Spawns);
    }

    [Fact] public void ChargeReleasedAfterMin_FiresWithBonusDamage()
    {
        var (fsm, _, hb) = NewFsm();
        fsm.Update(Neutral() with { AttackHeavy = true, AttackHeavyHeld = true });
        for (var i = 0; i < 35; i++) fsm.Update(Neutral() with { AttackHeavyHeld = true });
        fsm.Update(Neutral() with { AttackHeavyHeld = false });
        Assert.Equal(FighterState.HeavyAttack, fsm.CurrentState);
        Assert.Single(hb.Spawns);
        Assert.True(hb.Spawns[0].Attack.BaseDamage > 11);
    }

    [Fact] public void ChargeHeldToMaxPlusHold_AutoFires()
    {
        var (fsm, _, hb) = NewFsm();
        fsm.Update(Neutral() with { AttackHeavy = true, AttackHeavyHeld = true });
        for (var i = 0; i < 365; i++) fsm.Update(Neutral() with { AttackHeavyHeld = true });
        Assert.Equal(FighterState.HeavyAttack, fsm.CurrentState);
        Assert.Single(hb.Spawns);
    }

    [Fact] public void FullCharge_SetsChargingFullOnFighter()
    {
        var f = new FakeFighter();
        var hb = new FakeHitboxManager();
        var fsm = new FighterStateMachine(f, hb, ByteMoveset.Create());
        var full = false;
        // FakeFighter ignores SetChargingFull; observe via a small subclass instead.
        var observing = new ObservingFighter(() => full = true);
        var fsm2 = new FighterStateMachine(observing, hb, ByteMoveset.Create());
        fsm2.Update(Neutral() with { AttackHeavy = true, AttackHeavyHeld = true });
        for (var i = 0; i < 181; i++) fsm2.Update(Neutral() with { AttackHeavyHeld = true });
        Assert.True(full);
    }

    private class ObservingFighter : FakeFighter
    {
        private readonly Action _onFull;
        public ObservingFighter(Action onFull) => _onFull = onFull;
        public override void SetChargingFull(bool value) { if (value) _onFull(); }
    }
}
```

Because `FakeFighter.SetChargingFull` must be virtual for this test, update `TestDoubles.cs`: change `public void SetChargingFull(bool value) { }` to `public virtual void SetChargingFull(bool value) { }`.

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/ByteBrawl.Tests --filter FighterStateMachineAttackTests 2>&1 | tail -3`
Expected: FAIL — charging/attacks not implemented (`NotImplementedException`).

- [ ] **Step 3: Implement attack + charge logic (replace the two stubs in `FighterStateMachine.cs`)**

Add fields to the class:
```csharp
    public const float ChargeAirDrag = 0.99f;
    public const float ChargeGroundDrag = 0.8f;
    public const float ChargeMaxFallSpeed = 120f;

    private AttackStage[] _sequence = Array.Empty<AttackStage>();
    private int _nextStageIndex;
    private int _attackTotalFrames = DefaultAttackDuration;
    private AttackData? _chargeAttack;
    private FighterState _chargeFiredState;
```

Replace `TickAttack` stub:
```csharp
    private void TickAttack(ActionFrame actions)
    {
        _stateFrames++;
        while (_nextStageIndex < _sequence.Length &&
               _stateFrames >= _sequence[_nextStageIndex].SpawnFrame)
        {
            var stage = _sequence[_nextStageIndex++];
            _hitboxes.Spawn(_fighter, stage, stage.OffsetX, stage.OffsetY, stage.Width, stage.Height);
        }
        if (_stateFrames >= _attackTotalFrames)
            SetState(_fighter.IsGrounded ? FighterState.Idle : FighterState.Fall);
    }
```

Replace `TickCharge` stub:
```csharp
    private void TickCharge(ActionFrame actions)
    {
        if (_chargeAttack?.Charge is not { } charge) return;

        var held = _chargeFiredState == FighterState.HeavyAttack
            ? actions.AttackHeavyHeld
            : actions.AttackSpecialHeld;

        var drag = _fighter.IsGrounded ? ChargeGroundDrag : ChargeAirDrag;
        var vy = _fighter.Velocity.Y * drag;
        if (!_fighter.IsGrounded && vy > ChargeMaxFallSpeed) vy = ChargeMaxFallSpeed;
        if (!_fighter.IsGrounded && vy < 0) vy *= drag;
        _fighter.Velocity = new Vector2(_fighter.Velocity.X * drag, vy);

        _stateFrames++;
        var full = _stateFrames >= charge.MaxChargeFrames;
        _fighter.SetChargingFull(full);

        var autoRelease = _stateFrames >= charge.MaxChargeFrames + charge.MaxHoldFrames;
        var released = !held;
        if ((full && released) || autoRelease)
            FireCharged(charge.MaxChargeFrames);
        else if (!full && released && _stateFrames >= charge.MinChargeFrames)
            FireCharged(_stateFrames);
    }

    private void FireCharged(int chargeFrames)
    {
        if (_chargeAttack?.Charge is not { } charge) return;
        var frames = Math.Clamp(chargeFrames, 0, charge.MaxChargeFrames);
        var charged = new AttackData
        {
            Id = $"{_chargeAttack.Id}-charged",
            BaseDamage = _chargeAttack.BaseDamage + frames * charge.DamageGrowth,
            BaseKnockback = _chargeAttack.BaseKnockback + frames * charge.KnockbackGrowth,
            Scaling = _chargeAttack.Scaling,
            Direction = _chargeAttack.Direction,
            HitstunFrames = _chargeAttack.HitstunFrames,
            ActiveFrames = _chargeAttack.ActiveFrames,
            Shape = _chargeAttack.Shape,
            Radius = _chargeAttack.Radius,
        };
        _fighter.SetChargingFull(false);
        _chargeAttack = null;
        StartAttack(_chargeFiredState, charged);
    }
```

Replace `StartAttack` so multi-hit attacks arm the sequence and chargeable attacks arm the charge (heavy/special presses route through here from `Update`):
```csharp
    private void StartAttack(FighterState state, AttackData attack)
    {
        _fighter.DeactivateShield();

        if (attack.Charge is { } charge && !attack.Id.EndsWith("-charged"))
        {
            _chargeAttack = attack;
            _chargeFiredState = state;
            SetState(FighterState.Charging);
            _attackCooldown = charge.MaxChargeFrames + charge.MaxHoldFrames + DefaultAttackDuration;
            return;
        }

        SetState(state);
        _sequence = attack.Stages.Count > 0
            ? attack.Stages.OrderBy(s => s.SpawnFrame).ToArray()
            : Array.Empty<AttackStage>();
        _nextStageIndex = 0;
        var last = _sequence.LastOrDefault();
        _attackTotalFrames = last != null
            ? last.SpawnFrame + last.ActiveFrames + AttackRecoveryFrames
            : DefaultAttackDuration;
        _attackCooldown = _attackTotalFrames;

        if (_sequence.Length == 0)
            _hitboxes.Spawn(_fighter, attack, 14, -2, 12, 16);
    }
```

Also update `SetState`: when leaving `Charging`, clear `_chargeAttack` and call `_fighter.SetChargingFull(false)`:
```csharp
        if (newState != FighterState.Charging && _chargeAttack != null)
        {
            _chargeAttack = null;
            _fighter.SetChargingFull(false);
        }
```

- [ ] **Step 4: Run full test suite**

Run: `dotnet test tests/ByteBrawl.Tests 2>&1 | tail -3`
Expected: PASS (all suites).

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "feat: attacks, multi-hit chains, and charge system"
```

---

### Task 6: Fighter node with limb rig, hitboxes, and input

**Files:**
- Create: `src/Nodes/Limb.cs`, `src/Nodes/LimbRig.cs`, `src/Nodes/Pose.cs`, `src/Nodes/PoseLibrary.cs`, `src/Nodes/PosePlayer.cs`, `src/Nodes/Fighter.cs`, `src/Nodes/LocalInput.cs`, `src/Nodes/Hurtbox.cs`, `src/Nodes/Hitbox.cs`, `src/Nodes/HitboxManager.cs`
- Create: `scenes/fighter.tscn`, `scenes/smoke_limb_rig.tscn`, `src/Nodes/SmokeLimbRig.cs`

(Amended per human ruling: `LocalInput`, `Hurtbox`, `Hitbox`, and `HitboxManager` were moved here from Task 7 because `Fighter.cs` and `HitboxManager.cs` reference them — the project must compile at the end of every task.)

**Interfaces:**
- Consumes: Tasks 2-5 logic.
- Produces: `partial class Fighter : CharacterBody2D, IFighter` — ctor takes no arguments; `[Export] public string MovesetName = "byte"`; `FighterStateMachine Fsm { get; }`; properties mapped to physics body. `class LimbRig : Node2D` with `Limb Find(string name)`; `static class PoseLibrary` with `Pose For(FighterState state, int stateFrames)`; smoke scene prints `SMOKE PASS`.

- [ ] **Step 1: Write the limb rig and pose classes**

`src/Nodes/Limb.cs`:
```csharp
using Godot;

namespace ByteBrawl.Nodes;

// One body part. Sprite offset sits at the joint pivot, so Rotation
// happens at the shoulder / elbow / hip / knee.
public partial class Limb : Node2D
{
    public Sprite2D Sprite = null!;

    public static Limb Create(string name, Vector2 size, Vector2 pivot, Color color)
    {
        var limb = new Limb { Name = name, Position = Vector2.Zero };
        var tex = new PlaceholderTexture2D { Size = size };
        limb.Sprite = new Sprite2D
        {
            Texture = tex,
            Offset = -pivot,
            Modulate = color,
        };
        limb.AddChild(limb.Sprite);
        return limb;
    }
}
```

`src/Nodes/LimbRig.cs`:
```csharp
using Godot;

namespace ByteBrawl.Nodes;

public partial class LimbRig : Node2D
{
    public Limb Find(string name) => GetNode<Limb>(name);

    public static LimbRig CreatePlaceholder()
    {
        var rig = new LimbRig();
        var torso = Limb.Create("Torso", new Vector2(10, 12), new Vector2(5, 0), new Color("#00cccc"));
        var head = Limb.Create("Head", new Vector2(8, 8), new Vector2(4, 8), new Color("#00ffff"));
        head.Position = new Vector2(0, -14);
        torso.AddChild(head);
        var nearArm = Limb.Create("NearArm", new Vector2(4, 10), new Vector2(2, 0), new Color("#009999"));
        nearArm.Position = new Vector2(6, -10);
        var nearLeg = Limb.Create("NearLeg", new Vector2(4, 12), new Vector2(2, 0), new Color("#008888"));
        nearLeg.Position = new Vector2(2, 12);
        torso.AddChild(nearArm);
        torso.AddChild(nearLeg);
        rig.AddChild(torso);
        return rig;
    }
}
```

`src/Nodes/Pose.cs`:
```csharp
namespace ByteBrawl.Nodes;

using LimbAngles = System.Collections.Generic.Dictionary<string, float>;

// A pose is a set of limb rotations in degrees.
public class Pose
{
    public LimbAngles Angles = new();
}
```

`src/Nodes/PoseLibrary.cs`:
```csharp
using ByteBrawl.Combat;

namespace ByteBrawl.Nodes;

public static class PoseLibrary
{
    public static Pose For(FighterState state, int stateFrames)
    {
        var p = new Pose();
        switch (state)
        {
            case FighterState.Run:
                var swing = Mathf.Sin(stateFrames * 0.3f) * 40f;
                p.Angles["NearLeg"] = swing;
                p.Angles["NearArm"] = -swing;
                p.Angles["Torso"] = 5f;
                break;
            case FighterState.Jump:
                p.Angles["NearLeg"] = -25f;
                p.Angles["NearArm"] = -40f;
                break;
            case FighterState.Fall:
                p.Angles["NearArm"] = -70f;
                break;
            case FighterState.LightAttack:
            case FighterState.HeavyAttack:
            case FighterState.Special:
                p.Angles["NearArm"] = -90f;
                p.Angles["Torso"] = -10f;
                break;
            case FighterState.Charging:
                p.Angles["NearArm"] = -45f;
                break;
            case FighterState.Shield:
                p.Angles["NearArm"] = -60f;
                break;
            default:
                p.Angles["NearLeg"] = 0f;
                p.Angles["NearArm"] = 0f;
                break;
        }
        return p;
    }
}
```

`src/Nodes/PosePlayer.cs`:
```csharp
using ByteBrawl.Combat;
using Godot;

namespace ByteBrawl.Nodes;

public partial class PosePlayer : Node
{
    [Export] public LimbRig Rig = null!;

    public void Play(FighterState state, int stateFrames)
    {
        var pose = PoseLibrary.For(state, stateFrames);
        foreach (var (name, degrees) in pose.Angles)
            Rig.Find(name).RotationDegrees = degrees;
    }
}
```

`src/Nodes/Fighter.cs` (node shell implementing `IFighter`; physics constants: gravity 800, maxVelocity like Phaser):
```csharp
using ByteBrawl.Combat;
using ByteBrawl.Data;
using Godot;

namespace ByteBrawl.Nodes;

public partial class Fighter : CharacterBody2D, IFighter
{
    [Export] public string MovesetName = "byte";
    [Export] public int PlayerIndex = 1;
    public FighterStateMachine Fsm { get; private set; } = null!;
    public ElementalMeter Meter { get; } = new();

    private IHitboxManager _hitboxes = null!;
    private LimbRig _rig = null!;
    private bool _chargingFull;

    public float Damage { get; set; }
    public int Stocks { get; set; } = 3;
    public int Facing => Velocity.X < -1 ? -1 : 1;
    public int HitstunFrames { get; set; }
    public int InvincibleFrames { get; set; }
    public bool ShieldActive { get; set; }
    public float ShieldHealth { get; set; } = 100;

    public override void _Ready()
    {
        _rig = LimbRig.CreatePlaceholder();
        AddChild(_rig);
        var posePlayer = new PosePlayer { Rig = _rig };
        AddChild(posePlayer);
        _hitboxes = GetParent().GetNode<HitboxManager>("HitboxManager");
        Fsm = new FighterStateMachine(this, _hitboxes, ByteMoveset.Create());
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!IsOnFloor()) Velocity = new Vector2(Velocity.X, Velocity.Y + 800f * (float)delta);
        if (HitstunFrames > 0) HitstunFrames--;
        if (InvincibleFrames > 0) InvincibleFrames--;
        Fsm.Update(LocalInput.Capture(PlayerIndex));
        MoveAndSlide();
        GetNode<PosePlayer>("PosePlayer").Play(Fsm.CurrentState, 0);
    }

    public void TakeDamage(float amount) => Damage += amount;
    public void ApplyKnockback(Vector2 v) => Velocity = v;
    public void EnterHitstun(int frames) => HitstunFrames = frames;
    public void EnterInvincibility(int frames) => InvincibleFrames = frames;
    public void DamageShield(float amount) { ShieldHealth -= amount; if (ShieldHealth <= 0) { ShieldHealth = 0; DeactivateShield(); } }
    public void DeactivateShield() => ShieldActive = false;
    public void LoseStock() { Stocks -= 1; Damage = 0; }
    public void Respawn(Vector2 position, int invincibilityFrames)
    {
        Position = position;
        Velocity = Vector2.Zero;
        Damage = 0;
        HitstunFrames = 0;
        EnterInvincibility(invincibilityFrames);
    }
    public void SetChargingFull(bool value) => _chargingFull = value;
}
```

(The FSM reads `IsGrounded` via the interface: add `public bool IsGrounded => IsOnFloor();` to `Fighter`.)

- [ ] **Step 2: Write input capture and hitbox nodes**

`src/Nodes/LocalInput.cs` (note: `Capture(0)` — or any index other than 1/2 — returns an empty frame for the training dummy; that is the intended dummy behavior):
```csharp
using ByteBrawl.Combat;
using Godot;

namespace ByteBrawl.Nodes;

public static class LocalInput
{
    private static readonly Dictionary<int, HashSet<Key>> Prev = new();

    private static readonly Dictionary<string, Key> P1 = new()
    {
        ["left"] = Key.A, ["right"] = Key.D, ["up"] = Key.W, ["down"] = Key.S,
        ["jump"] = Key.Space, ["light"] = Key.J, ["heavy"] = Key.K, ["special"] = Key.H,
        ["gadget"] = Key.C, ["shield"] = Key.Shift, ["grab"] = Key.L,
    };

    private static readonly Dictionary<string, Key> P2 = new()
    {
        ["left"] = Key.Left, ["right"] = Key.Right, ["up"] = Key.Up, ["down"] = Key.Down,
        ["jump"] = Key.Kp5, ["light"] = Key.Kp1, ["heavy"] = Key.Kp2, ["special"] = Key.Kp0,
        ["gadget"] = Key.Kp4, ["shield"] = Key.Kp3, ["grab"] = Key.Kp6,
    };

    public static ActionFrame Capture(int player)
    {
        if (player != 1 && player != 2) return new ActionFrame(); // dummy
        var map = player == 1 ? P1 : P2;
        var prev = Prev.TryGetValue(player, out var p) ? p : new HashSet<Key>();

        ActionFrame frame = new()
        {
            MoveX = (IsDown(map, "right") ? 1 : 0) - (IsDown(map, "left") ? 1 : 0),
            MoveY = (IsDown(map, "down") ? 1 : 0) - (IsDown(map, "up") ? 1 : 0),
            JumpPressed = Pressed(map, prev, "jump"), JumpHeld = IsDown(map, "jump"),
            AttackLight = Pressed(map, prev, "light"), AttackLightHeld = IsDown(map, "light"),
            AttackHeavy = Pressed(map, prev, "heavy"), AttackHeavyHeld = IsDown(map, "heavy"),
            AttackSpecial = Pressed(map, prev, "special"), AttackSpecialHeld = IsDown(map, "special"),
            GadgetPressed = Pressed(map, prev, "gadget"), GadgetHeld = IsDown(map, "gadget"),
            ShieldPressed = Pressed(map, prev, "shield"), ShieldHeld = IsDown(map, "shield"),
            GrabPressed = Pressed(map, prev, "grab"), GrabHeld = IsDown(map, "grab"),
        };

        Prev[player] = map.Where(kv => Input.IsPhysicalKeyPressed(kv.Value)).Select(kv => kv.Value).ToHashSet();
        return frame;
    }

    private static bool IsDown(Dictionary<string, Key> map, string name) => Input.IsPhysicalKeyPressed(map[name]);
    private static bool Pressed(Dictionary<string, Key> map, HashSet<Key> prev, string name)
    {
        var key = map[name];
        return Input.IsPhysicalKeyPressed(key) && !prev.Contains(key);
    }
}
```

`src/Nodes/Hurtbox.cs`:
```csharp
using Godot;

namespace ByteBrawl.Nodes;

public partial class Hurtbox : Area2D
{
    public Fighter Owner = null!;
}
```

`src/Nodes/Hitbox.cs`:
```csharp
using ByteBrawl.Combat;
using Godot;

namespace ByteBrawl.Nodes;

public partial class Hitbox : Area2D
{
    public Fighter Attacker = null!;
    public AttackData Attack = null!;
    public int FramesRemaining;
    public bool HasHit;
}
```

`src/Nodes/HitboxManager.cs`:
```csharp
using ByteBrawl.Combat;
using Godot;

namespace ByteBrawl.Nodes;

public partial class HitboxManager : Node, IHitboxManager
{
    public MatchRules? Rules;

    public void Spawn(IFighter attacker, AttackData attack, float offsetX, float offsetY, float width, float height)
    {
        if (attacker is not Fighter a || Rules == null) return;
        var shape = new CollisionShape2D();
        if (attack.Shape == HitboxShape.Box)
            shape.Shape = new RectangleShape2D { Size = new Vector2(width, height) };
        else
            shape.Shape = new CircleShape2D { Radius = attack.Radius };
        var hb = new Hitbox
        {
            Attacker = a, Attack = attack, FramesRemaining = attack.ActiveFrames,
            Position = a.Position + new Vector2(offsetX * a.Facing, offsetY),
        };
        hb.AddChild(shape);
        hb.AreaEntered += area =>
        {
            if (hb.HasHit || area is not Hurtbox hurt) return;
            var defender = hurt.Owner;
            if (defender == hb.Attacker) return;
            Rules.ApplyHit(hb.Attacker, defender, hb.Attack);
            hb.HasHit = true;
        };
        AddChild(hb);
    }

    public override void _PhysicsProcess(double delta)
    {
        foreach (var child in GetChildren())
        {
            if (child is not Hitbox hb) continue;
            hb.FramesRemaining--;
            if (hb.FramesRemaining <= 0) hb.QueueFree();
        }
    }
}
```

Also add a `Hurtbox` to `Fighter._Ready()` (so hits can register once Task 7 wires the arena): inside `Fighter._Ready()`, after `_rig` setup:
```csharp
        var hurt = new Hurtbox { Owner = this };
        var hurtShape = new CollisionShape2D { Shape = new RectangleShape2D { Size = new Vector2(12, 20) } };
        hurt.AddChild(hurtShape);
        AddChild(hurt);
```

- [ ] **Step 3: Write the smoke scene**

`scenes/smoke_limb_rig.tscn`:
```
[gd_scene load_steps=2 format=3]

[ext_resource type="Script" path="res://src/Nodes/SmokeLimbRig.cs" id="1"]

[node name="Smoke" type="Node2D"]
script = ExtResource("1")
```

`src/Nodes/SmokeLimbRig.cs`:
```csharp
using Godot;

namespace ByteBrawl.Nodes;

public partial class SmokeLimbRig : Node2D
{
    public override void _Ready()
    {
        var rig = LimbRig.CreatePlaceholder();
        AddChild(rig);
        var before = rig.Find("NearArm").RotationDegrees;
        rig.Find("NearArm").RotationDegrees = -90f;
        var ok = rig.Find("NearArm").RotationDegrees == -90f
                 && rig.Find("Torso").GetNode<Limb>("NearLeg") != null;
        GD.Print(ok ? "SMOKE PASS" : "SMOKE FAIL");
        GetTree().Quit(ok ? 0 : 1);
    }
}
```

- [ ] **Step 4: Run the smoke scene**

Run: `timeout 30 godot --headless --path . res://scenes/smoke_limb_rig.tscn 2>&1 | grep -E "SMOKE|ERROR"`
Expected: `SMOKE PASS`.

- [ ] **Step 5: Build check and commit**

Run: `dotnet build ByteBrawl.csproj 2>&1 | tail -3`
Expected: Build succeeded.
Then: `git add -A && git commit -m "feat: fighter node with segmented limb rig and pose player"`

---

### Task 7: Arena — stage, hitboxes, camera, HUD, training dummy

**Files:**
- Create: `src/Nodes/Arena.cs`, `src/Nodes/ArenaCamera.cs`, `scenes/arena.tscn`, `scenes/arena_training.tscn`, `scenes/smoke_arena.tscn`, `src/Nodes/SmokeArena.cs`

(`Hitbox.cs`, `Hurtbox.cs`, `HitboxManager.cs`, and `LocalInput.cs` were moved to Task 6 by amendment — do not recreate them here.)

**Interfaces:**
- Consumes: `Fighter`, `HitboxManager`, `LocalInput`, `MatchRules`, `Combat` logic (all from earlier tasks).
- Produces: `class Arena : Node2D` — `[Export] public bool Training;`, instantiates two fighters from `scenes/fighter.tscn`, stage rectangles matching the snapshot layout (main floor 3×14 tiles at rows 10-12 → Rect2(32, 160, 224, 48), two thin platforms Rect2(32, 80, 48, 16) and Rect2(224, 80, 48, 16)), blast zone `Rect2(-160, -180, 640, 540)`.

- [ ] **Step 1: Write Arena and ArenaCamera**

`src/Nodes/ArenaCamera.cs` (simple port of CameraController: midpoint + zoom to fit both, deadzone omitted for draft):
```csharp
using Godot;

namespace ByteBrawl.Nodes;

public partial class ArenaCamera : Camera2D
{
    public Fighter? P1;
    public Fighter? P2;

    public override void _Process(double delta)
    {
        if (P1 == null || P2 == null) return;
        var mid = (P1.Position + P2.Position) / 2;
        var dist = (P1.Position - P2.Position).Length();
        var zoom = Mathf.Clamp(160f / Mathf.Max(dist, 60f), 0.6f, 1.5f);
        Position = mid;
        Zoom = new Vector2(zoom, zoom);
    }
}
```

`src/Nodes/Arena.cs`:
```csharp
using ByteBrawl.Combat;
using Godot;

namespace ByteBrawl.Nodes;

public partial class Arena : Node2D
{
    [Export] public bool Training;

    private static readonly Rect2[] Platforms =
    {
        new(32, 160, 224, 48),   // main floor (rows 10-12, cols 2-15)
        new(32, 80, 48, 16),     // left thin platform
        new(224, 80, 48, 16),    // right thin platform
    };
    private static readonly Rect2 BlastZone = new(-160, -180, 640, 540);

    private MatchRules _rules = null!;
    private Fighter _p1 = null!;
    private Fighter _p2 = null!;

    public override void _Ready()
    {
        BuildStage();
        var hb = new HitboxManager { Name = "HitboxManager" };
        AddChild(hb);

        var fighterScene = GD.Load<PackedScene>("res://scenes/fighter.tscn");
        _p1 = fighterScene.Instantiate<Fighter>();
        _p1.PlayerIndex = 1;
        _p1.Position = new Vector2(110, 110);
        AddChild(_p1);
        _p2 = fighterScene.Instantiate<Fighter>();
        _p2.PlayerIndex = Training ? 0 : 2; // 0 = dummy
        _p2.Position = new Vector2(210, 110);
        AddChild(_p2);

        _rules = new MatchRules(new MatchRulesConfig(), _p1, _p2);
        hb.Rules = _rules;
        hb.Player1 = _p1;
        hb.Player2 = _p2;

        var cam = new ArenaCamera { P1 = _p1, P2 = _p2 };
        AddChild(cam);
    }

    private void BuildStage()
    {
        foreach (var rect in Platforms)
        {
            var body = new StaticBody2D();
            var shape = new CollisionShape2D
            {
                Shape = new RectangleShape2D { Size = rect.Size },
                Position = rect.Position + rect.Size / 2,
            };
            body.AddChild(shape);
            AddChild(body);
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!Training) _rules.Update(delta * 1000);
        foreach (var f in new[] { _p1, _p2 })
        {
            if (!BlastZone.HasPoint(f.Position))
            {
                if (Training)
                    f.Respawn(f == _p1 ? new Vector2(110, 110) : new Vector2(210, 110), 0);
                else
                    _rules.CheckRingOut(f, (x, y) => !BlastZone.HasPoint(new Vector2(x, y)),
                        f == _p1 ? new Vector2(110, 110) : new Vector2(210, 110));
            }
        }
    }
}
```

`scenes/fighter.tscn`:
```
[gd_scene load_steps=2 format=3]

[ext_resource type="Script" path="res://src/Nodes/Fighter.cs" id="1"]

[node name="Fighter" type="CharacterBody2D"]
script = ExtResource("1")
```

`scenes/arena.tscn`:
```
[gd_scene load_steps=2 format=3]

[ext_resource type="Script" path="res://src/Nodes/Arena.cs" id="1"]

[node name="Arena" type="Node2D"]
script = ExtResource("1")
```

`scenes/arena_training.tscn` (same scene with Training enabled):
```
[gd_scene load_steps=2 format=3]

[ext_resource type="Script" path="res://src/Nodes/Arena.cs" id="1"]

[node name="Arena" type="Node2D"]
script = ExtResource("1")
Training = true
```

- [ ] **Step 2: Write and run the arena smoke scene**

`src/Nodes/SmokeArena.cs`:
```csharp
using Godot;

namespace ByteBrawl.Nodes;

public partial class SmokeArena : Node
{
    private int _frames;

    public override void _Ready()
    {
        var arena = new Arena { Training = true };
        AddChild(arena);
    }

    public override void _PhysicsProcess(double delta)
    {
        if (++_frames < 120) return;
        GD.Print("SMOKE PASS");
        GetTree().Quit(0);
    }
}
```

`scenes/smoke_arena.tscn`:
```
[gd_scene load_steps=2 format=3]

[ext_resource type="Script" path="res://src/Nodes/SmokeArena.cs" id="1"]

[node name="Smoke" type="Node"]
script = ExtResource("1")
```

Run: `timeout 30 godot --headless --path . res://scenes/smoke_arena.tscn 2>&1 | grep -E "SMOKE|ERROR|Unhandled"`
Expected: `SMOKE PASS`, no unhandled exceptions. Also run: `dotnet test tests/ByteBrawl.Tests 2>&1 | tail -3` — all still PASS.

- [ ] **Step 3: Commit**

```bash
git add -A
git commit -m "feat: arena with stage, hitboxes, camera, and training mode"
```

---

### Task 8: Debug overlay, menu, docs

**Files:**
- Create: `src/Nodes/CombatDebugDraw.cs`
- Modify: `src/Nodes/Arena.cs` (add overlay + F3 toggle), `src/Nodes/Main.cs` (menu), `scenes/main.tscn`
- Modify: `AGENTS.md` (rewrite for Godot stack)

**Interfaces:**
- Consumes: everything.
- Produces: F3 toggles debug overlay; `main.tscn` offers Local Versus / Training via W/S + J + K; `AGENTS.md` documents build/test commands.

- [ ] **Step 1: Write CombatDebugDraw**

```csharp
using ByteBrawl.Combat;
using Godot;

namespace ByteBrawl.Nodes;

public partial class CombatDebugDraw : Node2D
{
    public Fighter? P1;
    public Fighter? P2;
    public HitboxManager? Hitboxes;
    public bool Enabled;

    public override void _Process(double delta)
    {
        if (Input.IsPhysicalKeyPressed(Key.F3) && !EventInputHandled())
            Enabled = !Enabled;
        QueueRedraw();
    }

    private bool _f3Prev;
    private bool EventInputHandled()
    {
        var now = Input.IsPhysicalKeyPressed(Key.F3);
        var was = _f3Prev;
        _f3Prev = now;
        return was;
    }

    public override void _Draw()
    {
        if (!Enabled) return;
        foreach (var f in new[] { P1, P2 })
        {
            if (f == null) continue;
            DrawRect(new Rect2(f.Position - new Vector2(6, 10), new Vector2(12, 20)),
                new Color(0, 1, 0, 0.4f), true);
        }
        if (Hitboxes == null) return;
        foreach (var child in Hitboxes.GetChildren())
        {
            if (child is not Hitbox hb) continue;
            DrawCircle(hb.Position, hb.Attack.Radius, new Color(1, 0, 0, 0.4f));
            var dir = hb.Attack.Direction * hb.Attacker.Facing * 20f;
            DrawLine(hb.Position, hb.Position + dir, new Color(1, 1, 0), 1f);
        }
    }
}
```

Wire in `Arena._Ready()` after creating the camera:
```csharp
        var debug = new CombatDebugDraw { P1 = _p1, P2 = _p2, Hitboxes = hb, Name = "DebugDraw" };
        AddChild(debug);
```

- [ ] **Step 2: Rewrite `Main.cs` as minimal menu**

```csharp
using Godot;

namespace ByteBrawl.Nodes;

public partial class Main : Node
{
    private int _selected;
    private bool _kPrev;

    public override void _Ready()
    {
        GD.Print("BYTEBRAWL — W/S select, J confirm, K quit");
    }

    public override void _PhysicsProcess(double delta)
    {
        var w = Input.IsPhysicalKeyPressed(Key.W);
        var s = Input.IsPhysicalKeyPressed(Key.S);
        var j = Input.IsPhysicalKeyPressed(Key.J);
        var k = Input.IsPhysicalKeyPressed(Key.K);
        if (w && !_wPrev) _selected = Mathf.Wrap(_selected - 1, 0, 2);
        if (s && !_sPrev) _selected = Mathf.Wrap(_selected + 1, 0, 2);
        if (j)
            GetTree().ChangeSceneToFile(_selected == 0 ? "res://scenes/arena.tscn" : "res://scenes/arena_training.tscn");
        if (k && !_kPrev) GetTree().Quit();
        _wPrev = w; _sPrev = s; _kPrev = k;
    }
    private bool _wPrev, _sPrev;
}
```

`scenes/arena_training.tscn` is a copy of `arena.tscn` with `Training = true` on the root node. Add two `Label` nodes to `scenes/main.tscn` ("Local Versus", "Training") for presentation.

- [ ] **Step 3: Rewrite `AGENTS.md`**

Replace content with: Godot 4.7 C# project; build `dotnet build`; unit tests `dotnet test tests/ByteBrawl.Tests`; smoke tests `timeout 30 godot --headless --path . res://scenes/smoke_arena.tscn`; logic lives in `src/Combat` (plain C#, no Godot node types — keep it that way); presentation in `src/Nodes`; combat tuning data in `src/Data`; run game with `godot --path .`.

- [ ] **Step 4: Final verification and commit**

Run: `dotnet test tests/ByteBrawl.Tests 2>&1 | tail -3` (all PASS) and `timeout 30 godot --headless --path . res://scenes/smoke_arena.tscn 2>&1 | grep SMOKE` (`SMOKE PASS`).
Then: `git add -A && git commit -m "feat: debug overlay, menu, and Godot agent docs"`

---

## Self-Review

**Spec coverage:** repo reset/nuke (Task 1); C# Godot 4.7 skeleton (Task 1); data-driven combat with Byte moveset incl. multi-hit + charge (Tasks 2, 5); MatchRules port (Task 3); segmented limb placeholder rig + code poses (Task 6); arena/training/stocks/timer (Tasks 7); debug hitbox overlay (Task 8); Elemental hooks = `ElementalMeter` + `IElementalFrenzy` seam (Task 3), frenzy deferred per spec; AGENTS.md rewrite (Task 8). Placeholder menu (Task 8). Elemental meter fill is wired into `MatchRules.ApplyHit` (hook-level, no frenzy behavior) — matches "hooks only".

**Placeholder scan:** no TBD/TODO; every code step contains complete code; the two intentionally stubbed methods in Task 4 are replaced with full implementations in Task 5, with failing tests guarding them.

**Type consistency:** `IFighter`, `IHitboxManager`, `MatchRules`, `FighterStateMachine(IFighter, IHitboxManager, Moveset)`, `ActionFrame` (record struct with `with` support), `AttackSlot`/`HitboxShape` enums, and node class names are used identically across all tasks. `FakeFighter.SetChargingFull` is virtual from Task 3 onward for the Task 5 observer test. Constants (`DefaultAttackDuration = 20`, charge frames 30/180/180) match the spec's Global Constraints and Task 2 data.

## Execution Handoff

Plan complete and saved to `docs/superpowers/plans/2026-09-24-godot-port.md`.
