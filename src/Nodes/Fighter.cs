using ByteBrawl.Combat;
using ByteBrawl.Data;
using Godot;

namespace ByteBrawl.Nodes;

public partial class Fighter : CharacterBody2D, IFighter
{
    [Export] public string MovesetName = "byte";
    [Export] public int PlayerIndex = 1;
    public FighterStateMachine Fsm { get; private set; } = null!;
    public ElementalMeter Meter { get; } = new();
    public LimbRig Rig => _rig;

    private IHitboxManager _hitboxes = null!;
    private LimbRig _rig = null!;
    private bool _chargingFull;

    public float Damage { get; set; }
    public int Stocks { get; set; } = 3;
    private int _facing = 1;
    public int Facing { get => _facing; set => _facing = value; }
    public bool IsGrounded => IsOnFloor();
    public int HitstunFrames { get; set; }
    public int InvincibleFrames { get; set; }
    public bool ShieldActive { get; set; }
    public float ShieldHealth { get; set; } = 100;
    public IElementalFrenzy? Frenzy { get; set; }

    public override void _Ready()
    {
        var bodyShape = new CollisionShape2D { Shape = new RectangleShape2D { Size = new Vector2(12, 20) } };
        AddChild(bodyShape);
        _rig = LimbRig.CreatePlaceholder();
        if (PlayerIndex != 1) _rig.Modulate = new Color(1f, 0.35f, 0.7f); // P2 / dummy tint
        AddChild(_rig);
        var posePlayer = new PosePlayer { Rig = _rig, Name = "PosePlayer" };
        AddChild(posePlayer);
        foreach (var hurtbox in _rig.Hurtboxes)
            hurtbox.OwnerFighter = this;
        _hitboxes = GetParent().GetNode<HitboxManager>("HitboxManager");
        Fsm = new FighterStateMachine(this, _hitboxes, ByteMoveset.Create());
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!IsOnFloor()) Velocity = new Vector2(Velocity.X, Velocity.Y + 800f * (float)delta);
        if (HitstunFrames > 0) HitstunFrames--;
        if (InvincibleFrames > 0) InvincibleFrames--;
        Fsm.Update(LocalInput.Capture(PlayerIndex));
        MoveAndSlide();
        // mirror to face left/right; 0.65 fits the ~33px rig into the 20px-tall collision box
        _rig.Scale = new Vector2(Facing * 0.65f, 0.65f);
        GetNode<PosePlayer>("PosePlayer").Play(Fsm.CurrentState, Fsm.StateFrames);
    }

    public void TakeDamage(float amount) => Damage += amount;
    public void ApplyKnockback(Vector2 v) => Velocity = v;
    public void EnterHitstun(int frames) => HitstunFrames = frames;
    public void EnterInvincibility(int frames) => InvincibleFrames = frames;
    public void DamageShield(float amount) { ShieldHealth -= amount; if (ShieldHealth <= 0) { ShieldHealth = 0; DeactivateShield(); } }
    public void DeactivateShield() => ShieldActive = false;
    public void LoseStock() { Stocks -= 1; Damage = 0; }
    public void Respawn(Vector2 position, int invincibilityFrames)
    {
        Position = position;
        Velocity = Vector2.Zero;
        Damage = 0;
        HitstunFrames = 0;
        EnterInvincibility(invincibilityFrames);
    }
    public void SetChargingFull(bool value) => _chargingFull = value;
}
