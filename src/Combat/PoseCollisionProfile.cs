using Godot;
using System.Text.Json;

namespace ByteBrawl.Combat;

// Collision coordinates are keyed by animation frame, but contain no visual data.
public sealed record PoseCapsule(string Id, Vector2 Center, float Radius, float Height, float Rotation, LimbGroup Group);
public sealed record PoseCollisionFrame(string Id, IReadOnlyDictionary<string, PoseCapsule> Parts);
public sealed record PoseAttackCircle(int Frame, string Attack, Vector2 Center, float Radius, int Damage);

public sealed class PoseCollisionProfile
{
    private readonly PoseCollisionFrame[] _frames;
    private readonly Dictionary<int, PoseAttackCircle> _attacks;

    public int FrameCount => _frames.Length;
    public IReadOnlyDictionary<string, PoseCapsule> Frame(int index) =>
        index >= 0 && index < _frames.Length ? _frames[index].Parts : throw new ArgumentOutOfRangeException(nameof(index));
    public string FrameId(int index) =>
        index >= 0 && index < _frames.Length ? _frames[index].Id : throw new ArgumentOutOfRangeException(nameof(index));
    public PoseAttackCircle? AttackAt(int frame) => _attacks.GetValueOrDefault(frame);
    public IReadOnlyCollection<PoseAttackCircle> Attacks => _attacks.Values;

    private PoseCollisionProfile(PoseCollisionFrame[] frames, Dictionary<int, PoseAttackCircle> attacks)
    {
        _frames = frames;
        _attacks = attacks;
    }

    public static PoseCollisionProfile Parse(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        if (root.GetProperty("schema").GetInt32() != 1)
            throw new InvalidDataException("Unsupported collision profile schema");

        var frames = new List<PoseCollisionFrame>();
        string[]? expectedParts = null;
        foreach (var sourceFrame in root.GetProperty("frames").EnumerateArray())
        {
            var parts = new Dictionary<string, PoseCapsule>();
            foreach (var part in sourceFrame.GetProperty("parts").EnumerateArray())
            {
                var id = part.GetProperty("id").GetString()!;
                if (id == "backpack" || !Enum.TryParse<LimbGroup>(part.GetProperty("group").GetString(), true, out var group))
                    throw new InvalidDataException($"Invalid collision part {id}");
                var capsule = new PoseCapsule(id,
                    new Vector2(part.GetProperty("x").GetSingle(), part.GetProperty("y").GetSingle()),
                    part.GetProperty("radius").GetSingle(), part.GetProperty("height").GetSingle(),
                    part.GetProperty("rotation").GetSingle(), group);
                if (capsule.Radius <= 0 || capsule.Height < capsule.Radius * 2 ||
                    !float.IsFinite(capsule.Center.X) || !float.IsFinite(capsule.Center.Y) ||
                    !float.IsFinite(capsule.Rotation) || !parts.TryAdd(id, capsule))
                    throw new InvalidDataException($"Invalid capsule {id}");
            }
            var names = parts.Keys.Order(StringComparer.Ordinal).ToArray();
            expectedParts ??= names;
            if (names.Length != 15 || !names.SequenceEqual(expectedParts))
                throw new InvalidDataException("Every collision frame must contain the same 15 body parts");
            frames.Add(new PoseCollisionFrame(sourceFrame.GetProperty("id").GetString()!, parts));
        }
        if (frames.Count == 0) throw new InvalidDataException("Collision profile has no frames");

        var attacks = new Dictionary<int, PoseAttackCircle>();
        foreach (var source in root.GetProperty("attacks").EnumerateArray())
        {
            var circle = new PoseAttackCircle(source.GetProperty("frame").GetInt32(),
                source.GetProperty("attack").GetString()!,
                new Vector2(source.GetProperty("x").GetSingle(), source.GetProperty("y").GetSingle()),
                source.GetProperty("radius").GetSingle(), source.GetProperty("damage").GetInt32());
            if (circle.Frame < 0 || circle.Frame >= frames.Count || circle.Radius <= 0 ||
                circle.Damage < 0 || !attacks.TryAdd(circle.Frame, circle))
                throw new InvalidDataException("Invalid or duplicate attack frame");
        }
        return new PoseCollisionProfile(frames.ToArray(), attacks);
    }
}
