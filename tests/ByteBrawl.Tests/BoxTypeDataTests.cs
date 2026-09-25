using ByteBrawl.Combat;
using Godot;
using Xunit;

namespace ByteBrawl.Tests;

public class BoxTypeDataTests
{
    [Fact] public void HitboxSpec_DefaultsToDamageType()
    {
        var spec = new HitboxSpec();
        Assert.Equal(HitboxType.Damage, spec.Type);
        Assert.Equal(Vector2.Zero, spec.Push);
        Assert.Null(spec.Grab);
    }

    [Fact] public void GrabData_DefaultsAreValid()
    {
        Assert.True(new GrabData().IsValid());
    }

    [Fact] public void GrabData_InvalidWhenHoldFramesNegative()
    {
        Assert.False(new GrabData { HoldFrames = -1 }.IsValid());
    }

    [Fact] public void GrabData_InvalidWhenThrowInvalid()
    {
        Assert.False(new GrabData { ThrowForward = new ThrowData { Damage = -1 } }.IsValid());
    }

    [Fact] public void AttackData_HasEmptyArmorListByDefault()
    {
        Assert.Empty(new AttackData().Armor);
    }
}
