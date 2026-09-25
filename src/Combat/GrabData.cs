namespace ByteBrawl.Combat;

// Grab/throw data model — designed and validated, but the grab subsystem is
// NOT built yet: Grab-typed hitboxes render in debug and do nothing.
public class GrabData
{
    public int HoldFrames = 30;
    public float PummelDamage = 2;
    public int PummelInterval = 20;
    public float MashOutPerInput = 2;
    public int MashOutMax = 20;
    public ThrowData ThrowUp = new() { Angle = -90 };
    public ThrowData ThrowForward = new();
    public ThrowData ThrowDown = new() { Angle = 90 };

    public bool IsValid() =>
        HoldFrames >= 0 && PummelDamage >= 0 && PummelInterval > 0 &&
        MashOutPerInput >= 0 && MashOutMax >= 0 &&
        ThrowUp.IsValid() && ThrowForward.IsValid() && ThrowDown.IsValid();
}

public class ThrowData
{
    public float Damage = 6;
    public float BaseKnockback = 120;
    public float Scaling = 1f;
    public float Angle; // degrees, 0 = forward, negative = up

    public bool IsValid() => Damage >= 0 && BaseKnockback >= 0 && Scaling >= 0;
}
