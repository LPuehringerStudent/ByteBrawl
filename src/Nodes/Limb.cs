using Godot;

namespace ByteBrawl.Nodes;

// One body part. Sprite offset sits at the joint pivot, so Rotation
// happens at the shoulder / elbow / hip / knee / ankle.
public partial class Limb : Node2D
{
    public Sprite2D Sprite = null!;
    public Vector2 Size;
    public Vector2 Pivot;

    // Bakes a solid-color texture. (PlaceholderTexture2D renders as a
    // checkerboard at runtime, so it must not be used for game visuals.)
    public static ImageTexture Solid(Vector2 size, Color color)
    {
        var image = Image.CreateEmpty(Math.Max(1, (int)size.X), Math.Max(1, (int)size.Y), false, Image.Format.Rgba8);
        image.Fill(color);
        return ImageTexture.CreateFromImage(image);
    }

    public static Limb Create(string name, Vector2 size, Vector2 pivot, Color color, int z = 0)
    {
        var limb = new Limb { Name = name, Position = Vector2.Zero, Size = size, Pivot = pivot };
        limb.Sprite = new Sprite2D
        {
            Texture = Solid(size, color),
            Offset = -pivot,
            ZIndex = z,
        };
        limb.AddChild(limb.Sprite);
        return limb;
    }
}
