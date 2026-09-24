using Godot;

namespace ByteBrawl.Nodes;

public partial class Main : Node
{
    private int _selected;
    private bool _wPrev, _sPrev, _jPrev, _kPrev;

    public override void _Ready()
    {
        GD.Print("BYTEBRAWL — W/S select, J confirm, K quit");
    }

    public override void _PhysicsProcess(double delta)
    {
        var w = Input.IsPhysicalKeyPressed(Key.W);
        var s = Input.IsPhysicalKeyPressed(Key.S);
        var j = Input.IsPhysicalKeyPressed(Key.J);
        var k = Input.IsPhysicalKeyPressed(Key.K);
        if (w && !_wPrev) _selected = Mathf.Wrap(_selected - 1, 0, 2);
        if (s && !_sPrev) _selected = Mathf.Wrap(_selected + 1, 0, 2);
        if (j && !_jPrev)
            GetTree().ChangeSceneToFile(_selected == 0 ? "res://scenes/arena.tscn" : "res://scenes/arena_training.tscn");
        if (k && !_kPrev) GetTree().Quit();
        _wPrev = w; _sPrev = s; _jPrev = j; _kPrev = k;
    }
}
