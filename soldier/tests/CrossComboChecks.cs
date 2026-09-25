using Godot;
using System;
using System.Linq;
using System.Text.Json;
using System.Security.Cryptography;
using System.Threading.Tasks;
using UnnamedFightingGame;

public partial class MovementSmoke
{
    private void VerifyComboBoundaries()
    {
        Check(SoldierVisual.AttackSeconds==.2&&SoldierVisual.CrossSeconds==.3,"Punch durations are 200/300 ms at 1.5x speed");
        var combo = new FistCombo();
        Check(combo.Advance(0,true)&&combo.AttackNumber==1,"Fresh press starts punch one");
        combo.Advance(.05,true);combo.Advance(.05,true);
        Check(combo.Queued&&combo.AttackNumber==1,"Several first-punch presses buffer only one cross");
        Check(combo.Advance(.1,false)&&combo.AttackNumber==2&&!combo.Queued,"Buffered cross starts at the first-punch boundary");
        combo.Advance(.2,true);combo.Advance(.099999,true);
        Check(combo.AttackNumber==2&&combo.Queued,"Cross buffers one kick and holds its full 300 ms");
        combo.Advance(.000001,true);
        Check(combo.AttackNumber==3&&!combo.Queued,"Buffered kick starts at cross completion");
        combo.Advance(.3,true);
        Check(!combo.IsAttacking&&!combo.Queued,"Kick completion enters hard cooldown");
        combo.Advance(.149999,true);
        Check(!combo.IsAttacking,"Press just before cooldown expiry is discarded");
        Check(combo.Advance(.000001,true)&&combo.AttackNumber==1,"Fresh press at 150 ms cooldown boundary restarts punch one");
        combo.Reset();combo.Advance(0,true);combo.Advance(.2,false);
        Check(!combo.IsAttacking,"Single jab returns to movement during grace");
        combo.Advance(.199999,true);
        Check(combo.AttackNumber==2,"Second press just inside 200 ms grace selects cross");
        combo.Reset();combo.Advance(0,true);combo.Advance(.2,false);combo.Advance(.2,true);
        Check(combo.AttackNumber==1,"Press at grace expiry starts punch one");
        combo.Reset();combo.Advance(0,true);combo.Advance(1,true);
        Check(combo.AttackNumber==1,"Late press starts punch one even after a long update");
        combo.Reset();combo.Advance(0,true);combo.Advance(.1,true);combo.Advance(.1,false);
        combo.Advance(.3,false);combo.Advance(.15,false);
        Check(!combo.IsAttacking&&!combo.Queued,"No third attack without a fresh press during cross or grace");
        combo.Reset();combo.Advance(0,true);combo.Advance(.2,false);combo.Reset();combo.Advance(0,true);
        Check(combo.AttackNumber==1,"Reset clears the grace window");
        combo.Advance(.1,true);combo.Advance(.1,false);combo.Reset();combo.Advance(0,true);
        Check(combo.AttackNumber==1&&!combo.Queued,"Reset during cross returns to punch one");
        combo.Advance(.1,true);combo.Advance(.1,false);combo.Advance(.3,false);combo.Reset();
        Check(combo.Advance(0,true)&&combo.AttackNumber==1,"Reset clears combo cooldown");
    }

