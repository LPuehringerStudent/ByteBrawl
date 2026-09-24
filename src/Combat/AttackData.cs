using Godot;

namespace ByteBrawl.Combat;

public class AttackData
{
    public string Id = "";
    public float BaseDamage;
    public float BaseKnockback;
    public float Scaling;
    public Vector2 Direction = Vector2.Zero;
    public int HitstunFrames;
    public int ActiveFrames;
    public HitboxShape Shape = HitboxShape.Circle;
    public float Radius = 6f;
    public ChargeConfig? Charge;
    public List<AttackStage> Stages = new();
}
