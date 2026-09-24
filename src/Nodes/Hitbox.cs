using ByteBrawl.Combat;
using Godot;

namespace ByteBrawl.Nodes;

public partial class Hitbox : Area2D
{
    public Fighter Attacker = null!;
    public AttackData Attack = null!;
    public int FramesRemaining;
    public bool HasHit;
}
