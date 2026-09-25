# Soldier physics integration

Branch `feat/soldier-physics-hitboxes` adds the complete Soldier training game in `soldier/`. ByteBrawl's existing main game remains at the repository root.

## Run

Use Godot **4.7.2 .NET** and .NET SDK 8 or newer:

```powershell
dotnet build "soldier/Unnamed Fighting Game.csproj"
godot --path soldier --editor --import
godot --path soldier
```

Controls: A/D or arrows move; Space/W/Up jump and double jump; J chains jab/cross/kick (2/3/4%); R resets; Esc opens dummy settings; F3 displays hurtboxes and active attack circles.

## Shared physics and collision

`src/Combat/FighterPhysics.cs` extracts ByteBrawl's directional movement, neutral airborne drift, gravity and pre-hit-percentage knockback calculations. Both the original ByteBrawl controller and the Soldier project compile this same source file. Soldier tuning retains its native 235 run speed, 465 jump impulse, 1350 gravity, stronger second jump and existing attack steering/timing. No shield/dodge/charge mechanics are added to the Soldier's current moveset.

Fifteen animation-following capsules cover head, torso, pelvis, both upper arms/forearms/hands, thighs/shins/feet. Backpack excluded. Geometry is derived from existing approved pixels and pose joints, using ByteBrawl's capsule sizing convention. Terrain capsule stays radius 12 / height 108; attack circles stay radius 9 at their original sockets and frames. Contact is deduplicated by fighter so several overlapping limb capsules cannot multiply one hit.

Sprites, effects, stage, 57-frame atlas ordering, character design and part dimensions are unchanged. `soldier/assets/soldier_frames/hurtboxes.json` records capsule geometry for every pose. The `.import` settings are intentionally tracked to preserve exact RGBA art. `soldier/.gdignore` prevents the parent Godot project from scanning this independent project.

## Verify

```powershell
dotnet test tests/ByteBrawl.Tests
dotnet build "soldier/Unnamed Fighting Game.csproj"
godot --headless --path soldier res://tests/limb_physics_smoke.tscn
godot --headless --path soldier res://tests/movement_smoke.tscn
godot --headless --path soldier res://tests/training_smoke.tscn
godot --path soldier res://tests/responsive_smoke.tscn
```

`art/PIXELLOID_REPORT.json` is the Soldier smoke tests' existing pixel-identity fixture. Captures go to ignored `artifacts/`. Root ByteBrawl build excludes `soldier/**/*.cs`; the child Godot project has its own assembly and imports.

Source provenance: ByteBrawl baseline `9c84125651c7f72f919f325d433d6187bb5fd77c`; Soldier assets and game from David-Fruehwirt/unnamed_fighting_game, baseline `d6bbbe3`, followed by this integration. No unrelated references or local IDE files are included.

## Verified result

Root ByteBrawl: 67 unit tests passed; both original Godot smoke scenes passed. Child Soldier: 435 movement, 120 combat, 599 limb/physics and 72 rendered resize checks passed. Builds succeed for both projects. All 23 PNG assets are byte-identical to the original Soldier baseline; all previous pose metadata and import settings preserved. See [asset preservation report](soldier-verification.json).

Limb checks cover all animation frames in both directions, real thigh/shin collisions and once-per-fighter damage when several parts overlap. Debug overlay reviewed in-game. The parent editor may report that it is ignoring the nested Soldier project; this is intentional. Open `soldier/project.godot` separately to edit it.
