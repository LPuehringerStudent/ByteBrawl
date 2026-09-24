using Godot;

namespace ByteBrawl.Nodes;

public partial class SmokeLimbRig : Node2D
{
    public override void _Ready()
    {
        var rig = LimbRig.CreatePlaceholder();
        AddChild(rig);
        rig.Find("NearArm").RotationDegrees = -90f;
        var ok = rig.Find("NearArm").RotationDegrees == -90f
                 && rig.Find("Torso").GetNode<Limb>("NearLeg") != null;
        GD.Print(ok ? "SMOKE PASS" : "SMOKE FAIL");
        GetTree().Quit(ok ? 0 : 1);
    }
}
