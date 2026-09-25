using Godot;
using System;
using System.Linq;
using System.Text.Json;
using System.Security.Cryptography;
using System.Threading.Tasks;
using UnnamedFightingGame;

public partial class MovementSmoke : Node
{
    private int _failures, _checks;
    private Soldier _player = null!;
    private StagePlatform _stage = null!;
    private void Check(bool condition, string message)
    {
        _checks++;
        if (condition) GD.Print("PASS: " + message);
        else { _failures++; GD.PushError(message); }
    }
    private async Task Frames(int count)
    {
        for (int i=0;i<count;i++)
        {
            await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
        }
    }
    public override async void _Ready()
    {
        GetWindow().Size = new Vector2I(960,540); // Explicit baseline; headless defaults to 64x64.
        try
        {
            var arena=GD.Load<PackedScene>("res://scenes/arena.tscn").Instantiate();
            AddChild(arena);
            _player=arena.GetNode<Soldier>("Soldier");
            _stage=arena.GetNode<StagePlatform>("Platform");
            await Frames(10);
            Check(_player.IsOnFloor(),"Starts on scrapyard deck");
            Check(Math.Abs(_player.Position.Y-_stage.SurfaceY(_player.Position.X))<1,"Collision feet align with stage deck");
            Check(_player.Visual.PartCount==16,"Sixteen independent pixel part sprites");
            Check(_player.Visual.GetNodeOrNull<Marker2D>("NearWeaponSocket")!=null &&
                _player.Visual.GetNodeOrNull<Marker2D>("FarWeaponSocket")!=null,"Both weapon sockets preserved");
            await VerifyStage();
            VerifyArt();
            await VerifyAttack();
            await VerifyMobileAttacks();
            await VerifyCrossCombo();
            await VerifyKick();
            await VerifyJumpSequence();
            await VerifyDoubleJump();
            float startX=_player.Position.X;
            Input.ActionPress("move_right"); await Frames(25);
            Check(_player.Position.X>startX+60,"Right movement");
            Check(_player.MotionState=="run","Running selects run clip");
            Input.ActionRelease("move_right"); await Frames(15);
            Check(Math.Abs(_player.Velocity.X)<1,"Brakes to rest");
            Input.ActionPress("move_left"); await Frames(20);
            Check(_player.Visual.Scale.X==-1,"Left-facing whole-character flip");
            Input.ActionRelease("move_left"); await Frames(15);
            float floorY=_player.Position.Y;
            Input.ActionPress("jump"); await Frames(12);
            Check(_player.Position.Y<floorY-45,"Jump rises above platform");
            Check(!_player.IsOnFloor()&&_player.MotionState=="jump","Ascending pose");
            Input.ActionRelease("jump"); await Frames(14);
            Check(_player.MotionState=="fall","Descending pose");
            await Frames(40);
            Check(_player.IsOnFloor(),"Gravity lands on stage");
            Check(Math.Abs(_player.Position.Y-floorY)<1,"No floor penetration");
            _player.Position=new Vector2(_stage.Right+25,_stage.SurfaceY(_stage.Right)); await Frames(2);
            Input.ActionPress("jump"); await Frames(2);
            Check(_player.Velocity.Y < -100,"Coyote-time edge jump");
            Input.ActionRelease("jump");
            _player.Reset(); await Frames(5);
            _player.Position=new Vector2(_stage.Right+25,_stage.SurfaceY(_stage.Right)); await Frames(65);
            Check(_player.Position.DistanceTo(_player.SpawnPosition)<2,"Respawn after falling off");
            _player.Position=new Vector2(600,250);
            Input.ActionPress("reset"); await Frames(2); Input.ActionRelease("reset");
            Check(_player.Position.DistanceTo(_player.SpawnPosition)<2,"R resets player");
            _player.Position=new Vector2(935,240); Input.ActionPress("move_right"); await Frames(10);
            Check(_player.Position.X>938,"Movement can leave the original viewport");
            Input.ActionRelease("move_right");
            _player.Reset(); await Frames(4);
            _player.Position=new Vector2(_player.Position.X,_player.SpawnPosition.Y-22);
            _player.Velocity=new Vector2(0,220); _player.CoyoteLeft=0; await Frames(2);
            Input.ActionPress("jump"); await Frames(9);
            Check(_player.Velocity.Y < -100,"Jump buffered across landing");
            Input.ActionRelease("jump");
            Check(_player.Visual.GlobalPosition.DistanceTo(_player.GlobalPosition.Round())<0.01,
                "Visible sprite sits on integer pixels");
            GD.Print($"RESULT: {_checks} checks; {_failures} failures");
            GetTree().Quit(_failures==0?0:1);
        }
        catch(Exception ex) { GD.PushError(ex.ToString()); GetTree().Quit(1); }
    }

