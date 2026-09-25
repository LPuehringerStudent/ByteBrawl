using ByteBrawl.Combat;
using Godot;
using System;

namespace UnnamedFightingGame;

public interface IDamageReceiver
{
    DamageState Damage { get; }
    int ReceiveHit(AttackHit hit, float facing);
}

public readonly record struct AttackHit(int Percentage, string Name, float BaseSpeed, float Scaling, float Angle, double StunSeconds)
{
    public static readonly AttackHit Jab = new(2,"Jab",150,3,25,.15);
    public static readonly AttackHit Cross = new(3,"Cross",230,4.5f,35,.20);
    public static readonly AttackHit Kick = Cross with { Name="Kick", Percentage=4 };
    public Vector2 Launch(float damageBeforeHit, float facing)
    {
        float angle=Mathf.DegToRad(Angle);
        return FighterPhysics.Knockback(BaseSpeed,Scaling,damageBeforeHit,
            new Vector2(Mathf.Cos(angle),-Mathf.Sin(angle)),facing);
    }
}

public sealed class DamageState
{
    public float Percentage { get; private set; }
    public int LastDamage { get; private set; }
    public int HitCount { get; private set; }
    public double StunLeft { get; private set; }
    public bool Stunned => StunLeft>0;
    public Vector2 Apply(AttackHit hit,float facing,bool knockback=true)
    {
        if(hit.Percentage<=0)return Vector2.Zero;
        float before=Percentage;
        Percentage+=hit.Percentage;LastDamage=hit.Percentage;HitCount++;
        StunLeft=knockback?hit.StunSeconds:0;
        return knockback?hit.Launch(before,facing):Vector2.Zero;
    }
    public void Tick(double delta) { StunLeft=Math.Max(0,StunLeft-delta);if(StunLeft<.000001)StunLeft=0; }
    public void ClearStun() => StunLeft=0;
    public void Reset() { Percentage=0;LastDamage=HitCount=0;StunLeft=0; }
    public static Color Tint(float percent)
    {
        var white=new Color("e3edf5");var yellow=new Color("f0d778");
        var orange=new Color("ed9c51");var red=new Color("ee6155");
        if(percent<50)return white.Lerp(yellow,Mathf.Clamp(percent/50,0,1));
        if(percent<100)return yellow.Lerp(orange,(percent-50)/50);
        return orange.Lerp(red,Mathf.Clamp((percent-100)/50,0,1));
    }
}

public static class KnockoutBounds
{
    public const float Left=-240, Right=1200, Top=-300, Bottom=850;
    public static bool Outside(Vector2 p) => p.X<Left||p.X>Right||p.Y<Top||p.Y>Bottom;
}

public static class DamageFeedback
{
    public static void Show(Node2D parent,int damage,int sequence)
    {
        var number=new Label { Text=$"+{damage}%",Position=new Vector2(sequence%2==0?-45:20,-95),
            MouseFilter=Control.MouseFilterEnum.Ignore };
        number.AddThemeFontSizeOverride("font_size",18);
        number.AddThemeColorOverride("font_color",new Color("ffd787"));
        number.AddThemeColorOverride("font_outline_color",new Color("211b20"));
        number.AddThemeConstantOverride("outline_size",4);
        parent.AddChild(number);
        var tween=number.CreateTween().SetParallel();
        tween.TweenProperty(number,"position:y",number.Position.Y-38,.8);
        tween.TweenProperty(number,"modulate:a",0f,.8).SetDelay(.15);
        tween.Chain().TweenCallback(Callable.From(number.QueueFree));
    }
}
