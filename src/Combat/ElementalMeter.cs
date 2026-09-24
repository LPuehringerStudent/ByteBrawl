namespace ByteBrawl.Combat;

public class ElementalMeter
{
    public const float Max = 100f;
    public float Value { get; private set; }

    public void AddFromDealt(float damage) => Add(damage * 0.6f);
    public void AddFromTaken(float damage) => Add(damage * 0.4f);

    private void Add(float amount) => Value = Math.Min(Max, Value + amount);
}
