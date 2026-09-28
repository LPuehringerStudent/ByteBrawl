# Soldier collision bridge

This branch contains only collision behavior and pose data. It starts from current ByteBrawl `main` and does not copy Soldier sprites, animation scenes, effects, or the separate Soldier game. The older `feat/soldier-physics-hitboxes` branch remains a playable integration prototype.

The collision profile stores 15 hurtbox capsules for each of Soldier's 57 animation frames, plus six active jab, cross, and kick attack circles. The profile records positions in Soldier's existing 128 x 128 canvas coordinates relative to the character origin (64, 121). Thigh and shin capsules are separate. Backpack has no hurtbox.

`PoseCollisionRig` reads the profile and owns only Godot collision areas. The character's animation code remains authoritative: after it selects a frame, it calls `ApplyFrame(frame)` on the rig. Attach the rig under the same visual transform as the sprite, so facing and pixel snapping apply to both. The rig does not advance animations, set sprite textures, or change frame timing. `AttackAt(frame)` exposes the active circle for the existing combat code. Apply one damage event per target per attack, even if multiple limbs overlap.

In the combined character, set `OwnerFighter` to the ByteBrawl fighter before adding the rig to the scene. Then connect the Soldier visual's `FrameApplied` event to `rig.ApplyFrame`. The parent visual's scale handles facing. Spawn each `AttackAt(frame)` circle using its existing attack serial; the profile supplies its position, radius and 2/3/4% damage, while the existing combat manager decides when and whom it hits. Do not run both the placeholder rig's hurtboxes and this pose rig's hurtboxes on one fighter.

Regenerate only collision data after a deliberate pose edit:

```powershell
node tools/extract-soldier-collision.cjs <Soldier-hurtboxes.json> <Soldier-poses.json> collision/soldier_collision.json
```

The extractor reads JSON pose metadata and never reads or writes images.

To merge later, merge this branch into ByteBrawl first, then wire the Soldier animation's frame event to the collision rig. If frame numbering or canvas origin changes, regenerate the collision profile and update the mapping; the art itself does not need to be replaced. The existing ByteBrawl placeholder rig remains available until the shared character is deliberately assembled.

Verification: `dotnet test tests/ByteBrawl.Tests`, then run `scenes/smoke_pose_collision.tscn`, `scenes/smoke_arena.tscn`, and `scenes/smoke_limb_rig.tscn` with Godot 4.7.2 .NET. The new smoke scene checks frame updates, separate thigh/shin capsules, mirroring and the absence of sprites in the collision rig.
