using Godot;
using System;
using System.Linq;
using System.Threading.Tasks;
using ByteBrawl.Combat;
using UnnamedFightingGame;

public partial class LimbPhysicsSmoke : Node
{
    private int _checks;
    private void Check(bool value,string message) { if(!value)throw new Exception(message);_checks++;GD.Print("PASS: "+message); }
    private async Task Frames(int count=2)
    { for(int i=0;i<count;i++){await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);} }
    public override async void _Ready()
    {
        GetWindow().Size=new Vector2I(960,540);
        try
        {
            Check(FighterPhysics.Locomotion(0,1,235,true)==235,"ByteBrawl immediate ground acceleration");
            Check(FighterPhysics.Locomotion(235,0,235,true)==0,"ByteBrawl immediate ground stop");
            Check(FighterPhysics.Locomotion(320,0,235,false)==320,"ByteBrawl neutral air drift");
            Check(FighterPhysics.Locomotion(320,-1,235,false)==-235,"ByteBrawl directional air control");
            Check(FighterPhysics.GravityStep(790,1350,1.0/60,800)==800,"Soldier terminal fall speed preserved");
            Check(FighterPhysics.Knockback(150,3,50,Vector2.Right,-1)==new Vector2(-300,0),"Shared pre-hit knockback rule mirrors");
            var arena=GD.Load<PackedScene>("res://scenes/arena.tscn").Instantiate();AddChild(arena);await Frames(5);
            var player=arena.GetNode<Soldier>("Soldier");var rig=player.Hurtboxes;
            Check(rig.Parts.Count==15&&player.Visual.PartCount==16,"Fifteen hurtboxes; sixteen unchanged sprite parts");
            Check(!rig.Parts.ContainsKey("backpack"),"Backpack excluded as in ByteBrawl");
            var terrain=(CapsuleShape2D)player.GetNode<CollisionShape2D>("CollisionShape2D").Shape;
            Check(terrain.Radius==12&&terrain.Height==108,"Terrain collision dimensions preserved");
            player.SetPhysicsProcess(false);
            foreach(float facing in new[]{1f,-1f})
            foreach(var (clip,info) in SoldierVisual.Clips)
            for(int frame=0;frame<info.Count;frame++)
            {
                player.Visual.Scale=new Vector2(facing,1);player.Visual.SetPose(clip,frame);
                foreach(var name in new[]{"near_thigh","far_thigh","near_shin","far_shin"})
                {
                    var part=rig.Parts[name];
                    Check(part.Group=="Leg"&&part.Capsule.Radius>0&&part.Capsule.Height>=2*part.Capsule.Radius
                        && part.GlobalPosition.DistanceTo(player.Visual.ToGlobal(part.Position))<.001,
                        $"{clip}/{frame}/{facing}: {name} follows pose/facing with valid capsule");
                }
            }
            player.Visual.Scale=Vector2.One;player.Visual.SetPose("kick",3);await Frames();
            foreach(string name in new[]{"near_thigh","far_thigh","near_shin","far_shin"})
            {
                var part=rig.Parts[name];
                var hits=player.GetWorld2D().DirectSpaceState.IntersectPoint(new PhysicsPointQueryParameters2D {
                    Position=part.GlobalPosition,CollisionMask=8,CollideWithAreas=true,CollideWithBodies=false },128);
                Check(hits.Any(h=>h["collider"].AsGodotObject()==part),name+" participates in real physics queries");
            }
            // Damage is deduplicated by fighter even when one circle overlaps several limb areas.
            player.SetPhysicsProcess(true);player.Reset();await Frames(3);
            var opponent=GD.Load<PackedScene>("res://scenes/soldier.tscn").Instantiate<Soldier>();arena.AddChild(opponent);
            opponent.SetPhysicsProcess(false);opponent.CollisionLayer=0;opponent.CollisionMask=0;
            opponent.Position=player.Position+new Vector2(32,0);await Frames();
            Input.ActionPress("attack");await Frames(1);Input.ActionRelease("attack");
            player.SetPhysicsProcess(false);player.Visual.SetPose("attack",1);
            var socket=player.Visual.GetNode<Marker2D>("FarWeaponSocket");
            foreach(string name in new[]{"near_thigh","far_shin"})
                opponent.Hurtboxes.Parts[name].GlobalPosition=socket.GlobalPosition;
            await Frames(3);
            Check(opponent.Damage.Percentage==2&&opponent.Damage.HitCount==1,"Two simultaneous limb contacts cause one 2% jab");
            Check(player.Damage.Percentage==0,"Fighter excludes its own fifteen hurtboxes");
            rig.ShowDebug=true;player.Visual.SetPose("kick",4);
            if(OS.GetCmdlineUserArgs().Contains("--capture-limbs"))
            {
                await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
                Check(GetViewport().GetTexture().GetImage().SavePng("../artifacts/byte-soldier-hurtboxes.png")==Error.Ok,"Save debug hurtbox capture");
            }
            GD.Print($"LIMB PHYSICS RESULT: {_checks} checks; 0 failures");GetTree().Quit();
        }
        catch(Exception ex){GD.PushError(ex.ToString());GetTree().Quit(1);}
    }
}
