using ByteBrawl.Combat;
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
                 && rig.Hurtboxes.Count == LimbRig.HurtboxCount
                 && rig.Hurtboxes.Count(h => h.Group == LimbGroup.Head) == 1
                 && rig.Hurtboxes.Count(h => h.Group == LimbGroup.Torso) == 2
                 && rig.Hurtboxes.Count(h => h.Group == LimbGroup.Arm) == 6
                 && rig.Hurtboxes.Count(h => h.Group == LimbGroup.Leg) == 6;
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
