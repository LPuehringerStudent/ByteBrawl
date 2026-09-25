using ByteBrawl.Combat;
using ByteBrawl.Data;
using Godot;
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

    [Fact]
    public void ByteUpHeavy_IsSjpStyleRecovery()
    {
        var upHeavy = ByteMoveset.Create().Get(AttackSlot.UpHeavy);
        Assert.NotNull(upHeavy.Recovery);
        Assert.NotNull(upHeavy.Charge); // grounded up-heavy charges; the air recovery doesn't
        Assert.Equal(380, upHeavy.Recovery!.VerticalBoost, 0.01f);
        Assert.False(upHeavy.Recovery.CanActAfter);
        Assert.Equal(4, upHeavy.Stages.Count);
        // strong first hit, two carrying hits (no scaling), launcher last
        Assert.True(upHeavy.Stages[0].BaseKnockback > upHeavy.Stages[1].BaseKnockback);
        Assert.Equal(0, upHeavy.Stages[1].Scaling);
        Assert.Equal(0, upHeavy.Stages[2].Scaling);
        Assert.True(upHeavy.Stages[3].BaseKnockback > upHeavy.Stages[1].BaseKnockback);
        // hyper armor on the arm while rising (stages 1-3)
        for (var i = 0; i < 3; i++)
        {
            var armor = Assert.Single(upHeavy.Stages[i].Armor);
            Assert.Equal(LimbGroup.Arm, armor.Group);
            Assert.Equal(HurtboxType.HyperArmor, armor.Type);
        }
        Assert.Empty(upHeavy.Stages[3].Armor);
    }
}
