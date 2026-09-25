using Godot;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using UnnamedFightingGame;

public partial class TrainingSmoke : Node
{
    private int _checks,_failures;
    private Soldier _player=null!;
    private TrainingDummy _dummy=null!;
    private TrainingSettings _settings=null!;
    private const string TestSettings="user://training-smoke.cfg";
    private void Check(bool ok,string message)
    {
        _checks++;
        if(ok)GD.Print("PASS: "+message);else{_failures++;GD.PushError(message);}
    }
    private async Task Frames(int count)
    {
        for(int i=0;i<count;i++)
        {
            await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
        }
    }
    private async Task Setup(float x=570)
    {
        foreach(string action in new[]{"attack","jump","move_left","move_right"})Input.ActionRelease(action);
        _player.Reset();_player.Position=new(x,334);
        _dummy.KnockbackEnabled=false;_dummy.AutoResetPosition=true;_dummy.ResetDummy();
        await Frames(3);
    }
    private async Task Press()
    {
        Input.ActionPress("attack");await Frames(1);Input.ActionRelease("attack");
    }
    private async Task Capture(string name)
    {
        if(!OS.GetCmdlineUserArgs().Contains("--capture-training"))return;
        await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
        Check(GetViewport().GetTexture().GetImage().SavePng("../artifacts/"+name+".png")==Error.Ok,"Saved "+name+" visual review");
    }
    public override async void _Ready()
    {
        GetWindow().Size = new Vector2I(960,540); // Explicit baseline; headless defaults to 64x64.
        try
        {
            if(FileAccess.FileExists(TestSettings))DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(TestSettings));
            var arena=GD.Load<PackedScene>("res://scenes/arena.tscn").Instantiate();
            _settings=arena.GetNode<TrainingSettings>("HUD/TrainingSettings");_settings.SettingsPath=TestSettings;
            AddChild(arena);
            _player=arena.GetNode<Soldier>("Soldier");_dummy=arena.GetNode<TrainingDummy>("Dummy");
            await Frames(5);
            Check(!_dummy.KnockbackEnabled&&_dummy.AutoResetPosition,"Defaults: stationary target with automatic return");
            Check(_dummy.Damage.Percentage==0&&_dummy.IsOnFloor(),"Dummy starts at 0% damage on the deck");
            Vector2 home=_dummy.Position;await Frames(60);
            Check(_dummy.Position.DistanceTo(home)<.1,"Dummy does not move on its own");
            using(var image=_dummy.GetNode<Sprite2D>("Artwork").Texture.GetImage())
            {
                image.Convert(Image.Format.Rgba8);
                using var report=JsonDocument.Parse(System.IO.File.ReadAllText(System.IO.Path.Combine(ProjectSettings.GlobalizePath("res://"),"..","art","PIXELLOID_REPORT.json")));
                var entry=report.RootElement.GetProperty("files").EnumerateArray().Single(e=>e.GetProperty("file").GetString()=="training/dummy.png");
                Check(image.GetWidth()==80&&image.GetHeight()==120&&
                    Convert.ToHexString(SHA256.HashData(image.GetData())).ToLowerInvariant()==entry.GetProperty("processedRgbaSha256").GetString(),"Dummy imports match the 80x120 Pixelloid pixels exactly");
            }
            await Setup();await Press();
            Check(_dummy.Damage.Percentage==0,"Jab startup does not damage the dummy");
            await Frames(2);
            Check(_dummy.Damage.Percentage==2&&_dummy.Damage.LastDamage==2&&_dummy.Damage.HitCount==1,"Jab impact deals exactly 2% damage");
            Check(_dummy.GetNode("DamageNumbers").GetChildCount()==1,"A damage number appears on the hit");
            await Capture("training-hit");
            await Frames(10);
            Check(_dummy.Damage.Percentage==2&&_dummy.Damage.HitCount==1,"Several overlapping impact ticks deal damage only once");
            Check(_dummy.Position.DistanceTo(home)<.1,"Knockback off keeps the dummy stationary when hit");

            await Setup();await Press();await Frames(2);await Press();await Frames(32);
            Check(_dummy.Damage.Percentage==5&&_dummy.Damage.HitCount==2&&_dummy.Damage.LastDamage==3,"Jab-cross combo deals 2% plus 3%, once per punch");
            await Setup(480);await Press();await Frames(16);
            Check(_dummy.Damage.Percentage==0,"A distant punch misses");
            await Setup(670);await Press();await Frames(16);
            Check(_dummy.Damage.Percentage==0,"Punching away from the dummy misses");
            Input.ActionPress("move_left");await Press();Input.ActionRelease("move_left");await Frames(18);
            Check(_dummy.Damage.Percentage==3,"Left-facing cross uses its mirrored fist hit position");
            await Setup(480);await Press();await Frames(7);_player.Position=new(570,334);await Frames(5);
            Check(_dummy.Damage.Percentage==0,"Entering range during recovery cannot hit");
            await Setup();_player.Position=new(570,310);_player.Velocity=new(0,200);await Frames(1);
            await Press();await Frames(10);
            Check(_dummy.Damage.Percentage==2,"Falling punch can hit and land normally");
            await Setup();_player.Position=new(570,150);await Frames(1);await Press();await Frames(12);
            Check(_dummy.Damage.Percentage==0,"An aerial punch above the target misses");
            await Setup();Input.ActionPress("attack");await Frames(60);Input.ActionRelease("attack");
            Check(_dummy.Damage.Percentage==2&&_dummy.Damage.HitCount==1,"Holding J cannot repeatedly damage the target");
            await Setup();await Press();_player.Reset();await Frames(15);
            Check(_dummy.Damage.Percentage==0,"Reset before impact cancels damage");

            await Setup();_dummy.KnockbackEnabled=true;
            await Press();await Frames(6);
            Check(_dummy.Position.X>home.X+2&&_dummy.Position.Y<home.Y,"Enabled knockback pushes and lifts the dummy away");
            await Frames(120);
            Check(_dummy.Position.DistanceTo(home)<.1&&_dummy.Damage.Percentage==2,"Automatic return restores position after inactivity without clearing damage");
            _dummy.AutoResetPosition=false;_dummy.ReceiveHit(AttackHit.Jab,-1);await Frames(135);
            Check(_dummy.Position.X<home.X-3&&_dummy.Damage.Percentage==4,"Automatic return off leaves the displaced dummy in place");
            _dummy.AutoResetPosition=true;await Frames(2);
            Check(_dummy.Position.DistanceTo(home)<.1,"Enabling return restores an overdue displaced dummy");
            _dummy.ReceiveHit(AttackHit.Jab,1);await Frames(70);
            _dummy.ReceiveHit(AttackHit.Jab,1);await Frames(70);
            Check(_dummy.Position.X>home.X+2,"New hits restart the two-second return timer");
            _dummy.AutoResetPosition=false;_dummy.Position=new(800,860);await Frames(2);
            Check(_dummy.Position.DistanceTo(home)<.1&&_dummy.Damage.Percentage==0,"Off-stage rescue works even with automatic return disabled");
            _dummy.KnockbackEnabled=false;
            _dummy.ReceiveHit(AttackHit.Jab with { Percentage=99 },1);
            Check(_dummy.ReceiveHit(AttackHit.Cross,1)==3&&_dummy.Damage.Percentage==102&&_dummy.Damage.LastDamage==3,"Damage accumulates past 100% without truncating the hit");
            await Frames(62);
            Check(_dummy.Damage.Percentage==102,"Percentage does not automatically heal or cause death at 100%");

            _settings.SetKnockback(true);_settings.SetAutoResetPosition(false);
            var saved=new ConfigFile();saved.Load(TestSettings);
            Check(saved.GetValue("dummy","knockback").AsBool()&&!saved.GetValue("dummy","auto_reset_position").AsBool(),"Both toggles persist independently");
            _settings.SetOpen(true);
            Check(GetTree().Paused&&_settings.IsOpen,"Opening settings pauses gameplay");
            await Capture("training-settings");
            _dummy.ReceiveHit(AttackHit.Jab,1);_dummy.Position+=new Vector2(30,0);
            ((Button)_settings.FindChild("ResetDummy",true,false)).EmitSignal(Button.SignalName.Pressed);
            Check(_dummy.Position==home&&_dummy.Damage.Percentage==0&&_dummy.Damage.LastDamage==0,"Reset Dummy button immediately restores position and percentage while paused");
            var knockback=(CheckButton)_settings.FindChild("Knockback",true,false);
            knockback.ButtonPressed=false;
            Check(!_dummy.KnockbackEnabled&&!_dummy.AutoResetPosition,"Knockback checkbox does not change automatic return");
            ((CheckButton)_settings.FindChild("AutoResetPosition",true,false)).ButtonPressed=true;
            Check(_dummy.AutoResetPosition&&!_dummy.KnockbackEnabled,"Automatic return checkbox does not change knockback");
            _settings.SetOpen(false);
            Check(!GetTree().Paused,"Closing settings resumes gameplay");
            await Frames(2);
            Check(_dummy.GetNode("DamageNumbers").GetChildCount()==0,"Manual reset clears floating damage feedback");
            await VerifyKickDamage();
            await VerifyPercentageAndCamera();
            GD.Print($"TRAINING RESULT: {_checks} checks; {_failures} failures");
            DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(TestSettings));
            GetTree().Quit(_failures==0?0:1);
        }
        catch(Exception ex) { GD.PushError(ex.ToString());GetTree().Paused=false;GetTree().Quit(1); }
    }
}