    private async Task VerifyStage()
    {
        var sprite=_stage.GetNode<Sprite2D>("Artwork");
        Check(sprite.Texture.GetWidth()==600&&sprite.Texture.GetHeight()==300&&sprite.Scale==Vector2.One,
            "Stage uses the smaller 600x300 pixel export");
        Check(_stage.TextureFilter==CanvasItem.TextureFilterEnum.Nearest&&_stage.Position==_stage.Position.Round(),
            "Stage uses nearest sampling and integer placement");
        Check(ProjectSettings.GetSetting("display/window/stretch/scale_mode").AsString()=="integer",
            "Window resizing uses integer viewport scaling");
        using var image=sprite.Texture.GetImage();image.Convert(Image.Format.Rgba8);
        using var report=JsonDocument.Parse(System.IO.File.ReadAllText(System.IO.Path.Combine(ProjectSettings.GlobalizePath("res://"),"..","art","PIXELLOID_REPORT.json")));
        var entry=report.RootElement.GetProperty("files").EnumerateArray().Single(e=>e.GetProperty("file").GetString()=="stages/stage_1.png");
        Check(Convert.ToHexString(SHA256.HashData(image.GetData())).ToLowerInvariant()==entry.GetProperty("processedRgbaSha256").GetString(),
            "Stage Godot pixels match Pixelloid exactly");
        Check(image.GetPixel(0,0).A==0&&image.GetPixel(599,299).A==0,"Checkerboard outside the stage is transparent");
        var colors=new System.Collections.Generic.HashSet<Color>();
        int fineTransitions=0;
        for(int y=0;y<300;y++)for(int x=0;x<600;x++)
        {
            Color color=image.GetPixel(x,y);if(color.A>0)colors.Add(color);
            if(x%2==0&&x+1<600&&color.A>0&&image.GetPixel(x+1,y).A>0&&color!=image.GetPixel(x+1,y))fineTransitions++;
        }
        var part=_player.Visual.GetNode<Sprite2D>("torso");
        Check(fineTransitions>100&&sprite.GlobalTransform.X.Length()==part.GlobalTransform.X.Length()&&
            sprite.GlobalTransform.Y.Length()==part.GlobalTransform.Y.Length(),"Stage uses fine 1:1 pixels at the same scale as Soldier parts");
        Check(colors.Count==32,"Stage contains exactly 32 opaque colors");
        Check(_stage.Surface.Length==2&&_stage.Surface[0].Y==_stage.Surface[1].Y,"One straight horizontal walking surface");
        var polygon=_stage.GetNode<CollisionPolygon2D>("DeckCollision").Polygon;
        Check(polygon.Take(_stage.Surface.Length).SequenceEqual(_stage.Surface),"Collision follows the shared deck lip trace");
        Godot.Collections.Dictionary Ray(float x)
        {
            float y=_stage.SurfaceY(x);
            var query=PhysicsRayQueryParameters2D.Create(new Vector2(x,y-24),new Vector2(x,y+30));
            query.Exclude=new Godot.Collections.Array<Rid>{_player.GetRid()};
            return _stage.GetWorld2D().DirectSpaceState.IntersectRay(query);
        }
        for(int i=0;i<=10;i++)
        {
            float x=Mathf.Lerp(_stage.Left+1,_stage.Right-1,i/10f);
            var hit=Ray(x);
            Check(hit.Count>0&&Math.Abs(hit["position"].AsVector2().Y-_stage.SurfaceY(x))<.1,
                $"Deck sample {i}: physics stays at the flat walking height");
        }
        Check(Ray(_stage.Left+.5f).Count>0&&Ray(_stage.Left-.5f).Count==0,"Left collision ends exactly at the traced deck edge");
        Check(Ray(_stage.Right-.5f).Count>0&&Ray(_stage.Right+.5f).Count==0,"Right collision ends exactly at the traced deck edge");
        foreach(int direction in new[]{-1,1})
        {
            _player.Reset();
            float edge=direction<0?_stage.Left:_stage.Right;
            float inside=edge-direction*24;
            _player.Position=new Vector2(inside,_stage.SurfaceY(inside)-8);await Frames(15);
            Check(_player.IsOnFloor(),$"{direction}: flat deck end supports the Soldier");
            string action=direction<0?"move_left":"move_right";
            Input.ActionPress(action);
            for(int i=0;i<25&&_player.IsOnFloor();i++)await Frames(1);
            Input.ActionRelease(action);
            float overhang=direction*(_player.Position.X-edge);
            Check(!_player.IsOnFloor()&&overhang>=-1&&overhang<=17,$"{direction}: walking beyond the visible ledge starts falling");
            await Frames(18);
            Check(_player.Position.Y>_stage.SurfaceY(edge)+20,$"{direction}: no invisible floor beyond the stage");
            await Frames(60);
            Check(_player.Position.DistanceTo(_player.SpawnPosition)<2,$"{direction}: falling off returns to stage spawn");
        }
        _player.Reset();await Frames(4);
    }

