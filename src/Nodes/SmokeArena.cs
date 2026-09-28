using ByteBrawl.Combat;
using Godot;

namespace ByteBrawl.Nodes;

public partial class SmokeArena : Node
{
    private int _frames;
    private bool _sawWallSlide;
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
            var ok = p1.Position.Y > 110 && p1.IsOnFloor();
            if (!ok) { Fail($"drop-through p1={p1.Position} grounded={p1.IsOnFloor()}"); return; }
            // Phase 3: spawn P1 just outside the stage's left wall holding D —
            // it presses into the wall and should wall-slide at capped speed.
            p1.Respawn(new Vector2(9, 170), 0);
            InjectKey(Key.D, true);
        }

        if (_frames is > 300 and < 330)
        {
            if (p1.Position.Y is > 172 and < 200 && p1.Velocity.Y > 0)
            {
                // Threshold leaves room for a one-frame-stale read (ordering);
                // true free-fall through this band is 200+.
                if (p1.Velocity.Y > FighterStateMachine.WallSlideSpeed + 20)
                { Fail($"wall slide too fast: vy={p1.Velocity.Y} y={p1.Position.Y}"); return; }
                _sawWallSlide = true;
            }
        }

        if (_frames == 330)
        {
            if (!_sawWallSlide) { Fail("never wall-slid along the stage side"); return; }
            InjectKey(Key.Space, true); // wall jump (D still held: lockout must protect the hop)
        }

        if (_frames == 350)
        {
            InjectKey(Key.Space, false);
            InjectKey(Key.D, false);
            // The hop pushed P1 away from the wall (left) despite holding D.
            var ok = p1.Position.X < 8;
            GD.Print(ok ? "SMOKE PASS" : $"SMOKE FAIL wall jump x={p1.Position.X} vx={p1.Velocity.X}");
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