    private async Task VerifyCrossCombo()
    {
        VerifyComboBoundaries();
        await CleanAttackStart();
        Input.ActionPress("attack");await Frames(1);Input.ActionRelease("attack");await Frames(2);
        Input.ActionPress("attack");await Frames(1);Input.ActionRelease("attack");
        Input.ActionPress("move_right");
        while(_player.AttackNumber==1)await Frames(1);
        Check(_player.MotionState=="cross"&&_player.Visual.AtlasFrame==29,"Buffered cross begins on its load frame");
        float x=_player.Position.X;
        var seen=new System.Collections.Generic.HashSet<int>();
        bool synchronized=true,queued=false;
        // Without a third press, cross ends in a follow-up grace window.
        for(int i=0;i<26;i++)
        {

            if(_player.AttackNumber==2)
            {
                int frame=_player.Visual.AtlasFrame;
                seen.Add(frame);
                synchronized &= _player.Visual.AttackEffect.Visible&&_player.Visual.AttackEffect.Hframes==8&&
                    _player.Visual.AttackEffect.Frame==frame-29;
            }
            queued |= _player.AttackQueued;
            await Frames(1);
        }
        Input.ActionRelease("attack");Input.ActionRelease("move_right");
        Check(Enumerable.Range(29,8).All(seen.Contains)&&synchronized,"All eight cross frames synchronize their effects");
        Check(!queued&&!_player.IsAttacking,"Cross does not automatically start a kick");
        Check(_player.Position.X>x+45,"Cross keeps walking momentum");
        await Frames(8);
        Input.ActionPress("attack");await Frames(1);Input.ActionRelease("attack");
        Check(_player.AttackNumber==1&&_player.Visual.AttackEffect.Hframes==5,"New combo restores jab and five-frame effect sheet");

        await CleanAttackStart();
        Input.ActionPress("attack");await Frames(1);await Frames(60);
        Check(!_player.IsAttacking&&!_player.AttackQueued,"Holding J through an entire combo window never repeats");
        Input.ActionRelease("attack");

        await CleanAttackStart();
        Input.ActionPress("jump");Input.ActionPress("attack");await Frames(1);Input.ActionRelease("attack");await Frames(2);
        Input.ActionPress("attack");await Frames(1);Input.ActionRelease("attack");
        while(_player.AttackNumber==1)await Frames(1);
        Check(_player.AttackNumber==2&&!_player.IsOnFloor(),"Cross begins in the air without cancelling the jump");
        Input.ActionRelease("jump");await Frames(1);
        Check(_player.IsAttacking&&_player.Velocity.Y>=-170,"Short-jump release works during cross");
        _player.Position=new Vector2(480,310);_player.Velocity=new Vector2(100,200);
        await Frames(7);
        Check(_player.IsOnFloor()&&_player.AttackNumber==2&&_player.Visual.AtlasFrame>29,"Landing preserves cross frame progress");
        Input.ActionPress("attack");await Frames(1);Input.ActionRelease("attack");
        _player.Position=new Vector2(480,860);await Frames(1);
        Check(!_player.IsAttacking&&!_player.AttackQueued&&!_player.Visual.AttackEffect.Visible,"Respawn clears cross and effects");
        Input.ActionPress("attack");await Frames(1);Input.ActionRelease("attack");
        Check(_player.AttackNumber==1,"Respawn restarts the combo from punch one");

        await CleanAttackStart();
        Input.ActionPress("move_left");Input.ActionPress("attack");await Frames(1);Input.ActionRelease("attack");await Frames(2);
        Input.ActionPress("attack");await Frames(1);Input.ActionRelease("attack");
        while(_player.AttackNumber==1)await Frames(1);
        Check(_player.Visual.AttackEffect.GlobalTransform.X.X<0,"Left-facing cross mirrors body and effect together");
        Input.ActionRelease("move_left");Input.ActionPress("move_right");await Frames(2);
        Check(_player.Facing==-1,"Cross facing stays fixed while direction input changes");
        await CleanAttackStart();

        int[] ticks={2,2,2,2,2,4,2,2};
        for(int i=0;i<8;i++)
        {
            _player.Visual.SetPose("cross",i);
            _player.Visual.Advance("cross",(ticks[i]-.01)/60,1);
            Check(_player.Visual.AtlasFrame==29+i,$"Cross {i} holds accelerated duration");
            _player.Visual.Advance("cross",.02/60,1);
            Check(_player.Visual.AtlasFrame==29+Math.Min(i+1,7),$"Cross {i} advances without looping");
        }
        using var effect=_player.Visual.AttackEffect.Texture.GetImage();effect.Convert(Image.Format.Rgba8);
        using var report=JsonDocument.Parse(System.IO.File.ReadAllText(System.IO.Path.Combine(ProjectSettings.GlobalizePath("res://"),"..","art","PIXELLOID_REPORT.json")));
        var entry=report.RootElement.GetProperty("files").EnumerateArray().Single(e=>e.GetProperty("file").GetString()=="effects/cross.png");
        Check(Convert.ToHexString(SHA256.HashData(effect.GetData())).ToLowerInvariant()==entry.GetProperty("processedRgbaSha256").GetString(),
            "Cross effects match Pixelloid pixels exactly");
        await CleanAttackStart();
    }
}
