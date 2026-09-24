using Godot;

namespace ByteBrawl.Nodes;

public partial class SmokeArena : Node
{
    private int _frames;
    private Arena _arena = null!;

    public override void _Ready()
    {
        _arena = new Arena { Training = true };
        AddChild(_arena);
    }

    public override void _PhysicsProcess(double delta)
    {
        if (++_frames < 180) return;
        var p1 = _arena.P1;
        var p2 = _arena.P2;
        var ok = p1.IsOnFloor() && p2.IsOnFloor()
            && Mathf.Abs(p1.Position.X - 110) < 40 && Mathf.Abs(p1.Position.Y - 150) < 30
            && Mathf.Abs(p2.Position.X - 210) < 40 && Mathf.Abs(p2.Position.Y - 150) < 30;
        GD.Print(ok
            ? "SMOKE PASS"
            : $"SMOKE FAIL p1={p1.Position} grounded={p1.IsOnFloor()} p2={p2.Position} grounded={p2.IsOnFloor()}");
        GetTree().Quit(ok ? 0 : 1);
    }
}
