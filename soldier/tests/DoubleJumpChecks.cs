using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnnamedFightingGame;

public partial class MovementSmoke
{
    private async Task VerifyDoubleJump()
    {
        Input.ActionRelease("jump"); Input.ActionRelease("move_right"); Input.ActionRelease("attack");
        _player.Reset(); await Frames(8);
        Input.ActionPress("jump"); await Frames(15);
        Check(_player.AirJumpAvailable && !_player.Visual.ThrustersVisible,"First jump has no thrusters and holding does not consume second jump");
        Input.ActionRelease("jump"); await Frames(1);
        Input.ActionPress("jump"); await Frames(1);
        Check(!_player.AirJumpAvailable && _player.Velocity.Y < -470,"Fresh airborne press immediately launches second jump");
        Check(_player.Visual.ThrustersVisible && _player.Visual.PartCount==16,"Four thrusters are separate from sixteen body parts");
        var poses=new HashSet<int>(); var flames=new HashSet<int>();
        for(int i=0;i<65;i++)
        {
            poses.Add(_player.Visual.AtlasFrame); flames.Add(_player.Visual.ThrusterFrame);
            if(_player.Velocity.Y>=0)Check(!_player.Visual.ThrustersVisible,"Thrusters stop during descent");
            await Frames(1);
        }
        for(int i=37;i<=44;i++)Check(poses.Contains(i),$"Powered jump displays pose {i}");
        Check(flames.Count==4,"Powered ascent plays all four flame frames");
        Check(_player.IsOnFloor() && _player.AirJumpAvailable,"Landing replenishes second jump");
        Input.ActionRelease("jump"); await Frames(1);
        _player.Reset(); await Frames(6);
        Input.ActionPress("jump"); await Frames(12); Input.ActionRelease("jump"); await Frames(1);
        Input.ActionPress("jump"); await Frames(2); Input.ActionRelease("jump"); await Frames(1);
        Check(_player.Velocity.Y >= -182.1f,"Second jump release uses its short-hop clamp");
        float vy=_player.Velocity.Y;
        Input.ActionPress("jump"); await Frames(1);
        Check(_player.Velocity.Y > vy && !_player.AirJumpAvailable,"Third press cannot jump before ground contact");
        Input.ActionRelease("jump");
        _player.ReceiveHit(AttackHit.Jab,1);
        Check(!_player.AirJumpAvailable && !_player.Visual.ThrustersVisible,"Being hit clears thrust without replenishing air jump");
        Input.ActionPress("jump"); await Frames(1);
        Check(!_player.PoweredAscent,"Hitstun blocks jumping");
        Input.ActionRelease("jump");
        _player.Reset(); await Frames(6);
        Check(_player.AirJumpAvailable && !_player.Visual.ThrustersVisible,"Reset restores jump and clears flames");
        // Compare complete free-flight arcs from their launch heights, with full directional speed.
        async Task<(float Height,float Reach)> Arc(bool powered)
        {
            _player.Reset(); await Frames(6);
            _player.Position=new Vector2(400,_player.SpawnPosition.Y-100);
            _player.Velocity=new Vector2(powered?253:235,0);
            await Frames(1);
            _player.CoyoteLeft=powered?0:.1f;
            Input.ActionPress("move_right"); Input.ActionPress("jump");
            float y=_player.Position.Y,x=_player.Position.X,min=y;
            await Frames(1);
            for(int i=0;i<80;i++)
            {
                min=Math.Min(min,_player.Position.Y);
                if(_player.Velocity.Y>0 && _player.Position.Y>=y)break;
                await Frames(1);
            }
            var result=(y-min,_player.Position.X-x);
            Input.ActionRelease("jump"); Input.ActionRelease("move_right"); await Frames(1);
            return result;
        }
        var first=await Arc(false); var second=await Arc(true);
        double height=second.Height/first.Height-1,reach=second.Reach/first.Reach-1;
        GD.Print($"DOUBLE JUMP METRICS: height +{height:P2}, reach +{reach:P2}; first={first}, second={second}");
        Check(height>=.12 && height<=.18,"Second jump height is approximately 15 percent greater");
        Check(reach>=.12 && reach<=.18,"Second jump reach is approximately 15 percent greater");
        _player.Reset(); await Frames(6);
        Input.ActionPress("jump"); await Frames(12); Input.ActionRelease("jump"); await Frames(1);
        Input.ActionPress("jump"); await Frames(1); Input.ActionPress("attack"); await Frames(1); Input.ActionRelease("attack");
        Check(_player.IsAttacking && _player.Visual.ThrustersVisible,"Air attack preserves powered ascent effects");
        await Frames(18);
        Check(_player.Visual.AtlasFrame>=41,"Finishing an air attack does not restart powered jump poses");
        Input.ActionRelease("jump"); _player.Reset(); await Frames(8);
        // A ledge departure still grants only the one airborne jump.
        _player.Position=new Vector2(_stage.Right+25,_player.SpawnPosition.Y-60);
        _player.Velocity=Vector2.Zero; await Frames(8); _player.CoyoteLeft=0;
        Input.ActionPress("jump"); await Frames(1);
        Check(!_player.AirJumpAvailable && _player.PoweredAscent,"Walking off a ledge grants one powered jump");
        Input.ActionRelease("jump"); _player.Reset(); await Frames(8);
        // Consume the air jump before testing a press buffered across landing.
        Input.ActionPress("jump"); await Frames(12); Input.ActionRelease("jump"); await Frames(1);
        Input.ActionPress("jump"); await Frames(1); Input.ActionRelease("jump"); await Frames(1);
        _player.Position=new Vector2(400,_player.SpawnPosition.Y-22);
        _player.Velocity=new Vector2(0,220); _player.CoyoteLeft=0; await Frames(1);
        Input.ActionPress("jump"); await Frames(9);
        Check(_player.Velocity.Y < -100 && _player.AirJumpAvailable,"Spent air jump preserves the landing buffer and refreshes on ground");
        Input.ActionRelease("jump"); _player.Reset(); await Frames(8);
        // Ceiling contact ends thrust immediately and never refunds the consumed jump.
        var ceiling=new StaticBody2D { Position=new Vector2(400,160) };
        ceiling.AddChild(new CollisionShape2D { Shape=new RectangleShape2D { Size=new Vector2(200,10) } });
        GetParent().AddChild(ceiling);
        _player.Position=new Vector2(400,310); _player.Velocity=Vector2.Zero;
        await Frames(8); _player.CoyoteLeft=0;
        Input.ActionPress("jump"); await Frames(1);
        bool touched=false;
        for(int i=0;i<30;i++)
        {
            await Frames(1);
            if(_player.IsOnCeiling())
            {
                touched=true;
                Check(!_player.Visual.ThrustersVisible && !_player.AirJumpAvailable,"Ceiling contact stops flames and cannot restore jump");
                break;
            }
        }
        Check(touched,"Ceiling test makes physical contact");
        ceiling.QueueFree(); Input.ActionRelease("jump"); _player.Reset(); await Frames(8);
        // All outlets follow their frame metadata, also while mirrored and punching.
        using(var sockets=System.Text.Json.JsonDocument.Parse(FileAccess.GetFileAsString("res://assets/soldier_frames/thruster-sockets.json")))
        {
            _player.SetPhysicsProcess(false);
            foreach(float facing in new[]{1f,-1f})
            foreach(string clip in new[]{"double_rise","attack","cross"})
            {
                _player.Visual.Scale=new Vector2(facing,1);
                _player.Visual.SetPose(clip,2); _player.Visual.SetThrusters(true,.05);
                int index=0;
                foreach(var point in sockets.RootElement[_player.Visual.AtlasFrame].GetProperty("points").EnumerateArray())
                {
                    var effect=_player.Visual.GetNode<Sprite2D>("Thrusters/Outlet"+index++);
                    var expected=new Vector2(point.GetProperty("X").GetInt32()-64,point.GetProperty("Y").GetInt32()-121);
                    Check(effect.Position==expected && effect.Scale==Vector2.One && effect.GlobalPosition==_player.Visual.ToGlobal(expected),
                        "Thruster follows integer outlet in mirrored/attack pose without scaling");
                }
            }
            if(System.Array.IndexOf(OS.GetCmdlineUserArgs(),"--capture-double")>=0)
            {
                _player.Visual.Scale=Vector2.One;
                _player.Visual.SetPose("double_rise",2); _player.Visual.SetThrusters(true,.05);
                _player.Position-=new Vector2(0,100);
                await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
                Check(GetViewport().GetTexture().GetImage().SavePng("../artifacts/double-jump-game.png")==Error.Ok,"Capture powered jump in game");
            }
            _player.SetPhysicsProcess(true);
        }
        _player.Reset(); await Frames(8);
    }
}
