using Godot;

namespace ByteBrawl.Nodes;

// One body part. Sprite offset sits at the joint pivot, so Rotation
// happens at the shoulder / elbow / hip / knee.
public partial class Limb : Node2D
{
    public Sprite2D Sprite = null!;

    public static Limb Create(string name, Vector2 size, Vector2 pivot, Color color)
    {
        var limb = new Limb { Name = name, Position = Vector2.Zero };
        var tex = new PlaceholderTexture2D { Size = size };
        limb.Sprite = new Sprite2D
        {
            Texture = tex,
            Offset = -pivot,
            Modulate = color,
        };
        limb.AddChild(limb.Sprite);
        return limb;
    }
}
