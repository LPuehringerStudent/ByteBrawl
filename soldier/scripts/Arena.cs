using Godot;
using System;
using System.Linq;

namespace UnnamedFightingGame;

public partial class Arena : Node2D
{
    private Soldier _soldier = null!;
    private Label _stateLabel = null!;
    private Label _damageLabel = null!;

    public override void _Ready()
    {
        _soldier = GetNode<Soldier>("Soldier");
        _soldier.SetSpawn(GetNode<StagePlatform>("Platform").Spawn);
        var dummy=GetNode<TrainingDummy>("Dummy");
        var stage=GetNode<StagePlatform>("Platform");
        dummy.AddCollisionExceptionWith(_soldier);
        dummy.SetHome(new Vector2(620,stage.SurfaceY(620)));
        _stateLabel = GetNode<Label>("HUD/State");
        AddChild(new ArenaCamera { Name="Camera",Target=_soldier });
        var hud=GetNode<CanvasLayer>("HUD");
        _damageLabel=new Label { Name="SoldierPercentage",Position=new Vector2(40,96),Size=new Vector2(240,32),
            MouseFilter=Control.MouseFilterEnum.Ignore };
        _damageLabel.AddThemeFontSizeOverride("font_size",22);hud.AddChild(_damageLabel);
        hud.AddChild(new DummyIndicator { Name="DummyIndicator",Target=dummy });
        AddChild(new ResponsiveLayout { Name="ResponsiveLayout" });
        string[] args = OS.GetCmdlineUserArgs();
        if (args.Contains("--capture")) Capture(args);
    }

    public override void _Process(double delta)
    {
        _stateLabel.Text = _soldier.MotionState.ToUpperInvariant();
        _damageLabel.Text=$"SOLDIER  {_soldier.Damage.Percentage:0}%";
        _damageLabel.AddThemeColorOverride("font_color",DamageState.Tint(_soldier.Damage.Percentage));
    }

    private async void Capture(string[] args)
    {
        try
        {
            await ToSignal(GetTree().CreateTimer(0.4), SceneTreeTimer.SignalName.Timeout);
            string path = Argument(args, "--capture", "user://arena.png");
            string pose = Argument(args, "--pose", args.Contains("--run-pose") ? "run" : "idle");
            int frame = int.Parse(Argument(args, "--frame", "0"));
            _soldier.SetPhysicsProcess(false);
            _soldier.Visual.SetPose(pose, frame);
            _stateLabel.Text = pose.ToUpperInvariant();
            SetProcess(false);
            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            Error error = GetViewport().GetTexture().GetImage().SavePng(path);
            GD.Print($"Screenshot: {path}; result={error}");
            GetTree().Quit(error == Error.Ok ? 0 : 1);
        }
        catch (Exception ex) { GD.PushError(ex.ToString()); GetTree().Quit(1); }
    }

    private static string Argument(string[] args, string name, string fallback)
    {
        int index = Array.IndexOf(args, name);
        return index >= 0 && index + 1 < args.Length ? args[index + 1] : fallback;
    }
}
