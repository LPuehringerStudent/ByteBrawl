using ByteBrawl.Combat;
using Godot;
using Xunit;
namespace ByteBrawl.Tests;
public class FighterPhysicsTests
{
    [Fact] public void GroundStartsStopsImmediately()
    { Assert.Equal(120,FighterPhysics.Locomotion(0,1,120,true));Assert.Equal(0,FighterPhysics.Locomotion(120,0,120,true)); }
    [Fact] public void AirDriftsWithoutInputAndSteersWithInput()
    { Assert.Equal(300,FighterPhysics.Locomotion(300,0,120,false));Assert.Equal(-120,FighterPhysics.Locomotion(300,-1,120,false)); }
    [Fact] public void GravitySupportsAnOptionalTerminalSpeed()
    { Assert.Equal(800,FighterPhysics.GravityStep(790,800,.1,800));Assert.Equal(870,FighterPhysics.GravityStep(790,800,.1)); }
    [Fact] public void KnockbackUsesPreHitPercentageAndMirrorsHorizontalOnly()
    { var v=FighterPhysics.Knockback(100,2,50,new Vector2(1,-.5f),-1);Assert.Equal(new Vector2(-200,-100),v); }
}
