using Godot;
using System;
using System.Threading.Tasks;
using UnnamedFightingGame;

public partial class ResponsiveSmoke : Node
{
    private int _checks;
    private void Check(bool ok,string text) { if(!ok)throw new Exception(text); _checks++; GD.Print("PASS: "+text); }
    private async Task DrawFrames()
    {
        for(int i=0;i<5;i++)await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
        await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
    }
    public override async void _Ready()
    {
        ProcessMode=ProcessModeEnum.Always;
        try
        {
            var arena=GD.Load<PackedScene>("res://scenes/arena.tscn").Instantiate();AddChild(arena);
            var settings=arena.GetNode<TrainingSettings>("HUD/TrainingSettings");
            var soldier=arena.GetNode<Soldier>("Soldier");
            var stage=arena.GetNode<StagePlatform>("Platform");
            var worldPosition=stage.Position;
            foreach(var size in new[]{new Vector2I(960,540),new(1400,600),new(800,800),new(360,640),new(640,360),new(1280,720)})
            {
                GetWindow().Size=size;await DrawFrames();
                var bounds=GetViewport().GetVisibleRect();
                foreach(string name in new[]{"Title","Subtitle","State","Controls","SoldierPercentage"})
                {
                    var control=arena.GetNode<Control>("HUD/"+name);
                    Check(bounds.Encloses(control.GetGlobalRect()),$"{size}: {name} stays inside viewport");
                }
                var controls=arena.GetNode<Control>("HUD/Controls");
                Check(Math.Abs(controls.Position.Y-(bounds.Size.Y-28))<1,"Controls follow bottom edge");
                var state=arena.GetNode<Control>("HUD/State");
                Check(Math.Abs(state.GetGlobalRect().End.X-(bounds.Size.X-40))<1,"State follows right edge");
                Check(stage.Position==worldPosition && soldier.Visual.Scale.Abs()==Vector2.One,"Resize preserves world and body-part scale");
                Check(GetWindow().ContentScaleStretch==((size.X<960||size.Y<540)?Window.ContentScaleStretchEnum.Fractional:Window.ContentScaleStretchEnum.Integer),"Small windows fit; large windows retain integer scaling");
                settings.SetOpen(true);await DrawFrames();
                foreach(Node child in settings.GetChildren())if(child is Control control)
                    Check(bounds.Encloses(control.GetGlobalRect()),$"{size}: settings control stays onscreen");
                Check(GetViewport().GetTexture().GetImage().SavePng($"../artifacts/responsive-{size.X}x{size.Y}.png")==Error.Ok,"Save responsive layout screenshot");
                // Leave paused for the next resize to exercise layout while settings are open.
            }
            settings.SetOpen(false);
            GD.Print($"RESPONSIVE RESULT: {_checks} checks; 0 failures");GetTree().Quit();
        }
        catch(Exception ex){GD.PushError(ex.ToString());GetTree().Quit(1);}
    }
}
