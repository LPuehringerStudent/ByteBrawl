using Godot;

namespace ByteBrawl.Nodes;

// Esc pause overlay. Runs while the tree is paused (ProcessMode.Always):
// W/S navigate, J activate, Esc resume. In training, offers hitbox/hurtbox
// toggles wired to the arena's CombatDebugDraw.
public partial class PauseMenu : CanvasLayer
{
    private readonly List<(string Label, Action Activate)> _items = new();
    private CombatDebugDraw _debug = null!;
    private bool _training;
    private bool _paused;
    private int _selected;
    private Label _list = null!;
    private bool _escPrev, _wPrev, _sPrev, _jPrev;

    public void Initialize(CombatDebugDraw debug, bool training)
    {
        _debug = debug;
        _training = training;
        ProcessMode = ProcessModeEnum.Always;
        Layer = 10;
        var panel = new PanelContainer { Position = new Vector2(100, 55) };
        _list = new Label();
        _list.AddThemeFontSizeOverride("font_size", 10);
        panel.AddChild(_list);
        AddChild(panel);
        Visible = false;
    }

    public override void _PhysicsProcess(double delta)
    {
        var esc = Input.IsPhysicalKeyPressed(Key.Escape);
        if (esc && !_escPrev) SetPaused(!_paused);

        var w = Input.IsPhysicalKeyPressed(Key.W);
        var s = Input.IsPhysicalKeyPressed(Key.S);
        var j = Input.IsPhysicalKeyPressed(Key.J);
        if (_paused)
        {
            if (w && !_wPrev) { _selected = Mathf.Wrap(_selected - 1, 0, _items.Count); Render(); }
            if (s && !_sPrev) { _selected = Mathf.Wrap(_selected + 1, 0, _items.Count); Render(); }
            if (j && !_jPrev) _items[_selected].Activate();
        }
        _escPrev = esc; _wPrev = w; _sPrev = s; _jPrev = j;
    }

    private void SetPaused(bool pause)
    {
        _paused = pause;
        GetTree().Paused = pause;
        Visible = pause;
        if (pause)
        {
            _selected = 0;
            BuildItems();
            Render();
        }
    }

    private void BuildItems()
    {
        _items.Clear();
        _items.Add(("Resume", () => SetPaused(false)));
        if (_training)
        {
            _items.Add(($"Hitboxes: {OnOff(_debug.ShowHitboxes)}",
                () => { _debug.ShowHitboxes = !_debug.ShowHitboxes; BuildItems(); }));
            _items.Add(($"Hurtboxes: {OnOff(_debug.ShowHurtboxes)}",
                () => { _debug.ShowHurtboxes = !_debug.ShowHurtboxes; BuildItems(); }));
        }
        _items.Add(("Quit to Menu", () =>
        {
            GetTree().Paused = false;
            GetTree().ChangeSceneToFile("res://scenes/main.tscn");
        }));
        _selected = Mathf.Clamp(_selected, 0, _items.Count - 1);
    }

    private void Render()
    {
        var lines = _items.Select((item, i) => (i == _selected ? "> " : "  ") + item.Label);
        _list.Text = string.Join("\n", lines);
    }

    private static string OnOff(bool value) => value ? "ON" : "OFF";
}
