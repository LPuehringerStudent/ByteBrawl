using Godot;

namespace ByteBrawl.Nodes;

// 16-part puppet rig, hierarchy modeled on the reference limb system:
//   pelvis -> far leg (behind)
//   pelvis -> torso -> backpack, far arm (behind), head, near arm (front)
//   pelvis -> near leg (front)
public partial class LimbRig : Node2D
{
    public const int PartCount = 16;

    public Limb Find(string name) => (Limb)FindChild(name, recursive: true, owned: false)!;

    public static LimbRig CreatePlaceholder()
    {
        var near = new Color(0.8f, 0.95f, 0.95f);
        var mid = near.Darkened(0.25f);
        var far = near.Darkened(0.5f);

        var rig = new LimbRig();

        var pelvis = Limb.Create("Pelvis", new Vector2(10, 5), new Vector2(5, 2), mid);
        var torso = Limb.Create("Torso", new Vector2(10, 10), new Vector2(5, 10), near);
        torso.Position = new Vector2(0, -3);
        pelvis.AddChild(torso);

        var backpack = Limb.Create("Backpack", new Vector2(3, 7), new Vector2(1.5f, 1), far, z: -1);
        backpack.Position = new Vector2(-3, -9);
        torso.AddChild(backpack);

        var head = Limb.Create("Head", new Vector2(8, 8), new Vector2(4, 8), near.Lerp(Colors.White, 0.2f), z: 1);
        head.Position = new Vector2(0, -10);
        torso.AddChild(head);

        // arms: shoulder -> elbow -> hand
        var farArm = Arm("Far", far, z: -1, shoulder: new Vector2(2, -9));
        var nearArm = Arm("Near", near, z: 1, shoulder: new Vector2(8, -9));
        torso.AddChild(farArm);
        torso.AddChild(nearArm);

        // legs: hip -> knee -> foot
        var farLeg = Leg("Far", far, z: -1, hip: new Vector2(2, 2));
        var nearLeg = Leg("Near", near, z: 1, hip: new Vector2(6, 2));
        pelvis.AddChild(farLeg);
        pelvis.AddChild(nearLeg);

        rig.AddChild(pelvis);
        return rig;
    }

    private static Limb Arm(string side, Color color, int z, Vector2 shoulder)
    {
        var upper = Limb.Create($"{side}UpperArm", new Vector2(3, 7), new Vector2(1.5f, 0), color, z);
        upper.Position = shoulder;
        var fore = Limb.Create($"{side}Forearm", new Vector2(3, 7), new Vector2(1.5f, 0), color, z);
        fore.Position = new Vector2(0, 7);
        var hand = Limb.Create($"{side}Hand", new Vector2(3, 3), new Vector2(1.5f, 0), color.Darkened(0.1f), z);
        hand.Position = new Vector2(0, 7);
        fore.AddChild(hand);
        upper.AddChild(fore);
        return upper;
    }

    private static Limb Leg(string side, Color color, int z, Vector2 hip)
    {
        var thigh = Limb.Create($"{side}Thigh", new Vector2(4, 8), new Vector2(2, 0), color, z);
        thigh.Position = hip;
        var shin = Limb.Create($"{side}Shin", new Vector2(4, 8), new Vector2(2, 0), color, z);
        shin.Position = new Vector2(0, 8);
        var foot = Limb.Create($"{side}Foot", new Vector2(5, 3), new Vector2(2, 1), color.Darkened(0.1f), z);
        foot.Position = new Vector2(0, 8);
        shin.AddChild(foot);
        thigh.AddChild(shin);
        return thigh;
    }
}
