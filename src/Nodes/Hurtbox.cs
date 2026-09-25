using ByteBrawl.Combat;
using Godot;

namespace ByteBrawl.Nodes;

// One body-part hurtbox: a capsule spanning the limb segment
// (joint to joint), child of the limb so it follows pose and facing.
// Fighter assigns OwnerFighter on _Ready. Group comes from the part name
// (LimbRig.GroupFor); CurrentType is driven by the FSM via
// IFighter.SetHurtboxOverride (dodge intangibility, attack-stage armor).
public partial class Hurtbox : Area2D
{
    public Fighter? OwnerFighter;
    public float Radius;
    public float Height;
    public LimbGroup Group;
    public HurtboxType CurrentType = HurtboxType.Vulnerable;
    public float ArmorBreakKb = float.MaxValue; // only meaningful for SuperArmor
}
