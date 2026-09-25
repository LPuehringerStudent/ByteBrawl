using Godot;

namespace UnnamedFightingGame;

public partial class TrainingSettings : Control
{
    public string SettingsPath { get; set; } = "user://training.cfg";
    public bool IsOpen => _panel.Visible;
    public TrainingDummy Dummy { get; private set; } = null!;
    private PanelContainer _panel = null!;
    private CheckButton _knockback = null!, _autoReturn = null!;
    private bool _previousPause;
    private Button _open = null!;

    public override void _Ready()
    {
        ProcessMode=ProcessModeEnum.Always;
        MouseFilter=MouseFilterEnum.Ignore;
        Dummy=GetNode<TrainingDummy>("../../Dummy");
        _open=new Button { Text="Settings  [Esc]",Position=new(786,78),Size=new(134,28),FocusMode=FocusModeEnum.None };
        AddChild(_open);_open.Pressed+=()=>SetOpen(!IsOpen);
        _panel=new PanelContainer { Position=new(660,116),Size=new(260,220),Visible=false };
        AddChild(_panel);
        var margin=new MarginContainer();_panel.AddChild(margin);
        foreach(string side in new[]{"left","right","top","bottom"})margin.AddThemeConstantOverride("margin_"+side,12);
        var column=new VBoxContainer();column.AddThemeConstantOverride("separation",8);margin.AddChild(column);
        column.AddChild(new Label { Text="TRAINING SETTINGS" });
        _knockback=new CheckButton { Text="Knockback",Name="Knockback" };column.AddChild(_knockback);
        _autoReturn=new CheckButton { Text="Auto-reset position",Name="AutoResetPosition" };column.AddChild(_autoReturn);
        var hint=new Label { Text="Return after 2 seconds without a hit." };
        hint.AddThemeFontSizeOverride("font_size",11);column.AddChild(hint);
        var reset=new Button { Text="Reset Dummy",Name="ResetDummy" };column.AddChild(reset);reset.Pressed+=Dummy.ResetDummy;
        var close=new Button { Text="Back to training",Name="Close" };column.AddChild(close);close.Pressed+=()=>SetOpen(false);
        var config=new ConfigFile();config.Load(SettingsPath);
        Apply(config.GetValue("dummy","knockback",false).AsBool(),config.GetValue("dummy","auto_reset_position",true).AsBool());
        _knockback.Toggled+=value=>SetKnockback(value);
        _autoReturn.Toggled+=value=>SetAutoResetPosition(value);
    }

    public void Relayout(Vector2 viewportSize)
    {
        Size = viewportSize;
        _open.Position = new Vector2(viewportSize.X - 40 - _open.Size.X, 78).Round();
        _panel.Size = new Vector2(260,220);
        _panel.Position = new Vector2(viewportSize.X - 40 - _panel.Size.X,
            Mathf.Min(116, viewportSize.Y - 40 - _panel.Size.Y)).Round();
    }

    private void Apply(bool knockback,bool autoReturn)
    {
        Dummy.KnockbackEnabled=knockback;Dummy.AutoResetPosition=autoReturn;
        _knockback.SetPressedNoSignal(knockback);_autoReturn.SetPressedNoSignal(autoReturn);
    }

    private void Save()
    {
        var config=new ConfigFile();config.SetValue("dummy","knockback",Dummy.KnockbackEnabled);
        config.SetValue("dummy","auto_reset_position",Dummy.AutoResetPosition);
        if(config.Save(SettingsPath)!=Error.Ok) GD.PushWarning("Training settings could not be saved.");
    }

    public void SetKnockback(bool enabled) { Apply(enabled,Dummy.AutoResetPosition);Save(); }
    public void SetAutoResetPosition(bool enabled) { Apply(Dummy.KnockbackEnabled,enabled);Save(); }
    public void SetOpen(bool open)
    {
        if(IsOpen==open) return;
        if(open) { _previousPause=GetTree().Paused;GetTree().Paused=true; }
        else GetTree().Paused=_previousPause;
        _panel.Visible=open;
    }

    public override void _UnhandledInput(InputEvent input)
    {
        if(input is InputEventKey { Pressed:true,Echo:false,Keycode:Key.Escape })
        { SetOpen(!IsOpen);GetViewport().SetInputAsHandled(); }
    }

    public override void _ExitTree() { if(_panel!=null&&IsOpen)GetTree().Paused=_previousPause; }
}
