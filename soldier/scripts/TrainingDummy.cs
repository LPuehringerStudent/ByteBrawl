using ByteBrawl.Combat;
using Godot;
using System;

namespace UnnamedFightingGame;

public partial class TrainingDummy : CharacterBody2D, IDamageReceiver
{
    public DamageState Damage { get; } = new();
    public bool KnockbackEnabled { get; set; }
    public bool AutoResetPosition { get; set; } = true;
    public Vector2 HomePosition { get; private set; }
    public const double ReturnDelay = 2;
    private double _sinceHit;
    private bool _returnPending;
    private Label _percentText = null!, _lastHit = null!;
    private Node2D _numbers = null!;
    private Sprite2D _art = null!;

    public override void _Ready()
    {
        HomePosition = GlobalPosition;
        _art = GetNode<Sprite2D>("Artwork");
        _percentText = MakeLabel(new(-60,-139),new(120,26),22);
        _lastHit = MakeLabel(new(-70,-163),new(140,18),11);
        _numbers = new Node2D { Name = "DamageNumbers" };
        AddChild(_numbers);
        ResetDummy();
    }

    private Label MakeLabel(Vector2 position,Vector2 size,int fontSize)
    {
        var label = new Label { Position=position,Size=size,HorizontalAlignment=HorizontalAlignment.Center,
            MouseFilter=Control.MouseFilterEnum.Ignore };
        label.AddThemeFontSizeOverride("font_size",fontSize);
        label.AddThemeColorOverride("font_shadow_color",Colors.Black);
        label.AddThemeConstantOverride("shadow_offset_x",1);
        label.AddThemeConstantOverride("shadow_offset_y",1);
        AddChild(label);
        return label;
    }

    public void SetHome(Vector2 position) { HomePosition=position;ResetDummy(); }

    public int ReceiveHit(AttackHit hit,float facing)
    {
        if(hit.Percentage<=0)return 0;
        var impulse=Damage.Apply(hit,facing,KnockbackEnabled);
        _sinceHit=0;_returnPending=true;
        if(KnockbackEnabled)Velocity=impulse;
        _lastHit.Text=$"{hit.Name.ToUpperInvariant()}  +{Damage.LastDamage}%";
        DamageFeedback.Show(_numbers,Damage.LastDamage,Damage.HitCount);
        UpdatePercentage();
        return Damage.LastDamage;
    }

    public override void _PhysicsProcess(double delta)
    {
        _sinceHit+=delta;
        if(KnockoutBounds.Outside(GlobalPosition)) { ResetDummy();return; }
        if(AutoResetPosition&&_returnPending&&_sinceHit>=ReturnDelay) ResetPosition();
        var velocity=Velocity;
        if(!KnockbackEnabled)velocity.X=0;
        else if(!Damage.Stunned)velocity.X=Mathf.MoveToward(velocity.X,0,600*(float)delta);
        Damage.Tick(delta);
        velocity.Y=FighterPhysics.GravityStep(velocity.Y,1350,delta,800);
        Velocity=velocity;
        MoveAndSlide();
        _art.Position=GlobalPosition.Round()-GlobalPosition;
    }

    public void ResetPosition()
    {
        GlobalPosition=HomePosition;Velocity=Vector2.Zero;_returnPending=false;Damage.ClearStun();
        _art.Position=Vector2.Zero;
    }

    public void ResetDummy()
    {
        ResetPosition();Damage.Reset();_sinceHit=0;
        _lastHit.Text="WOODEN DUMMY";
        foreach(Node number in _numbers.GetChildren()) number.QueueFree();
        UpdatePercentage();
    }

    private void UpdatePercentage()
    {
        _percentText.Text=$"{Damage.Percentage:0}%";
        _percentText.AddThemeColorOverride("font_color",DamageState.Tint(Damage.Percentage));
    }
}
