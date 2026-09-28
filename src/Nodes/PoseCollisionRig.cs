using ByteBrawl.Combat;
using Godot;

namespace ByteBrawl.Nodes;

// Attach this under the visual transform. The visual's animation clock calls ApplyFrame.
// This node creates collision areas only; it never creates or changes sprites.
public partial class PoseCollisionRig : Node2D
{
    [Export(PropertyHint.File, "*.json")]
    public string ProfilePath { get; set; } = "res://collision/soldier_collision.json";
    public Fighter? OwnerFighter { get; set; }
    public PoseCollisionProfile Profile { get; private set; } = null!;
    public IReadOnlyDictionary<string, Hurtbox> Parts => _parts;
    private readonly Dictionary<string, Hurtbox> _parts = new();
    private readonly Dictionary<string, CapsuleShape2D> _shapes = new();

    public override void _Ready()
    {
        if (Profile != null) return;
        Configure(PoseCollisionProfile.Parse(Godot.FileAccess.GetFileAsString(ProfilePath)), OwnerFighter);
    }

    public void Configure(PoseCollisionProfile profile, Fighter? owner)
    {
        if (Profile != null) throw new InvalidOperationException("Collision rig is already configured");
        Profile = profile;
        OwnerFighter = owner;
        foreach (var (id, pose) in profile.Frame(0))
        {
            var capsule = new CapsuleShape2D { Radius = pose.Radius, Height = pose.Height };
            var area = new Hurtbox { Name = id, OwnerFighter = owner, Group = pose.Group };
            area.AddChild(new CollisionShape2D { Name = "Shape", Shape = capsule });
            AddChild(area);
            _parts.Add(id, area);
            _shapes.Add(id, capsule);
        }
        ApplyFrame(0);
    }

    public void ApplyFrame(int frame)
    {
        if (Profile == null) throw new InvalidOperationException("Collision rig must be configured first");
        foreach (var (id, pose) in Profile.Frame(frame))
        {
            var area = _parts[id];
            var capsule = _shapes[id];
            area.Position = pose.Center;
            area.Rotation = pose.Rotation;
            area.Radius = capsule.Radius = pose.Radius;
            area.Height = capsule.Height = pose.Height;
            area.Group = pose.Group;
        }
    }

    public PoseAttackCircle? AttackAt(int frame) => Profile.AttackAt(frame);
}
