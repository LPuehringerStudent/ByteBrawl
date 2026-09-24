using ByteBrawl.Combat;
using Godot;

namespace ByteBrawl.Nodes;

public partial class Arena : Node2D
{
    [Export] public bool Training;

    private static readonly Rect2[] Platforms =
    {
        new(32, 160, 224, 48),   // main floor (rows 10-12, cols 2-15)
        new(32, 80, 48, 16),     // left thin platform
        new(224, 80, 48, 16),    // right thin platform
    };
    private static readonly Rect2 BlastZone = new(-160, -180, 640, 540);

    private MatchRules _rules = null!;
    private Fighter _p1 = null!;
    private Fighter _p2 = null!;

    public override void _Ready()
    {
        BuildStage();
        var hb = new HitboxManager { Name = "HitboxManager" };
        AddChild(hb);

        var fighterScene = GD.Load<PackedScene>("res://scenes/fighter.tscn");
        _p1 = fighterScene.Instantiate<Fighter>();
        _p1.PlayerIndex = 1;
        _p1.Position = new Vector2(110, 110);
        AddChild(_p1);
        _p2 = fighterScene.Instantiate<Fighter>();
        _p2.PlayerIndex = Training ? 0 : 2; // 0 = dummy
        _p2.Position = new Vector2(210, 110);
        AddChild(_p2);

        _rules = new MatchRules(new MatchRulesConfig(), _p1, _p2);
        hb.Rules = _rules;

        var cam = new ArenaCamera { P1 = _p1, P2 = _p2 };
        AddChild(cam);

        var debug = new CombatDebugDraw { P1 = _p1, P2 = _p2, Hitboxes = hb, Name = "DebugDraw" };
        AddChild(debug);

        BuildHud();
    }

    private Label _p1Label = null!;
    private Label _p2Label = null!;
    private Label _timerLabel = null!;
    private Label _bannerLabel = null!;
    private bool _matchEnded;

    private static Label MakeLabel(float x, float y, int fontSize, HorizontalAlignment align = HorizontalAlignment.Left)
    {
        var label = new Label
        {
            Position = new Vector2(x, y),
            HorizontalAlignment = align,
        };
        label.AddThemeFontSizeOverride("font_size", fontSize);
        return label;
    }

    private void BuildHud()
    {
        var layer = new CanvasLayer();
        AddChild(layer);
        _p1Label = MakeLabel(8, 4, 10);
        _p2Label = MakeLabel(312, 4, 10, HorizontalAlignment.Right);
        _timerLabel = MakeLabel(160, 4, 12, HorizontalAlignment.Center);
        _bannerLabel = MakeLabel(160, 70, 20, HorizontalAlignment.Center);
        _bannerLabel.AddThemeColorOverride("font_color", new Color(1, 0.85f, 0.2f));
        foreach (var l in new[] { _p1Label, _p2Label, _timerLabel, _bannerLabel })
            layer.AddChild(l);
    }

    private void BuildStage()
    {
        foreach (var rect in Platforms)
        {
            var body = new StaticBody2D();
            var shape = new CollisionShape2D
            {
                Shape = new RectangleShape2D { Size = rect.Size },
                Position = rect.Position + rect.Size / 2,
            };
            body.AddChild(shape);
            AddChild(body);
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!Training)
        {
            _rules.Update(delta * 1000);
            UpdateHud();
            if (_rules.State != MatchState.Active)
            {
                if (!_matchEnded)
                {
                    _matchEnded = true;
                    _bannerLabel.Text = _rules.State == MatchState.P1Win ? "PLAYER 1 WINS!" : "PLAYER 2 WINS!";
                    _p1.SetPhysicsProcess(false);
                    _p2.SetPhysicsProcess(false);
                }
                if (Input.IsPhysicalKeyPressed(Key.K))
                    GetTree().ChangeSceneToFile("res://scenes/main.tscn");
                return;
            }
        }
        foreach (var f in new[] { _p1, _p2 })
        {
            if (!BlastZone.HasPoint(f.Position))
            {
                if (Training)
                    f.Respawn(f == _p1 ? new Vector2(110, 110) : new Vector2(210, 110), 0);
                else
                    _rules.CheckRingOut(f, (x, y) => !BlastZone.HasPoint(new Vector2(x, y)),
                        f == _p1 ? new Vector2(110, 110) : new Vector2(210, 110));
            }
        }
    }

    private void UpdateHud()
    {
        _p1Label.Text = $"P1  {(int)_p1.Damage}%   x{_p1.Stocks}";
        _p2Label.Text = $"x{_p2.Stocks}   {(int)_p2.Damage}%  P2";
        var t = Math.Max(0, (int)_rules.MatchTimer);
        _timerLabel.Text = $"{t / 60:D2}:{t % 60:D2}";
    }
}
