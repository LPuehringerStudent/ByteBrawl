using ByteBrawl.Combat;
using Godot;

namespace ByteBrawl.Nodes;

public static class PoseLibrary
{
    public static Pose For(FighterState state, int stateFrames)
    {
        var p = new Pose();
        switch (state)
        {
            case FighterState.Run:
                var swing = Mathf.Sin(stateFrames * 0.3f) * 40f;
                p.Angles["NearLeg"] = swing;
                p.Angles["NearArm"] = -swing;
                p.Angles["Torso"] = 5f;
                break;
            case FighterState.Jump:
                p.Angles["NearLeg"] = -25f;
                p.Angles["NearArm"] = -40f;
                break;
            case FighterState.Fall:
                p.Angles["NearArm"] = -70f;
                break;
            case FighterState.LightAttack:
            case FighterState.HeavyAttack:
            case FighterState.Special:
                p.Angles["NearArm"] = -90f;
                p.Angles["Torso"] = -10f;
                break;
            case FighterState.Charging:
                p.Angles["NearArm"] = -45f;
                break;
            case FighterState.Shield:
                p.Angles["NearArm"] = -60f;
                break;
            default:
                p.Angles["NearLeg"] = 0f;
                p.Angles["NearArm"] = 0f;
                break;
        }
        return p;
    }
}
