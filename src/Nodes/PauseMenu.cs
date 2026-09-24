using Godot;

namespace ByteBrawl.Nodes;

// Esc pause overlay. Runs while the tree is paused (ProcessMode.Always):
// W/S navigate, J activate, Esc resume. In training, offers hitbox/hurtbox
// toggles wired to the arena's CombatDebugDraw.
public partial class PauseMenu : CanvasLayer
{
    private static readonly Color SelectedColor = new(0.2f, 1f, 1f);
    private static readonly Color IdleColor = new(0.75f, 0.75f, 0.8f);

    private sealed record Row(string Name, Action Activate, Func<string>? State);

    private readonly List<Row> _rows = new();
    private readonly List<Label> _rowLabels = new();
    private CombatDebugDraw _debug = null!;
    private bool _paused;
    private int _selected;
    private bool _escPrev, _wPrev, _sPrev, _jPrev;

    public void Initialize(CombatDebugDraw debug, bool training)
    {
        _debug = debug;
        ProcessMode = ProcessModeEnum.Always;
        Layer = 10;

        var center = new CenterContainer();
        center.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        var panel = new PanelContainer();
        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.1f, 0.1f, 0.18f, 0.92f),
            BorderColor = new Color(0.35f, 0.35f, 0.45f),
        };
        style.SetBorderWidthAll(2);
        style.SetCornerRadiusAll(4);
        panel.AddThemeStyleboxOverride("panel", style);
        var box = new VBoxContainer();
        var title = new Label { Text = "PAUSED", HorizontalAlignment = HorizontalAlignment.Center };
        title.AddThemeFontSizeOverride("font_size", 14);
        box.AddChild(title);
        box.AddChild(new HSeparator());
        panel.AddChild(box);
        center.AddChild(panel);
        AddChild(center);

        _rows.Add(new Row("Resume", () => SetPaused(false), null));
        if (training)
        {
            _rows.Add(new Row("Hitboxes", () => { _debug.ShowHitboxes = !_debug.ShowHitboxes; Render(); },
                () => OnOff(_debug.ShowHitboxes)));
            _rows.Add(new Row("Hurtboxes", () => { _debug.ShowHurtboxes = !_debug.ShowHurtboxes; Render(); },
                () => OnOff(_debug.ShowHurtboxes)));
        }
        _rows.Add(new Row("Quit to Menu", () =>
        {
            GetTree().Paused = false;
            GetTree().ChangeSceneToFile("res://scenes/main.tscn");
        }, null));

        foreach (var row in _rows)
        {
            var label = new Label();
            label.AddThemeFontSizeOverride("font_size", 10);
            box.AddChild(label);
            _rowLabels.Add(label);
        }
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
            if (w && !_wPrev) { _selected = Mathf.Wrap(_selected - 1, 0, _rows.Count); Render(); }
            if (s && !_sPrev) { _selected = Mathf.Wrap(_selected + 1, 0, _rows.Count); Render(); }
            if (j && !_jPrev) _rows[_selected].Activate();
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
            Render();
        }
    }

    private void Render()
    {
        for (var i = 0; i < _rows.Count; i++)
        {
            var state = _rows[i].State?.Invoke();
            _rowLabels[i].Text = (i == _selected ? "> " : "  ") + _rows[i].Name + (state != null ? $"  [{state}]" : "");
            _rowLabels[i].Modulate = i == _selected ? SelectedColor : IdleColor;
        }
    }

    private static string OnOff(bool value) => value ? "ON" : "OFF";
}
