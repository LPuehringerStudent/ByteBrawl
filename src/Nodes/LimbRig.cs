using ByteBrawl.Combat;
using Godot;

namespace ByteBrawl.Nodes;

// 16-part puppet rig, hierarchy modeled on the reference limb system:
//   pelvis -> far leg (behind)
//   pelvis -> torso -> backpack, far arm (behind), head, near arm (front)
//   pelvis -> near leg (front)
// Every body part except the backpack carries a hurtbox capsule derived
// from the part's sprite size/pivot, so hitboxes hug whatever art is in
// place (placeholders now, real sprites later) with no manual placement.
public partial class LimbRig : Node2D
{
    public const int PartCount = 16;
    public const int HurtboxCount = 15;

    public Limb Find(string name) => (Limb)FindChild(name, recursive: true, owned: false)!;
    public readonly List<Hurtbox> Hurtboxes = new();

    public static LimbRig CreatePlaceholder()
    {
        var near = new Color(0.8f, 0.95f, 0.95f);
        var mid = near.Darkened(0.25f);
        var far = near.Darkened(0.5f);

        var rig = new LimbRig();

        var pelvis = Limb.Create("Pelvis", new Vector2(10, 5), new Vector2(5, 2), mid);
        AttachHurtbox(rig, pelvis);
        var torso = Limb.Create("Torso", new Vector2(10, 10), new Vector2(5, 10), near);
        torso.Position = new Vector2(0, -3);
        AttachHurtbox(rig, torso);
        pelvis.AddChild(torso);

        var backpack = Limb.Create("Backpack", new Vector2(3, 7), new Vector2(1.5f, 1), far, z: -1);
        backpack.Position = new Vector2(-3, -9);
        torso.AddChild(backpack);

        var head = Limb.Create("Head", new Vector2(8, 8), new Vector2(4, 8), near.Lerp(Colors.White, 0.2f), z: 1);
        head.Position = new Vector2(0, -10);
        AttachHurtbox(rig, head);
        torso.AddChild(head);

        // arms: shoulder -> elbow -> hand
        var farArm = Arm(rig, "Far", far, z: -1, shoulder: new Vector2(2, -9));
        var nearArm = Arm(rig, "Near", near, z: 1, shoulder: new Vector2(8, -9));
        torso.AddChild(farArm);
        torso.AddChild(nearArm);

        // legs: hip -> knee -> foot
        var farLeg = Leg(rig, "Far", far, z: -1, hip: new Vector2(2, 2));
        var nearLeg = Leg(rig, "Near", near, z: 1, hip: new Vector2(6, 2));
        pelvis.AddChild(farLeg);
        pelvis.AddChild(nearLeg);

        rig.AddChild(pelvis);
        return rig;
    }

    private static Limb Arm(LimbRig rig, string side, Color color, int z, Vector2 shoulder)
    {
        var upper = Limb.Create($"{side}UpperArm", new Vector2(3, 7), new Vector2(1.5f, 0), color, z);
        upper.Position = shoulder;
        AttachHurtbox(rig, upper);
        var fore = Limb.Create($"{side}Forearm", new Vector2(3, 7), new Vector2(1.5f, 0), color, z);
        fore.Position = new Vector2(0, 7);
        AttachHurtbox(rig, fore);
        var hand = Limb.Create($"{side}Hand", new Vector2(3, 3), new Vector2(1.5f, 0), color.Darkened(0.1f), z);
        hand.Position = new Vector2(0, 7);
        AttachHurtbox(rig, hand);
        fore.AddChild(hand);
        upper.AddChild(fore);
        return upper;
    }

    private static Limb Leg(LimbRig rig, string side, Color color, int z, Vector2 hip)
    {
        var thigh = Limb.Create($"{side}Thigh", new Vector2(4, 8), new Vector2(2, 0), color, z);
        thigh.Position = hip;
        AttachHurtbox(rig, thigh);
        var shin = Limb.Create($"{side}Shin", new Vector2(4, 8), new Vector2(2, 0), color, z);
        shin.Position = new Vector2(0, 8);
        AttachHurtbox(rig, shin);
        var foot = Limb.Create($"{side}Foot", new Vector2(5, 3), new Vector2(2, 1), color.Darkened(0.1f), z);
        foot.Position = new Vector2(0, 8);
        AttachHurtbox(rig, foot);
        shin.AddChild(foot);
        thigh.AddChild(shin);
        return thigh;
    }

    // Part-name -> group mapping; see spec. Backpack has no hurtbox.
    // "Forearm" must match too, hence case-insensitive "arm".
    public static LimbGroup GroupFor(string partName) =>
        partName.Contains("Head") ? LimbGroup.Head
        : partName.Contains("Arm", StringComparison.OrdinalIgnoreCase) || partName.Contains("Hand") ? LimbGroup.Arm
        : partName.Contains("Thigh") || partName.Contains("Shin") || partName.Contains("Foot") ? LimbGroup.Leg
        : LimbGroup.Torso; // Torso, Pelvis

    // Circle placement for limb-anchored hitbox chains: same geometry rule as
    // hurtboxes (center on the segment, radius = half the limb thickness),
    // in limb-local space — the limb's own transform handles rotation.
    public static (Vector2 Center, float Radius) ChainGeometry(Vector2 size, Vector2 pivot, float radiusScale)
    {
        var center = size / 2 - pivot;
        var alongX = size.X > size.Y;
        var radius = (alongX ? size.Y : size.X) / 2f * radiusScale;
        return (center, radius);
    }

    // Capsule from the part's own geometry: centered on the sprite, radius =
    // half the limb thickness, height along the long axis with slight joint overlap.
    private static void AttachHurtbox(LimbRig rig, Limb limb)
    {
        var size = limb.Size;
        var center = size / 2 - limb.Pivot;
        var alongX = size.X > size.Y;
        var length = alongX ? size.X : size.Y;
        var radius = (alongX ? size.Y : size.X) / 2f;
        var height = length + radius * 0.6f;
        var area = new Hurtbox { Radius = radius, Height = height, Position = center, Group = GroupFor(limb.Name) };
        if (alongX) area.RotationDegrees = 90f;
        area.AddChild(new CollisionShape2D { Shape = new CapsuleShape2D { Radius = radius, Height = height } });
        limb.AddChild(area);
        rig.Hurtboxes.Add(area);
    }
}
