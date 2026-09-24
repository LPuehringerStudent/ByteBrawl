using ByteBrawl.Combat;
using Godot;

namespace ByteBrawl.Nodes;

public static class LocalInput
{
    private static readonly Dictionary<int, HashSet<Key>> Prev = new();

    private static readonly Dictionary<string, Key> P1 = new()
    {
        ["left"] = Key.A, ["right"] = Key.D, ["up"] = Key.W, ["down"] = Key.S,
        ["jump"] = Key.Space, ["light"] = Key.J, ["heavy"] = Key.K, ["special"] = Key.H,
        ["gadget"] = Key.C, ["shield"] = Key.Shift, ["grab"] = Key.L,
    };

    private static readonly Dictionary<string, Key> P2 = new()
    {
        ["left"] = Key.Left, ["right"] = Key.Right, ["up"] = Key.Up, ["down"] = Key.Down,
        ["jump"] = Key.Kp5, ["light"] = Key.Kp1, ["heavy"] = Key.Kp2, ["special"] = Key.Kp0,
        ["gadget"] = Key.Kp4, ["shield"] = Key.Kp3, ["grab"] = Key.Kp6,
    };

    public static ActionFrame Capture(int player)
    {
        if (player != 1 && player != 2) return new ActionFrame(); // dummy
        var map = player == 1 ? P1 : P2;
        var prev = Prev.TryGetValue(player, out var p) ? p : new HashSet<Key>();

        ActionFrame frame = new()
        {
            MoveX = (IsDown(map, "right") ? 1 : 0) - (IsDown(map, "left") ? 1 : 0),
            MoveY = (IsDown(map, "down") ? 1 : 0) - (IsDown(map, "up") ? 1 : 0),
            JumpPressed = Pressed(map, prev, "jump"), JumpHeld = IsDown(map, "jump"),
            AttackLight = Pressed(map, prev, "light"), AttackLightHeld = IsDown(map, "light"),
            AttackHeavy = Pressed(map, prev, "heavy"), AttackHeavyHeld = IsDown(map, "heavy"),
            AttackSpecial = Pressed(map, prev, "special"), AttackSpecialHeld = IsDown(map, "special"),
            GadgetPressed = Pressed(map, prev, "gadget"), GadgetHeld = IsDown(map, "gadget"),
            ShieldPressed = Pressed(map, prev, "shield"), ShieldHeld = IsDown(map, "shield"),
            GrabPressed = Pressed(map, prev, "grab"), GrabHeld = IsDown(map, "grab"),
        };

        Prev[player] = map.Where(kv => Input.IsPhysicalKeyPressed(kv.Value)).Select(kv => kv.Value).ToHashSet();
        return frame;
    }

    private static bool IsDown(Dictionary<string, Key> map, string name) => Input.IsPhysicalKeyPressed(map[name]);
    private static bool Pressed(Dictionary<string, Key> map, HashSet<Key> prev, string name)
    {
        var key = map[name];
        return Input.IsPhysicalKeyPressed(key) && !prev.Contains(key);
    }
}
