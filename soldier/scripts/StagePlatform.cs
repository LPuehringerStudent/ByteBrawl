using Godot;
using System;
using System.Linq;
using System.Text.Json;

namespace UnnamedFightingGame;

/// <summary>The artwork and deck collision share one source-coordinate layout.</summary>
public partial class StagePlatform : StaticBody2D
{
    public Vector2[] Surface { get; private set; } = Array.Empty<Vector2>();
    public Vector2 Spawn { get; private set; }
    public float Left => ToGlobal(Surface[0]).X;
    public float Right => ToGlobal(Surface[^1]).X;

    public override void _Ready()
    {
        using var document=JsonDocument.Parse(FileAccess.GetFileAsString("res://assets/stage/stage_1.layout.json"));
        var layout=document.RootElement;
        Vector2 Point(JsonElement p)=>new(p.GetProperty("x").GetSingle(),p.GetProperty("y").GetSingle());
        Position=Point(layout.GetProperty("position"));
        Spawn=Point(layout.GetProperty("spawn"));
        var crop=Point(layout.GetProperty("crop"));
        Surface=layout.GetProperty("surface").EnumerateArray().Select(p=>new Vector2(p[0].GetSingle(),p[1].GetSingle())-crop).ToArray();
        float depth=layout.GetProperty("collisionDepth").GetSingle();
        var polygon=Surface.Concat(Surface.Reverse().Select(p=>p+new Vector2(0,depth))).ToArray();
        GetNode<CollisionPolygon2D>("DeckCollision").Polygon=polygon;
    }

    public float SurfaceY(float worldX)
    {
        float x=ToLocal(new Vector2(worldX,0)).X;
        for(int i=1;i<Surface.Length;i++)if(x<=Surface[i].X)
        {
            Vector2 a=Surface[i-1],b=Surface[i];
            return GlobalPosition.Y+Mathf.Lerp(a.Y,b.Y,Mathf.Clamp((x-a.X)/(b.X-a.X),0,1));
        }
        return ToGlobal(Surface[^1]).Y;
    }
}
