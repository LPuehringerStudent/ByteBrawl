using Godot;

namespace ByteBrawl.Nodes;

// One body-part hurtbox. The rig carries one per limb (circle at the
// segment midpoint); Fighter assigns OwnerFighter on _Ready.
public partial class Hurtbox : Area2D
{
    public Fighter? OwnerFighter;
    public float Radius;
}
