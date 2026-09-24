namespace ByteBrawl.Nodes;

using LimbAngles = System.Collections.Generic.Dictionary<string, float>;

// A pose is a set of limb rotations in degrees.
public class Pose
{
    public LimbAngles Angles = new();
}
