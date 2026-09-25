using Godot;
namespace UnnamedFightingGame;

/// <summary>A damage-receiving body part, independent of the terrain collision capsule.</summary>
public partial class LimbHurtbox : Area2D
{
    public Soldier Fighter { get; set; } = null!;
    public string Group { get; set; } = "";
    public CapsuleShape2D Capsule { get; } = new();
    public override void _Ready()
    {
        CollisionLayer=8; CollisionMask=0; Monitoring=false;
        AddChild(new CollisionShape2D { Name="Shape", Shape=Capsule });
    }
}