    private async Task VerifyAttack()
    {
        Check(InputMap.ActionGetEvents("attack").OfType<InputEventKey>().Any(k=>k.PhysicalKeycode==Key.J),
            "J physical key binds the attack action");
        _player.Reset();await Frames(3);
        float x=_player.Position.X;
        Input.ActionPress("attack");await Frames(1);
        Check(_player.MotionState=="attack"&&_player.Visual.AtlasFrame==24,"J starts the smear pose immediately");
        var seen=new System.Collections.Generic.HashSet<int>{_player.Visual.AtlasFrame};
        bool synced=true;
        for(int i=0;i<24;i++)
        {
            if(_player.MotionState=="attack")
            {
                seen.Add(_player.Visual.AtlasFrame);
                synced &= _player.Visual.AttackEffect.Visible &&
                    _player.Visual.AttackEffect.Frame==_player.Visual.AtlasFrame-24;
            }
            await Frames(1);
        }
        Check(Enumerable.Range(24,5).All(seen.Contains),"Jab shows all five reference poses");
        Check(synced,"Every jab pose synchronizes its smear or impact stage");
        Check(_player.MotionState=="idle"&&!_player.Visual.AttackEffect.Visible,"Held J completes once and clears effects");
        Check(Math.Abs(_player.Position.X-x)<.01,"Jab keeps its feet planted");
        Input.ActionRelease("attack");await Frames(2);
        Input.ActionPress("move_left");await Frames(4);Input.ActionRelease("move_left");await Frames(8);
        Input.ActionPress("attack");await Frames(1);
        Check(_player.MotionState=="attack","A fresh J press starts another jab");
        Check(_player.Visual.AttackEffect.GlobalTransform.X.X<0,"Left-facing jab mirrors the effect with the body");
        Input.ActionPress("move_right");await Frames(2);
        Check(_player.Facing==-1&&_player.Velocity.X>0,"Direction input steers without flipping the active jab");
        Input.ActionRelease("move_right");Input.ActionRelease("attack");
        Input.ActionPress("jump");await Frames(1);
        Check(_player.MotionState=="attack"&&_player.Visual.AttackEffect.Visible,"Jump preparation preserves active jab and effects");
        Input.ActionRelease("jump");await Frames(5);
        Input.ActionPress("attack");await Frames(1);
        Check(_player.MotionState=="attack"&&_player.AttackQueued,"Airborne press queues the next jab during an active attack");
        Input.ActionRelease("attack");_player.Reset();await Frames(3);
        Input.ActionPress("attack");await Frames(1);_player.Reset();
        Check(_player.MotionState=="idle"&&!_player.Visual.AttackEffect.Visible,"Reset clears jab state and effect");
        Input.ActionRelease("attack");await Frames(3);

        Input.ActionPress("attack");await Frames(1);Input.ActionRelease("attack");await Frames(11);
        Input.ActionPress("attack");await Frames(1);
        Check(_player.MotionState=="cross"&&_player.Visual.AtlasFrame==29,
            "J pressed exactly at jab completion starts the cross");
        Input.ActionRelease("attack");_player.Reset();await Frames(3);

        int[] ticks={2,4,2,2,2};
        for(int i=0;i<5;i++)
        {
            _player.Visual.SetPose("attack",i);
            _player.Visual.Advance("attack",(ticks[i]-.01)/60,1);
            Check(_player.Visual.AtlasFrame==24+i,$"Jab {i} holds for its accelerated duration");
            _player.Visual.Advance("attack",.02/60,1);
            Check(_player.Visual.AtlasFrame==24+Math.Min(i+1,4),$"Jab {i} advances at accelerated boundary without looping");
        }
        using var effect=_player.Visual.AttackEffect.Texture.GetImage();effect.Convert(Image.Format.Rgba8);
        using var report=JsonDocument.Parse(System.IO.File.ReadAllText(System.IO.Path.Combine(ProjectSettings.GlobalizePath("res://"),"..","art","PIXELLOID_REPORT.json")));
        var entry=report.RootElement.GetProperty("files").EnumerateArray().Single(e=>e.GetProperty("file").GetString()=="effects/attack.png");
        Check(Convert.ToHexString(SHA256.HashData(effect.GetData())).ToLowerInvariant()==entry.GetProperty("processedRgbaSha256").GetString(),
            "Godot effect pixels match the Pixelloid export exactly");
        _player.Reset();await Frames(3);
    }

