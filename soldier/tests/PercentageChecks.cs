using Godot;
using System;
using System.Threading.Tasks;
using UnnamedFightingGame;

public partial class TrainingSmoke
{
    private async Task VerifyPercentageAndCamera()
    {
        foreach(int initial in new[]{0,50,100,200})
        foreach(var hit in new[]{AttackHit.Jab,AttackHit.Cross})
        {
            var damage=new DamageState();
            damage.Apply(new AttackHit(initial,"Setup",0,0,0,0),1,false);
            Vector2 right=damage.Apply(hit,1),left=hit.Launch(initial,-1);
            Check(damage.Percentage==initial+hit.Percentage,$"{hit.Name} adds its full percentage at {initial}%");
            Check(Math.Abs(right.Length()-(hit.BaseSpeed+hit.Scaling*initial))<.001,
                $"{hit.Name} launch uses ByteBrawl pre-hit percentage at {initial}%");
            Check(right.X>0&&right.Y<0&&left.X==-right.X&&left.Y==right.Y,$"{hit.Name} launch mirrors at {initial}%");
            damage.Tick(hit.StunSeconds-.001);bool stunHeld=damage.Stunned;
            damage.Tick(.001);
            Check(stunHeld&&!damage.Stunned,$"{hit.Name} preserves its complete hitstun duration");
        }
        Check(DamageState.Tint(0)!=DamageState.Tint(50)&&DamageState.Tint(50)!=DamageState.Tint(100)&&
            DamageState.Tint(100)!=DamageState.Tint(150),"Percentage display progresses through white, yellow, orange and red");

        await Setup(480);
        var ownShape=_player.GetNode<CollisionShape2D>("Visual/HurtboxRig/head/Shape");
        var original=ownShape.Shape;ownShape.Shape=new RectangleShape2D { Size=new Vector2(220,110) };
        await Frames(2);await Press();await Frames(13);
        Check(_player.Damage.Percentage==0,"A fist overlapping its own hurtbox cannot self-hit");
        ownShape.Shape=original;

        await Setup();
        var opponent=GD.Load<PackedScene>("res://scenes/soldier.tscn").Instantiate<Soldier>();
        _player.GetParent().AddChild(opponent);
        opponent.SetPhysicsProcess(false);opponent.Position=new Vector2(620,334);
        await Frames(2);await Press();await Frames(8);
        Check(opponent.Damage.Percentage==2&&opponent.Damage.HitCount==1,"Shared hit query damages another Soldier once");
        opponent.QueueFree();await Frames(1);

        await Setup();await Press();
        _player.ReceiveHit(AttackHit.Cross,-1);
        Vector2 launch=_player.Velocity;
        Check(_player.Damage.Percentage==3&&_player.Damage.Stunned&&!_player.IsAttacking&&
            !_player.Visual.AttackEffect.Visible,"Received hit cancels the Soldier combo and starts hitstun");
        Input.ActionPress("move_right");Input.ActionPress("jump");Input.ActionPress("attack");
        await Frames(6);
        Check(Math.Abs(_player.Velocity.X-launch.X)<.01&&_player.Velocity.Y>launch.Y&&!_player.IsAttacking,
            "Hitstun preserves horizontal launch against steering, jumping and attack input while gravity continues");
        await Frames(7);
        Check(!_player.Damage.Stunned,"Soldier regains control after cross hitstun");
        await Setup();

        foreach(Vector2 outside in new[]{new Vector2(-241,334),new Vector2(1201,334),new Vector2(480,-301),new Vector2(480,851)})
        {
            _player.ReceiveHit(AttackHit.Jab,1);_player.Position=outside;await Frames(1);
            Check(_player.Position==_player.SpawnPosition&&_player.Damage.Percentage==0&&_player.Velocity==Vector2.Zero&&
                !_player.Damage.Stunned&&!_player.IsAttacking,$"Soldier knockout at {outside} resets all combat state");
            _dummy.ReceiveHit(AttackHit.Jab,1);_dummy.Position=outside;await Frames(1);
            Check(_dummy.Position==_dummy.HomePosition&&_dummy.Damage.Percentage==0&&_dummy.Velocity==Vector2.Zero,
                $"Dummy knockout at {outside} resets percentage and position");
        }
        Check(!KnockoutBounds.Outside(new(-240,-300))&&!KnockoutBounds.Outside(new(1200,850))&&
            KnockoutBounds.Outside(new(1200.01f,334)),"Knockout occurs beyond the shared boundary, not at its edge");

        _dummy.AutoResetPosition=false;_dummy.ReceiveHit(AttackHit.Jab,1);await Frames(125);
        _dummy.Position=new Vector2(1201,334);_dummy.AutoResetPosition=true;await Frames(1);
        Check(_dummy.Damage.Percentage==0&&_dummy.Position==_dummy.HomePosition,
            "Knockout takes precedence over an overdue position-only return");
        Check(_dummy.GetNode<Sprite2D>("Artwork").Position==Vector2.Zero,"Dummy reset clears any fractional display offset");

        await Setup(450);_player.Position=new Vector2(450,160);_player.Velocity=new Vector2(300,0);await Frames(1);
        _player.Velocity=new Vector2(260,_player.Velocity.Y);
        Vector2 before=_player.Velocity;await Press();
        Check(Math.Abs(_player.Velocity.X-(before.X+68))<.1,"Airborne jab adds 70 horizontal speed before neutral braking");
        Check(Math.Abs(_player.Velocity.Y-(before.Y+_player.Gravity/60))<.1,"Jab recovery adds no upward lift");
        Input.ActionPress("move_right");float boosted=_player.Velocity.X;await Frames(1);
        Check(Math.Abs(_player.Velocity.X-boosted)<.1,"Same-direction attack input preserves boosted air momentum");
        await Press();
        float previousY=_player.Velocity.Y;
        while(_player.AttackNumber==1) { previousY=_player.Velocity.Y;await Frames(1); }
        Check(_player.AttackNumber==2&&Math.Abs(_player.Velocity.X-360)<.1,"Airborne cross adds 100 speed with a 360 cap");
        Check(Math.Abs(_player.Velocity.Y-(previousY+_player.Gravity/60))<.1,"Cross recovery does not interrupt falling velocity");
        await Setup(450);_player.Position=new(450,160);_player.Velocity=new(-300,0);
        Input.ActionPress("move_left");await Frames(1);before=_player.Velocity;await Press();
        Check(_player.Velocity.X<before.X-60&&_player.Facing==-1,"Recovery momentum follows left-facing punches too");
        await Setup();

        var camera=_player.GetParent().GetNode<ArenaCamera>("Camera");
        var indicator=_player.GetParent().GetNode<DummyIndicator>("HUD/DummyIndicator");
        _player.SetPhysicsProcess(false);_dummy.SetPhysicsProcess(false);
        _player.Reset();await Frames(2);
        Check(camera.GlobalPosition==ArenaCamera.Home&&camera.Zoom==Vector2.One,"Original framing and 1x pixel scale hold on stage");
        _dummy.ReceiveHit(AttackHit.Cross with { Percentage=200 },1);
        foreach(Vector2 position in new[]{new Vector2(1080,334),new Vector2(-180,334),new Vector2(480,-200),new Vector2(480,780)})
        {
            _dummy.Position=position;await Frames(2);
            Check(indicator.Visible&&indicator.Position.X>=70&&indicator.Position.X<=890&&indicator.Position.Y>=130&&indicator.Position.Y<=470,
                $"Offscreen dummy at {position} has a bounded edge indicator");
            Vector2 direction=(position+new Vector2(0,-62)-indicator.Position).Normalized();
            Check(indicator.ArrowDirection.Dot(direction)>.99,"Indicator arrow points toward the offscreen target");
            Check(camera.GlobalPosition==ArenaCamera.Home,"Dummy position does not move the camera");
        }
        _dummy.Position=new(1080,334);await Frames(2);await Capture("percentage-offscreen");
        _dummy.Position=new(980,334);await Frames(2);
        Check(!indicator.Visible,"A partially visible dummy does not need an edge icon");
        _dummy.ResetDummy();await Frames(1);
        Check(!indicator.Visible,"Dummy reset hides the offscreen indicator");

        foreach(var (position,goal) in new[]{(new Vector2(1050,334),new Vector2(720,270)),
            (new Vector2(-100,334),new Vector2(240,270)),(new Vector2(480,-200),new Vector2(280,90)),
            (new Vector2(480,700),new Vector2(280,450))})
        {
            _player.Position=position;await Frames(75);
            Check(camera.Goal==goal&&camera.GlobalPosition.DistanceTo(goal)<=1,$"Camera settles at bounded center {goal}");
            Check(camera.GlobalPosition==camera.GlobalPosition.Round()&&camera.Zoom==Vector2.One,"Camera preserves integer render pixels without zooming");
        }
        _player.Position=new(1050,334);await Frames(60);await Capture("percentage-camera");
        _player.Reset();
        Check(camera.GlobalPosition==ArenaCamera.Home,"Player reset immediately snaps the camera home");
        _settings.SetOpen(true);Vector2 paused=camera.GlobalPosition;_player.Position=new(1000,334);await Frames(3);
        Check(camera.GlobalPosition==paused,"Settings pause camera movement along with gameplay");
        _settings.SetOpen(false);
        _player.Reset();_dummy.ResetDummy();_player.SetPhysicsProcess(true);_dummy.SetPhysicsProcess(true);
        await Setup();
        _dummy.ReceiveHit(AttackHit.Cross with { Percentage=150 },1);
        _player.ReceiveHit(AttackHit.Jab with { Percentage=50 },-1);
        await Frames(1);await Capture("percentage-high-damage");
        await Setup();
    }
}
