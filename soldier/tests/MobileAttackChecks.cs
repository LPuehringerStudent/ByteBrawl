using Godot;
using System;
using System.Threading.Tasks;

public partial class MovementSmoke
{
    private async Task CleanAttackStart()
    {
        foreach(string action in new[]{"move_left","move_right","jump","attack","reset"})Input.ActionRelease(action);
        _player.Reset();await Frames(3);
    }

    private async Task VerifyMobileAttacks()
    {
        await CleanAttackStart();
        Input.ActionPress("move_right");await Frames(16);
        float speed=_player.Velocity.X,x=_player.Position.X;
        Input.ActionPress("attack");await Frames(1);Input.ActionRelease("attack");
        Check(_player.IsAttacking&&_player.Velocity.X>=speed-11,"Running jab preserves incoming momentum");
        await Frames(9);
        Check(_player.Position.X>x+25&&_player.Velocity.X>100&&_player.Velocity.X<150,"Ground jab carries forward and approaches half run speed");
        Input.ActionRelease("move_right");speed=_player.Velocity.X;await Frames(1);
        Check(Math.Abs(_player.Velocity.X-Math.Max(0,speed-15))<.1,"Ground attack neutral input brakes at 900 px/s squared");

        await CleanAttackStart();
        Input.ActionPress("attack");await Frames(1);Input.ActionRelease("attack");await Frames(2);
        for(int i=0;i<3;i++){Input.ActionPress("attack");await Frames(1);Input.ActionRelease("attack");await Frames(1);}
        Check(_player.AttackQueued,"Repeated presses retain a pending follow-up");
        Input.ActionPress("move_left");
        int previous=_player.Visual.AtlasFrame,restarts=0;bool seamless=false,newFacing=false;
        for(int i=0;i<45;i++)
        {
            bool active=_player.IsAttacking;
            await Frames(1);
            if(_player.IsAttacking&&_player.Visual.AtlasFrame==29&&previous!=29)
            {restarts++;seamless=active;newFacing=_player.Facing==-1;}
            previous=_player.Visual.AtlasFrame;
        }
        Check(restarts==1&&!_player.AttackQueued&&!_player.IsAttacking,"Multiple presses coalesce into exactly one complete follow-up");
        Check(seamless&&newFacing,"Queued cross starts without an idle gap and chooses the current direction");

        await CleanAttackStart();
        Input.ActionPress("jump");Input.ActionPress("attack");await Frames(1);Input.ActionRelease("attack");
        Check(_player.IsAttacking&&_player.IsOnFloor(),"Simultaneous jump/J starts a jab during grounded preparation");
        await Frames(5);
        Check(_player.IsAttacking&&!_player.IsOnFloor()&&_player.Velocity.Y<0,"Takeoff neither rejects nor cancels the jab");
        await Frames(13);
        Check(!_player.IsAttacking&&_player.MotionState=="jump","Completed aerial jab returns to the current jump animation");
        Input.ActionPress("attack");await Frames(1);Input.ActionRelease("attack");
        Check(_player.IsAttacking&&_player.Velocity.Y<0,"A fresh J press attacks during ascent");
        Input.ActionRelease("jump");await Frames(1);
        Check(_player.Velocity.Y>=-170&&_player.IsAttacking,"Short-jump release still works during an attack");

        await CleanAttackStart();
        _player.Position=new Vector2(480,160);_player.Velocity=new Vector2(200,100);await Frames(1);
        speed=_player.Velocity.X;float vertical=_player.Velocity.Y;
        Input.ActionPress("attack");await Frames(1);Input.ActionRelease("attack");
        Check(_player.IsAttacking&&Math.Abs(_player.Velocity.X-(speed+70-2))<.1,"Falling jab adds forward momentum and retains 120 px/s squared neutral braking");
        Check(_player.Velocity.Y>vertical,"Gravity continues during the jab");
        Input.ActionPress("move_left");speed=_player.Velocity.X;await Frames(1);
        Check(Math.Abs(_player.Velocity.X-(speed-10))<.1&&_player.Facing==1,"Air steering changes velocity at 600 while jab facing stays locked");
        Input.ActionRelease("move_left");

        await CleanAttackStart();
        _player.Position=new Vector2(480,310);_player.Velocity=new Vector2(0,200);await Frames(1);
        Input.ActionPress("attack");await Frames(1);Input.ActionRelease("attack");
        await Frames(7);
        Check(_player.IsOnFloor()&&_player.IsAttacking&&_player.Visual.AtlasFrame>24,"Landing preserves the jab's progress");
        await Frames(12);
        Check(!_player.IsAttacking&&_player.MotionState=="idle"&&!_player.Visual.AttackEffect.Visible,"After landing and attack recovery, idle resumes without lingering effects");

        await CleanAttackStart();
        Input.ActionPress("attack");await Frames(1);Input.ActionRelease("attack");await Frames(1);
        Input.ActionPress("attack");await Frames(1);Input.ActionRelease("attack");
        Check(_player.AttackQueued,"Reset test begins with a queued attack");
        _player.Reset();await Frames(22);
        Check(!_player.IsAttacking&&!_player.AttackQueued&&!_player.Visual.AttackEffect.Visible,"Reset clears both active and queued attacks");
        Input.ActionPress("attack");await Frames(1);Input.ActionRelease("attack");await Frames(1);
        Input.ActionPress("attack");await Frames(1);Input.ActionRelease("attack");
        _player.Position=new Vector2(480,860);await Frames(1);
        Check(!_player.IsAttacking&&!_player.AttackQueued&&_player.Position==_player.SpawnPosition,"Respawn also clears the attack queue");
        await CleanAttackStart();
    }
}
