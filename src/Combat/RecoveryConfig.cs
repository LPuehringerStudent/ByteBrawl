namespace ByteBrawl.Combat;

// Marks an attack as a recovery: fired in the air with up+heavy, once per
// airtime, with an initial vertical boost. CanActAfter opts out of the
// post-recovery attack lockout (combo-finisher archetype).
public class RecoveryConfig
{
    public float VerticalBoost;
    public float GroundBoost;      // applied when a grounded charge of this attack is released
    public bool CanActAfter = false;
}
