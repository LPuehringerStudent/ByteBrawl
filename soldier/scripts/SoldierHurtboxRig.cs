using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace UnnamedFightingGame;

/// <summary>ByteBrawl-style per-limb capsules fitted to each discrete Soldier sprite pose.</summary>
public partial class SoldierHurtboxRig : Node2D
{
    public Soldier Fighter { get; set; } = null!;
    public bool ShowDebug { get; set; }
    public IReadOnlyDictionary<string,LimbHurtbox> Parts => _parts;
    private readonly Dictionary<string,LimbHurtbox> _parts=new();
    private readonly List<Dictionary<string,CapsulePose>> _frames=new();
    private readonly record struct CapsulePose(Vector2 Center,float Radius,float Height,float Rotation,string Group);
    public override void _Ready()
    {
        ZIndex=20;
        if(!InputMap.HasAction("combat_debug"))
        {
            InputMap.AddAction("combat_debug");
            InputMap.ActionAddEvent("combat_debug",new InputEventKey { PhysicalKeycode=Key.F3 });
        }
        using var doc=JsonDocument.Parse(FileAccess.GetFileAsString("res://assets/soldier_frames/hurtboxes.json"));
        foreach(var frame in doc.RootElement.GetProperty("frames").EnumerateArray())
        {
            var poses=new Dictionary<string,CapsulePose>();
            foreach(var part in frame.GetProperty("parts").EnumerateArray())
                poses[part.GetProperty("id").GetString()!]=new(new(part.GetProperty("x").GetSingle(),part.GetProperty("y").GetSingle()),
                    part.GetProperty("radius").GetSingle(),part.GetProperty("height").GetSingle(),
                    part.GetProperty("rotation").GetSingle(),part.GetProperty("group").GetString()!);
            _frames.Add(poses);
        }
        foreach(var (id,pose) in _frames[0])
        {
            var part=new LimbHurtbox { Name=id,Fighter=Fighter,Group=pose.Group };
            AddChild(part); _parts.Add(id,part);
        }
        Fighter.Visual.FrameApplied+=ApplyFrame;
        ApplyFrame(Fighter.Visual.AtlasFrame);
    }
    private void ApplyFrame(int frame)
    {
        foreach(var (id,pose) in _frames[frame])
        {
            var part=_parts[id];
            part.Position=pose.Center;part.Rotation=pose.Rotation;
            part.Capsule.Radius=pose.Radius;part.Capsule.Height=pose.Height;
        }
        QueueRedraw();
    }
    public override void _Process(double delta)
    {
        if(Input.IsActionJustPressed("combat_debug"))ShowDebug=!ShowDebug;
        QueueRedraw();
    }
    public override void _Draw()
    {
        if(!ShowDebug)return;
        foreach(var part in _parts.Values)
        {
            float r=part.Capsule.Radius,half=part.Capsule.Height/2-r;
            var a=part.Transform*new Vector2(0,-half);var b=part.Transform*new Vector2(0,half);
            var color=new Color(.4f,1,.55f,.3f);
            DrawLine(a,b,color,r*2);DrawCircle(a,r,color);DrawCircle(b,r,color);
        }
        var socket=Fighter.GetNode<SoldierCombat>("Combat").ActiveSocket;
        if(socket!=null)DrawCircle(ToLocal(socket.GlobalPosition),9,new Color(1,.2f,.15f,.65f));
    }
    public override void _ExitTree() { if(Fighter!=null)Fighter.Visual.FrameApplied-=ApplyFrame; }
}
