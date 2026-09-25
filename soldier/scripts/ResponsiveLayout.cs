using Godot;

namespace UnnamedFightingGame;

/// <summary>Reflows screen-space UI without changing world geometry or sprite scale.</summary>
public partial class ResponsiveLayout : Node
{
    private Window _window = null!;
    private Viewport _viewport = null!;
    private CanvasLayer _hud = null!;
    private Vector2I _lastWindowSize;
    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        _window = GetWindow();
        _viewport = GetViewport();
        _hud = GetParent().GetNode<CanvasLayer>("HUD");
        _window.SizeChanged += Resize;
        if (_viewport != _window) _viewport.SizeChanged += Resize;
        Resize();
    }

    public override void _Process(double delta)
    {
        // An OS resize can keep the same logical viewport size (e.g. 640x360 to 1280x720).
        if (_window.Size != _lastWindowSize) Resize();
    }

    private void Resize()
    {
        _lastWindowSize = _window.Size;
        // Integer scaling cannot shrink below the source resolution without clipping.
        var mode = _window.Size.X < 960 || _window.Size.Y < 540
            ? Window.ContentScaleStretchEnum.Fractional : Window.ContentScaleStretchEnum.Integer;
        if (_window.ContentScaleStretch != mode) _window.ContentScaleStretch = mode;
        Vector2 size = _viewport.GetVisibleRect().Size;
        const float margin = 40;
        void Place(string name, Vector2 position, Vector2 extent)
        {
            var control = _hud.GetNode<Control>(name);
            control.Position = position.Round();
            control.Size = extent.Round();
        }
        Place("Title",new(margin,30),new(size.X-2*margin-180,30));
        Place("Subtitle",new(margin+1,61),new(size.X-2*margin-180,24));
        Place("SoldierPercentage",new(margin,96),new(240,32));
        Place("State",new(size.X-margin-180,41),new(180,25));
        var controls = _hud.GetNode<Label>("Controls");
        controls.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        controls.Text = "A / D or ARROWS  Move     SPACE / W / UP  Jump / Double jump     J  Attack     R  Reset";
        Place("Controls",new(margin,size.Y-28),new(size.X-2*margin,24));
        _hud.GetNode<TrainingSettings>("TrainingSettings").Relayout(size);
    }

    public override void _ExitTree()
    {
        _window.SizeChanged -= Resize;
        if (_viewport != _window) _viewport.SizeChanged -= Resize;
    }
}
