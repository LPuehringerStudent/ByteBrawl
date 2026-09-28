# Soldier collision bridge

This branch contains only collision behavior and pose data. It starts from current ByteBrawl `main` and does not copy Soldier sprites, animation scenes, effects, or the separate Soldier game. The older `feat/soldier-physics-hitboxes` branch remains a playable integration prototype.

The collision profile stores 15 hurtbox capsules for each of Soldier's 57 animation frames, plus six active jab, cross, and kick attack circles. The profile records positions in Soldier's existing 128 x 128 canvas coordinates relative to the character origin (64, 121). Thigh and shin capsules are separate. Backpack has no hurtbox.

`PoseCollisionRig` reads the profile and owns only Godot collision areas. The character's animation code remains authoritative: after it selects a frame, it calls `ApplyFrame(frame)` on the rig. Attach the rig under the same visual transform as the sprite, so facing and pixel snapping apply to both. The rig does not advance animations, set sprite textures, or change frame timing. `AttackAt(frame)` exposes the active circle for the existing combat code. Apply one damage event per target per attack, even if multiple limbs overlap.

To merge later, merge this branch into ByteBrawl first, then wire the Soldier animation's frame event to the collision rig. If frame numbering or canvas origin changes, regenerate the collision profile and update the mapping; the art itself does not need to be replaced. The existing ByteBrawl placeholder rig remains available until the shared character is deliberately assembled.
