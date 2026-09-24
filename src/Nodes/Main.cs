using Godot;

namespace ByteBrawl.Nodes;

public partial class Main : Node
{
    public override void _Ready()
    {
        GD.Print("BOOT OK");
        GetTree().Quit();
    }
}
