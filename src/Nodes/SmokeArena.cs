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
        _frames++;
        var p1 = _arena.P1;
        var p2 = _arena.P2;

        if (_frames == 10) GetViewport().PushInput(new InputEventKey { PhysicalKeycode = Key.F4, Pressed = true });
        if (_frames == 11)
        {
            if (!_arena.GetNode<CollisionOverlay>("CollisionOverlay").Visible) { Fail("F4 did not show collisions"); return; }
            GetViewport().PushInput(new InputEventKey { PhysicalKeycode = Key.F4, Pressed = false });
        }
        if (_frames == 12) GetViewport().PushInput(new InputEventKey { PhysicalKeycode = Key.F4, Pressed = true });
        if (_frames == 13)
        {
            if (_arena.GetNode<CollisionOverlay>("CollisionOverlay").Visible) { Fail("F4 did not hide collisions"); return; }
            GetViewport().PushInput(new InputEventKey { PhysicalKeycode = Key.F4, Pressed = false });
        }

        if (_frames == 180)
        {
            var ok = p1.IsOnFloor() && p2.IsOnFloor()
                && Mathf.Abs(p1.Position.X - 110) < 40 && Mathf.Abs(p1.Position.Y - 150) < 30
                && Mathf.Abs(p2.Position.X - 210) < 40 && Mathf.Abs(p2.Position.Y - 150) < 30;
            if (!ok) { Fail($"settle p1={p1.Position} grounded={p1.IsOnFloor()} p2={p2.Position} grounded={p2.IsOnFloor()}"); return; }
            // Phase 2: put P1 above the left thin platform and let it land on top.
            p1.Respawn(new Vector2(56, 40), 0);
        }

        if (_frames == 240)
        {
            // Standing on the one-way platform (top y=80, 20px box → center ≈ 70).
            var ok = p1.IsOnFloor() && Mathf.Abs(p1.Position.X - 56) < 6 && Mathf.Abs(p1.Position.Y - 70) < 6;
            if (!ok) { Fail($"one-way land p1={p1.Position} grounded={p1.IsOnFloor()}"); return; }
            InjectKey(Key.S, true); // hold down: drop through
        }

        if (_frames == 300)
        {
            InjectKey(Key.S, false);
            // Fell well past the 16px platform onto the main stage below.
            var ok = p1.Position.Y > 110 && p1.IsOnFloor();
            GD.Print(ok ? "SMOKE PASS" : $"SMOKE FAIL drop-through p1={p1.Position} grounded={p1.IsOnFloor()}");
            GetTree().Quit(ok ? 0 : 1);
        }
    }

    private void Fail(string why)
    {
        GD.Print($"SMOKE FAIL {why}");
        GetTree().Quit(1);
    }

    private static void InjectKey(Key key, bool pressed)
        => Input.ParseInputEvent(new InputEventKey { PhysicalKeycode = key, Pressed = pressed });
}
