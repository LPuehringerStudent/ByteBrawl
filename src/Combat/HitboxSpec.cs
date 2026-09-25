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
}
