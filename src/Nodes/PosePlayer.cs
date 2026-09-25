using ByteBrawl.Combat;
using Godot;

namespace ByteBrawl.Nodes;

public partial class PosePlayer : Node
{
    [Export] public LimbRig Rig = null!;

    public void Play(FighterState state, int stateFrames, bool recovering = false)
    {
        var pose = PoseLibrary.For(state, stateFrames, recovering);
        foreach (var (name, degrees) in pose.Angles)
            Rig.Find(name).RotationDegrees = degrees;
    }
}
