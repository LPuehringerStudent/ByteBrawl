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
}
