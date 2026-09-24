using Godot;

namespace ByteBrawl.Nodes;

public partial class SmokeLimbRig : Node2D
{
    public override void _Ready()
    {
        var rig = LimbRig.CreatePlaceholder();
        AddChild(rig);
        rig.Find("NearUpperArm").RotationDegrees = -90f;
        var ok = rig.Find("NearUpperArm").RotationDegrees == -90f
                 && rig.Find("NearForearm") != null
                 && rig.Find("NearThigh") != null
                 && rig.Find("FarFoot") != null
                 && CountLimbs(rig) == LimbRig.PartCount
                 && rig.Hurtboxes.Count == LimbRig.HurtboxCount;
        GD.Print(ok ? "SMOKE PASS" : "SMOKE FAIL");
        GetTree().Quit(ok ? 0 : 1);
    }

    private static int CountLimbs(Node node)
    {
        var count = node is Limb ? 1 : 0;
        foreach (var child in node.GetChildren())
            count += CountLimbs(child);
        return count;
    }
}
