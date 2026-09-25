using ByteBrawl.Nodes;
using Godot;
using Xunit;

namespace ByteBrawl.Tests;

public class ChainGeometryTests
{
    [Fact] public void VerticalSegment_CenterBelowPivot()
    {
        // arm segment: size (3,7), pivot (1.5,0) — joint at the top
        var (center, _) = LimbRig.ChainGeometry(new Vector2(3, 7), new Vector2(1.5f, 0), 1f);
        Assert.Equal(new Vector2(0, 3.5f), center);
    }

    [Fact] public void HorizontalSegment_CenterRightOfPivot()
    {
        var (center, _) = LimbRig.ChainGeometry(new Vector2(8, 3), new Vector2(0, 1.5f), 1f);
        Assert.Equal(new Vector2(4, 0), center);
    }

    [Fact] public void ThinLimbs_HitRadiusFloor()
    {
        // placeholder limbs are ~3px thick → 1.5px radius, floored to 4
        var (_, radius) = LimbRig.ChainGeometry(new Vector2(3, 7), new Vector2(1.5f, 0), 1f);
        Assert.Equal(LimbRig.MinChainRadius, radius, 0.01f);
    }

    [Fact] public void ThickLimbs_RadiusFromThickness()
    {
        var (_, radius) = LimbRig.ChainGeometry(new Vector2(10, 10), new Vector2(5, 5), 1f);
        Assert.Equal(5f, radius, 0.01f);
    }

    [Fact] public void RadiusScale_MultipliesAboveFloor()
    {
        var (_, radius) = LimbRig.ChainGeometry(new Vector2(12, 12), new Vector2(6, 6), 0.75f);
        Assert.Equal(4.5f, radius, 0.01f);
    }
}
