using Godot;

namespace ByteBrawl.Nodes;

public partial class Main : Node
{
    private static readonly Color SelectedColor = new(0.2f, 1f, 1f);
    private static readonly Color IdleColor = new(0.55f, 0.55f, 0.6f);

    private int _selected;
    private bool _wPrev, _sPrev, _jPrev, _kPrev;
    private Label _versusLabel = null!;
    private Label _trainingLabel = null!;

    public override void _Ready()
    {
        GD.Print("BYTEBRAWL — W/S select, J confirm, K quit");
        _versusLabel = GetNode<Label>("LocalVersus");
        _trainingLabel = GetNode<Label>("Training");
        UpdateHighlight();
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
        UpdateHighlight();
    }

    private void UpdateHighlight()
    {
        _versusLabel.Text = (_selected == 0 ? "> " : "  ") + "Local Versus";
        _trainingLabel.Text = (_selected == 1 ? "> " : "  ") + "Training";
        _versusLabel.Modulate = _selected == 0 ? SelectedColor : IdleColor;
        _trainingLabel.Modulate = _selected == 1 ? SelectedColor : IdleColor;
    }
}
