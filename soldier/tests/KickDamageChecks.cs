using Godot;
using System;
using System.Threading.Tasks;
using UnnamedFightingGame;
public partial class TrainingSmoke
{
    private async Task VerifyKickDamage()
    {
        foreach(bool left in new[]{false,true})
        {
            await Setup(left?670:570);
            if(left){Input.ActionPress("move_left");await Frames(1);Input.ActionRelease("move_left");}
            await Press();await Frames(1);await Press();
            for(int i=0;i<20&&_player.AttackNumber==1;i++)await Frames(1);
            await Press();
            for(int i=0;i<25&&_player.AttackNumber==2;i++)await Frames(1);
            float percent=_dummy.Damage.Percentage;int count=_dummy.Damage.HitCount;
            Check(_player.AttackNumber==3,"Damage test reaches third attack");
            await Frames(18);
            Check(_dummy.Damage.Percentage==percent+4&&_dummy.Damage.HitCount==count+1,"Kick adds exactly four percent once across three active frames in either facing");
            Check(_dummy.Damage.LastDamage==4,"Kick damage display receives four percent");
        }
        await Setup(380);await Press();await Frames(1);await Press();
        for(int i=0;i<20&&_player.AttackNumber==1;i++)await Frames(1);
        await Press();await Frames(40);
        Check(_dummy.Damage.Percentage==0,"Kick and its blue effects miss outside foot reach");
        await Setup();
    }
}
