using ByteBrawl.Combat;
using ByteBrawl.Data;
using Xunit;

namespace ByteBrawl.Tests;

public class CombatDataTests
{
    [Fact]
    public void ByteMoveset_FillsAllSixteenSlots()
    {
        var moveset = ByteMoveset.Create();
        foreach (AttackSlot slot in Enum.GetValues<AttackSlot>())
            Assert.True(moveset.Attacks.ContainsKey(slot), $"missing {slot}");
    }

    [Fact]
    public void ByteSpecial_IsThreeHitChain_WithFinisherLast()
    {
        var stages = ByteMoveset.Create().Get(AttackSlot.NeutralSpecial).Stages;
        Assert.Equal(3, stages.Count);
        Assert.Equal(0, stages[0].Scaling);
        Assert.Equal(0, stages[1].Scaling);
        Assert.True(stages[2].BaseKnockback > 0);
        Assert.True(stages[2].Scaling > 0);
        Assert.True(stages[1].SpawnFrame > stages[0].SpawnFrame);
        Assert.True(stages[2].SpawnFrame > stages[1].SpawnFrame);
    }

    [Fact]
    public void ByteHeavy_IsChargeable()
    {
        var charge = ByteMoveset.Create().Get(AttackSlot.NeutralHeavy).Charge;
        Assert.NotNull(charge);
        Assert.Equal(180, charge!.MaxChargeFrames);
        Assert.Equal(180, charge.MaxHoldFrames);
    }

    [Fact]
    public void ByteHeavy_ArmsHyperArmorDuringActiveFrames()
    {
        var heavy = ByteMoveset.Create().Get(AttackSlot.NeutralHeavy);
        var armor = Assert.Single(heavy.Armor);
        Assert.Equal(LimbGroup.Arm, armor.Group);
        Assert.Equal(HurtboxType.HyperArmor, armor.Type);
        Assert.Null(armor.BreakKbThreshold);
    }
}
