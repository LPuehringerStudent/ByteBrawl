using ByteBrawl.Nodes;
using Godot;
using Xunit;

namespace ByteBrawl.Tests;

public class ChainGeometryTests
{
    [Fact] public void VerticalSegment_CenterBelowPivot()
    {
        // arm segment: size (3,7), pivot (1.5,0) — joint at the top
        var (center, radius) = LimbRig.ChainGeometry(new Vector2(3, 7), new Vector2(1.5f, 0), 1f);
        Assert.Equal(new Vector2(0, 3.5f), center);
        Assert.Equal(1.5f, radius, 0.01f);
    }

    [Fact] public void HorizontalSegment_UsesThickness()
    {
        var (center, radius) = LimbRig.ChainGeometry(new Vector2(8, 3), new Vector2(0, 1.5f), 1f);
        Assert.Equal(new Vector2(4, 0), center);
        Assert.Equal(1.5f, radius, 0.01f);
    }

    [Fact] public void RadiusScale_Multiplies()
    {
        var (_, radius) = LimbRig.ChainGeometry(new Vector2(3, 7), new Vector2(1.5f, 0), 0.75f);
        Assert.Equal(1.125f, radius, 0.01f);
    }
}
