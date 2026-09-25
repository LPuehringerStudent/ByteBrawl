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
