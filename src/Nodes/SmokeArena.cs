using Godot;

namespace ByteBrawl.Nodes;

public partial class SmokeArena : Node
{
    private int _frames;

    public override void _Ready()
    {
        var arena = new Arena { Training = true };
        AddChild(arena);
    }

    public override void _PhysicsProcess(double delta)
    {
        if (++_frames < 120) return;
        GD.Print("SMOKE PASS");
        GetTree().Quit(0);
    }
}
