using ByteBrawl.Combat;
using Godot;

namespace ByteBrawl.Nodes;

public partial class Hitbox : Area2D
{
    public Fighter Attacker = null!;
    public AttackData Attack = null!;
    public HitboxSpec Spec = null!;
    public int FramesRemaining;
    public bool HasHit;
    public int RehitTimer;      // counts down to HasHit=false when Spec.RehitFrames > 0
    public float ResolvedRadius; // actual collision/draw radius (chain circles derive from the limb)
    // Limb-anchored chain circle: follows the limb's transform each frame.
    // When null, follows the attacker at Spec.OffsetX/OffsetY instead.
    public Limb? Anchor;
    public Vector2 AnchorOffset;
}
