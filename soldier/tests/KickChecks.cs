using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using UnnamedFightingGame;

public partial class MovementSmoke
{
    private async Task QueueKick()
    {
        Input.ActionPress("attack");await Frames(1);Input.ActionRelease("attack");await Frames(1);
        Input.ActionPress("attack");await Frames(1);Input.ActionRelease("attack");
        for(int i=0;i<20&&_player.AttackNumber==1;i++)await Frames(1);
        Input.ActionPress("attack");await Frames(1);Input.ActionRelease("attack");
        for(int i=0;i<25&&_player.AttackNumber==2;i++)await Frames(1);
        Check(_player.AttackNumber==3,"Third fresh J starts the kick after cross");
    }
    private async Task VerifyKick()
    {
        Check(SoldierVisual.KickSeconds==SoldierVisual.CrossSeconds,"Kick matches cross duration");
        Check(AttackHit.Jab.Percentage==2 && AttackHit.Cross.Percentage==3 && AttackHit.Kick.Percentage==4 && AttackHit.Kick.Launch(100,1)==AttackHit.Cross.Launch(100,1)
            && AttackHit.Kick.StunSeconds==AttackHit.Cross.StunSeconds,"Damage is 2/3/4; kick retains cross knockback and hitstun");
        var combo=new FistCombo();
        void Second(){combo.Reset();combo.Advance(0,true);combo.Advance(.05,true);combo.Advance(.15,false);}
        Second();combo.Advance(.3,false);combo.Advance(.199999,true);
        Check(combo.AttackNumber==3,"Third press just inside cross grace starts kick");
        Second();combo.Advance(.3,false);combo.Advance(.2,true);
        Check(combo.AttackNumber==1,"Third press at grace expiry restarts jab");
        Second();combo.Advance(.1,true);combo.Advance(.1,true);combo.Advance(.1,false);
        Check(combo.AttackNumber==3&&!combo.Queued,"Cross spam queues exactly one kick");
        combo.Advance(.299999,true);Check(combo.AttackNumber==3&&!combo.Queued,"Kick ignores extra presses");
        combo.Advance(.000001,true);Check(!combo.IsAttacking,"Kick has a hard ending");
        combo.Advance(.149999,true);Check(!combo.IsAttacking,"Cooldown discards early presses");
        combo.Advance(.000001,true);Check(combo.AttackNumber==1,"Fresh press at cooldown end starts new jab");
        combo.Reset();Check(!combo.Queued&&!combo.IsAttacking,"Reset clears three-attack sequence");
        await CleanAttackStart();Input.ActionPress("move_right");await QueueKick();
        float x=_player.Position.X;var seen=new HashSet<int>();bool sync=true;
        for(int i=0;i<18;i++)
        {
            if(_player.AttackNumber==3)
            {
                int frame=_player.Visual.AtlasFrame;seen.Add(frame);
                sync &= _player.Visual.AttackEffect.Frame==frame-45&&_player.Visual.AttackEffect.Hframes==12;
            }
            if(i%2==0)Input.ActionPress("attack");else Input.ActionRelease("attack");
            await Frames(1);
        }
        Input.ActionRelease("attack");Input.ActionRelease("move_right");
        Check(Enumerable.Range(45,12).All(seen.Contains)&&sync,"All twelve kick poses play with synchronized effects");
        Check(_player.Position.X>x+20,"Kick maintains walking momentum");
        await Frames(10);Check(!_player.IsAttacking&&!_player.AttackQueued,"Spam cannot extend combo beyond kick");
        await CleanAttackStart();Input.ActionPress("move_left");await QueueKick();
        Check(_player.Visual.AttackEffect.GlobalTransform.X.X<0&&_player.Visual.KickSocket.GlobalPosition.X<_player.GlobalPosition.X+20,
            "Kick and foot socket mirror with facing");
        _player.ReceiveHit(AttackHit.Jab,1);Check(!_player.IsAttacking&&!_player.Visual.AttackEffect.Visible,"Hitstun interrupts kick and clears effects");
        await CleanAttackStart();Input.ActionPress("jump");await QueueKick();
        Check(!_player.IsOnFloor(),"Kick follows jab and cross during a jump");
        Input.ActionRelease("jump");await Frames(1);Input.ActionPress("jump");await Frames(1);
        Check(_player.Visual.ThrustersVisible&&_player.AttackNumber==3,"Powered second jump works during kick");
        for(int i=0;i<4;i++)
        {
            var outlet=_player.Visual.GetNode<Sprite2D>("Thrusters/Outlet"+i);
            Check(outlet.Visible&&outlet.Position==outlet.Position.Round(),"Kick thruster outlets use integer pose attachments");
        }
        int previous=_player.Visual.AtlasFrame;
        _player.Position=new Vector2(480,310);_player.Velocity=new Vector2(80,200);
        await Frames(6);
        Check(_player.IsOnFloor()&&_player.AttackNumber==3&&_player.Visual.AtlasFrame>previous,"Landing preserves kick progress");
        Check(!_player.Visual.ThrustersVisible,"Landing during kick stops thrusters");
        _player.Position=new Vector2(480,860);await Frames(1);
        Check(!_player.IsAttacking&&!_player.AttackQueued&&!_player.Visual.AttackEffect.Visible,"Respawn clears kick and effects");
        await CleanAttackStart();
        int[] ticks={1,2,1,2,3,2,1,1,1,1,1,2};
        _player.SetPhysicsProcess(false);
        for(int i=0;i<12;i++)
        {
            _player.Visual.SetPose("kick",i);_player.Visual.Advance("kick",(ticks[i]-.01)/60,1);
            Check(_player.Visual.AtlasFrame==45+i,$"Kick pose {i+1} holds its duration");
            _player.Visual.Advance("kick",.02/60,1);
            Check(_player.Visual.AtlasFrame==45+Math.Min(i+1,11),$"Kick pose {i+1} advances without looping");
        }
        using(var image=_player.Visual.AttackEffect.Texture.GetImage())
        using(var report=JsonDocument.Parse(System.IO.File.ReadAllText(System.IO.Path.Combine(ProjectSettings.GlobalizePath("res://"),"..","art","PIXELLOID_REPORT.json"))))
        {
            image.Convert(Image.Format.Rgba8);
            var entry=report.RootElement.GetProperty("files").EnumerateArray().Single(e=>e.GetProperty("file").GetString()=="effects/kick.png");
            Check(Convert.ToHexString(SHA256.HashData(image.GetData())).ToLowerInvariant()==entry.GetProperty("processedRgbaSha256").GetString(),"Imported kick effect equals Pixelloid RGBA");
        }
        if(OS.GetCmdlineUserArgs().Contains("--capture-kick"))
        {
            foreach(int frame in new[]{2,4,8})
            {
                _player.Visual.SetPose("kick",frame);
                await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
                Check(GetViewport().GetTexture().GetImage().SavePng($"../artifacts/kick-game-{frame+1}.png")==Error.Ok,"Capture kick review frame");
            }
        }
        _player.SetPhysicsProcess(true);await CleanAttackStart();
    }
}
