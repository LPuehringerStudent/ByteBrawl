using ByteBrawl.Combat;
using Godot;
using Xunit;

namespace ByteBrawl.Tests;

public class PoseCollisionProfileTests
{
    private static PoseCollisionProfile Load() => PoseCollisionProfile.Parse(
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "soldier_collision.json")));

    [Fact]
    public void EveryAnimationFrameHasTheSameSeparateBodyPartCapsules()
    {
        var profile = Load();
        Assert.Equal(57, profile.FrameCount);
        var names = profile.Frame(0).Keys.Order().ToArray();
        Assert.Equal(15, names.Length);
        Assert.DoesNotContain("backpack", names);
        foreach (var name in new[] { "near_thigh", "near_shin", "far_thigh", "far_shin" })
            Assert.Contains(name, names);
        for (var frame = 0; frame < profile.FrameCount; frame++)
        {
            Assert.Equal(names, profile.Frame(frame).Keys.Order().ToArray());
            foreach (var part in profile.Frame(frame).Values)
            {
                Assert.True(part.Radius > 0);
                Assert.True(part.Height >= part.Radius * 2);
            }
        }
    }

    [Fact]
    public void CollisionPoseChangesWithAnimationWithoutChangingAnyArt()
    {
        var profile = Load();
        Assert.Equal("idle_0", profile.FrameId(0));
        Assert.NotEqual(profile.Frame(0)["near_thigh"].Center, profile.Frame(1)["near_thigh"].Center);
        Assert.Equal(LimbGroup.Leg, profile.Frame(0)["near_shin"].Group);
        Assert.Equal(LimbGroup.Leg, profile.Frame(0)["far_thigh"].Group);
        Assert.Equal(new Vector2(54, -82), profile.AttackAt(25)!.Center);
    }

    [Fact]
    public void AttackCirclesUseExistingImpactFramesAndDamage()
    {
        var profile = Load();
        Assert.Equal(new[] { 25, 32, 33, 47, 48, 49 }, profile.Attacks.Select(a => a.Frame).Order().ToArray());
        Assert.Equal(("jab", 2), (profile.AttackAt(25)!.Attack, profile.AttackAt(25)!.Damage));
        Assert.Equal(("cross", 3), (profile.AttackAt(32)!.Attack, profile.AttackAt(32)!.Damage));
        Assert.Equal(("kick", 4), (profile.AttackAt(47)!.Attack, profile.AttackAt(47)!.Damage));
        Assert.All(profile.Attacks, attack => Assert.Equal(9, attack.Radius));
        Assert.Null(profile.AttackAt(26));
    }
}
