using Godot;
using System.Collections.Generic;

namespace UnnamedFightingGame;

/// <summary>Damage follows the active fist or foot only during impact poses, once per target per attack.</summary>
public partial class SoldierCombat : Node2D
{
    [Export] public int JabDamage { get; set; } = 2;
    [Export] public int CrossDamage { get; set; } = 3;
    [Export] public int KickDamage { get; set; } = 4;
    private Soldier _soldier = null!;
    private readonly CircleShape2D _fist = new() { Radius=9 };
    private readonly HashSet<ulong> _hitTargets = new();
    private ulong _serial;

    public Marker2D? ActiveSocket
    {
        get
        {
            if(!_soldier.IsAttacking)return null;
            int frame=_soldier.Visual.AtlasFrame;
            return _soldier.AttackNumber switch
            {
                1 when frame==25 => _soldier.Visual.GetNode<Marker2D>("FarWeaponSocket"),
                2 when frame is 32 or 33 => _soldier.Visual.GetNode<Marker2D>("NearWeaponSocket"),
                3 when frame is 47 or 48 or 49 => _soldier.Visual.KickSocket,
                _ => null
            };
        }
    }
    public override void _Ready() { _soldier=GetParent<Soldier>();ProcessPhysicsPriority=1; }

    public override void _PhysicsProcess(double delta)
    {
        if(_serial!=_soldier.AttackSerial) { _serial=_soldier.AttackSerial;_hitTargets.Clear(); }
        var socket=ActiveSocket;
        if(socket==null)return;
        bool cross=_soldier.AttackNumber==2, kick=_soldier.AttackNumber==3;
        var query=new PhysicsShapeQueryParameters2D {
            Shape=_fist,Transform=new Transform2D(0,socket.GlobalPosition),CollisionMask=8,
            CollideWithAreas=true,CollideWithBodies=false
        };
        foreach(var result in GetWorld2D().DirectSpaceState.IntersectShape(query,128))
        {
            if(result["collider"].AsGodotObject() is not Area2D area)continue;
            var target=area is LimbHurtbox limb ? limb.Fighter : area.GetParent() as IDamageReceiver;
            if(target is not Node owner || ReferenceEquals(target,_soldier)||!_hitTargets.Add(owner.GetInstanceId()))continue;
            var hit=(kick?AttackHit.Kick:cross?AttackHit.Cross:AttackHit.Jab) with { Percentage=kick?KickDamage:cross?CrossDamage:JabDamage };
            target.ReceiveHit(hit,_soldier.Facing);
        }
    }
}
