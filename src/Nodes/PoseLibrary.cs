using ByteBrawl.Combat;
using Godot;

namespace ByteBrawl.Nodes;

public static class PoseLibrary
{
    public static Pose For(FighterState state, int stateFrames, bool recovering = false)
    {
        var p = new Pose();
        switch (state)
        {
            case FighterState.HeavyAttack when recovering:
                // Rising uppercut: arm straight up, body stretched (SJP-style).
                p.Angles["NearUpperArm"] = -170f;
                p.Angles["NearForearm"] = -10f;
                p.Angles["FarUpperArm"] = -40f;
                p.Angles["FarForearm"] = -20f;
                p.Angles["Torso"] = -14f;
                break;
            case FighterState.Run:
                var swing = Mathf.Sin(stateFrames * 0.3f) * 40f;
                p.Angles["NearThigh"] = swing;
                p.Angles["FarThigh"] = -swing;
                p.Angles["NearShin"] = Mathf.Max(0, -swing * 0.6f);
                p.Angles["FarShin"] = Mathf.Max(0, swing * 0.6f);
                p.Angles["NearUpperArm"] = -swing * 0.7f;
                p.Angles["FarUpperArm"] = swing * 0.7f;
                p.Angles["Torso"] = 5f;
                break;
            case FighterState.Jump:
                p.Angles["NearThigh"] = -30f;
                p.Angles["FarThigh"] = -15f;
                p.Angles["NearShin"] = 25f;
                p.Angles["NearUpperArm"] = -50f;
                p.Angles["FarUpperArm"] = -40f;
                break;
            case FighterState.Fall:
                p.Angles["NearUpperArm"] = -80f;
                p.Angles["FarUpperArm"] = -70f;
                p.Angles["NearForearm"] = -20f;
                p.Angles["FarForearm"] = -20f;
                p.Angles["NearThigh"] = 15f;
                p.Angles["FarThigh"] = -10f;
                break;
            case FighterState.LightAttack:
            case FighterState.HeavyAttack:
            case FighterState.Special:
                p.Angles["NearUpperArm"] = -85f;
                p.Angles["NearForearm"] = -15f;
                p.Angles["Torso"] = -8f;
                break;
            case FighterState.Charging:
                p.Angles["NearUpperArm"] = -40f;
                p.Angles["NearForearm"] = -70f;
                p.Angles["FarUpperArm"] = -20f;
                break;
            case FighterState.Shield:
                p.Angles["NearUpperArm"] = -60f;
                p.Angles["NearForearm"] = -50f;
                p.Angles["FarUpperArm"] = -50f;
                p.Angles["FarForearm"] = -40f;
                break;
            case FighterState.Hitstun:
                p.Angles["Torso"] = -12f;
                p.Angles["NearUpperArm"] = -30f;
                p.Angles["FarUpperArm"] = -20f;
                break;
            default: // Idle
                p.Angles["NearUpperArm"] = 4f;
                p.Angles["FarUpperArm"] = -4f;
                break;
        }
        return p;
    }
}