    private async Task VerifyJumpSequence()
    {
        _player.Reset();await Frames(3);
        float floor=_player.Position.Y;
        Input.ActionPress("jump");await Frames(1);
        Check(_player.MotionState=="prepare"&&_player.IsOnFloor(),"Jump begins with grounded preparation");
        var seen=new System.Collections.Generic.HashSet<int>{_player.Visual.AtlasFrame};
        for(int i=0;i<80;i++){await Frames(1);seen.Add(_player.Visual.AtlasFrame);}
        Input.ActionRelease("jump");await Frames(2);
        Check(Enumerable.Range(16,8).All(seen.Contains),"Full jump displays all eight reference poses");
        Check(_player.IsOnFloor()&&_player.MotionState=="idle","Landing recovery returns to idle");
        Input.ActionPress("jump");await Frames(1);Input.ActionRelease("jump");
        float peak=floor;
        for(int i=0;i<50;i++){await Frames(1);peak=Math.Min(peak,_player.Position.Y);}
        Check(floor-peak>5&&floor-peak<40,"Releasing during preparation produces a short jump");
        _player.Reset();await Frames(4);
    }

    private void VerifyArt()
    {
        string reportPath=System.IO.Path.Combine(ProjectSettings.GlobalizePath("res://"),"..","art","PIXELLOID_REPORT.json");
        using var report=JsonDocument.Parse(System.IO.File.ReadAllText(reportPath));
        var entries=report.RootElement.GetProperty("files").EnumerateArray()
            .Where(e=>e.GetProperty("file").GetString()!.StartsWith("layers/"))
            .ToDictionary(e=>System.IO.Path.GetFileNameWithoutExtension(e.GetProperty("file").GetString()!));
        foreach(var node in _player.Visual.GetChildren())
        {
            if(node is not Sprite2D sprite)continue;
            using var image=sprite.Texture.GetImage();
            image.Convert(Image.Format.Rgba8);
            string hash=Convert.ToHexString(SHA256.HashData(image.GetData())).ToLowerInvariant();
            Check(hash==entries[sprite.Name.ToString()].GetProperty("processedRgbaSha256").GetString(),
                $"{sprite.Name}: Godot pixels match Pixelloid output");
            Check(sprite.Rotation==0&&sprite.Scale==Vector2.One&&sprite.Hframes==57,
                $"{sprite.Name}: exact pixels, no raster rotation or scaling");
        }
        foreach(var (clip,info) in SoldierVisual.Clips)
        {
            for(int frame=0;frame<info.Count;frame++)
            {
                _player.Visual.SetPose(clip,frame);
                Check(_player.Visual.GetChildren().OfType<Sprite2D>().All(s=>s.Frame==info.Start+frame),
                    $"{clip}/{frame}: all layers share frame index");
            }
        }
        int[] idleTicks={6,6,6,6,3,3,3,3};
        double walkSeconds=SoldierVisual.Clips["run"].Ticks/60;
        for(int i=0;i<8;i++)
        {
            _player.Visual.SetPose("run",i);
            _player.Visual.Advance("run",walkSeconds-.001,1);
            Check(_player.Visual.AtlasFrame==8+i,$"Walk {i} retains the configured hold duration");
            _player.Visual.Advance("run",.002,1);
            Check(_player.Visual.AtlasFrame==8+(i+1)%8,$"Walk {i} advances at reference boundary");
        }
        _player.Visual.SetPose("run",0);
        _player.Visual.Advance("run",walkSeconds*8+.00001,1);
        Check(_player.Visual.AtlasFrame==8,"Walk completes its eight poses at the configured cadence");
        for(int i=0;i<8;i++)
        {
            _player.Visual.SetPose("idle",i);
            _player.Visual.Advance("idle",(idleTicks[i]-.01)/60,1);
            Check(_player.Visual.AtlasFrame==i,$"Idle {i} holds for its reference duration");
            _player.Visual.Advance("idle",.02/60,1);
            Check(_player.Visual.AtlasFrame==(i+1)%8,$"Idle {i} advances and wraps on the correct boundary");
        }
        _player.Visual.SetPose("idle",0);
        _player.Visual.Advance("idle",.60,1);
        Check(_player.Visual.AtlasFrame==0,"Idle loops at 600 ms");
        _player.Visual.SetPose("run",3);
        _player.Visual.Advance("idle",.1,1);
        Check(_player.Visual.AtlasFrame==0,"Returning from movement starts idle at its first pose");
        using var poses=JsonDocument.Parse(FileAccess.GetFileAsString("res://assets/soldier_frames/poses.json"));
        foreach(var frame in poses.RootElement.GetProperty("frames").EnumerateArray().Take(8))
        {
            var parts=frame.GetProperty("parts");
            int Y(string part,string point)=>parts.GetProperty(part).GetProperty(point).GetProperty("Y").GetInt32();
            Check(Y("near_hand","Start")<Y("near_forearm","Start") &&
                  Y("far_hand","Start")<Y("far_forearm","Start"),
                frame.GetProperty("id").GetString()+": both fists stay above elbows in guard");
        }
        _player.Visual.SetPose("idle",0);
    }
}
