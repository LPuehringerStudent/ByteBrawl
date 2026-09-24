using Godot;

namespace ByteBrawl.Nodes;

// One body-part hurtbox: a capsule spanning the limb segment
// (joint to joint), child of the limb so it follows pose and facing.
// Fighter assigns OwnerFighter on _Ready.
public partial class Hurtbox : Area2D
{
    public Fighter? OwnerFighter;
    public float Radius;
    public float Height;
}
