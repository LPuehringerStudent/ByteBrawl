using Godot;

namespace ByteBrawl.Nodes;

public partial class SmokePoseCollision : Node2D
{
    public override void _Ready()
    {
        var visualTransform = new Node2D();
        AddChild(visualTransform);
        var rig = new PoseCollisionRig();
        visualTransform.AddChild(rig);
        var first = rig.Parts["near_thigh"].Position;
        rig.ApplyFrame(1);
        var second = rig.Parts["near_thigh"].Position;
        var thigh = rig.Parts["near_thigh"].GetNode<CollisionShape2D>("Shape");
        var shin = rig.Parts["near_shin"].GetNode<CollisionShape2D>("Shape");
        var ok = rig.Parts.Count == 15 && first != second
            && thigh.Shape is CapsuleShape2D && shin.Shape is CapsuleShape2D
            && rig.AttackAt(25)?.Damage == 2 && rig.AttackAt(26) == null
            && rig.FindChildren("*", "Sprite2D", true, false).Count == 0;
        visualTransform.Scale = new Vector2(-1, 1);
        ok &= Mathf.IsEqualApprox(rig.Parts["near_thigh"].GlobalPosition.X, -second.X);
        GD.Print(ok ? "SMOKE PASS" : "SMOKE FAIL");
        GetTree().Quit(ok ? 0 : 1);
    }
}
