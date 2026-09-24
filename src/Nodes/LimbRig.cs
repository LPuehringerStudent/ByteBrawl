using Godot;

namespace ByteBrawl.Nodes;

public partial class LimbRig : Node2D
{
    public Limb Find(string name) => (Limb)FindChild(name, recursive: true, owned: false)!;

    public static LimbRig CreatePlaceholder()
    {
        var rig = new LimbRig();
        var torso = Limb.Create("Torso", new Vector2(10, 12), new Vector2(5, 0), new Color("#00cccc"));
        var head = Limb.Create("Head", new Vector2(8, 8), new Vector2(4, 8), new Color("#00ffff"));
        head.Position = new Vector2(0, -14);
        torso.AddChild(head);
        var nearArm = Limb.Create("NearArm", new Vector2(4, 10), new Vector2(2, 0), new Color("#009999"));
        nearArm.Position = new Vector2(6, -10);
        var nearLeg = Limb.Create("NearLeg", new Vector2(4, 12), new Vector2(2, 0), new Color("#008888"));
        nearLeg.Position = new Vector2(2, 12);
        torso.AddChild(nearArm);
        torso.AddChild(nearLeg);
        rig.AddChild(torso);
        return rig;
    }
}
