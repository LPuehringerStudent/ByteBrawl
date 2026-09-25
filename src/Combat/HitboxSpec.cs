using Godot;

namespace ByteBrawl.Combat;

// One circular hitbox of an attack. Several specs per attack/stage give
// capsule-like coverage along a limb (elbow circle + fist circle) or
// sweet/sour spots via the damage/knockback overrides.
public class HitboxSpec
{
    public float OffsetX;
    public float OffsetY;
    public float Radius = 6f;
    public float? DamageOverride;
    public float? KnockbackOverride;
    public HitboxType Type = HitboxType.Damage;
    public Vector2 Push = Vector2.Zero;      // Wind: applied to victim velocity, no damage
    public string SearchId = "";             // Search: detection-only callback key
    public GrabData? Grab;                   // Grab: throw data (mechanics unbuilt)
    public bool NoKnockback;                 // Stun-only: damage + hitstun, zero knockback (multihit filler)
    // Rehit: after hitting, the box may hit the same opponent again every N
    // frames while active (Mario SJP "rehit rate of 2"). 0 = hit once.
    public int RehitFrames;
    // Limb-anchored chain: when non-empty, one circle per named limb,
    // auto-derived from limb geometry and following the limb's pose per frame.
    // When empty, the fixed OffsetX/OffsetY circle is used instead.
    public List<string> LimbChain = new();
    public float RadiusScale = 1f;           // radius multiplier vs. the limb-derived default
}
