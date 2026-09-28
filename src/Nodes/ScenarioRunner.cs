using System.Globalization;
using System.Text.Json;
using Godot;

namespace ByteBrawl.Nodes;

// Data-driven node-level test runner. A scenario JSON describes a frame
// timeline of injected inputs and teleports, plus declarative checks on the
// fighters. Runs the training arena headless; under Xvfb it can also save
// viewport screenshots at chosen frames.
//
//   xvfb-run -a godot --path . --audio-driver Dummy --rendering-driver opengl3 \
//     --script src/Nodes/ScenarioRunner.cs -- --scenario=tests/scenarios/arena.json
//
// Prints "SCENARIO PASS <name>" and exits 0 when every check holds; failures
// print a compact trace (only the failing frames). Timings are exact: inputs
// scheduled at frame f are injected before physics frame f runs, and checks
// at frame f sample the state after physics frame f.
public partial class ScenarioRunner : SceneTree
{
    private readonly record struct Sample(
        int Frame, bool Grounded, string State, float X, float Y, float Vx, float Vy, int WallDir);

    private sealed class Scenario
    {
        public string Name = "unnamed";
        public int Frames;
        public bool Training = true;
        public readonly List<(int At, int Player, string Key, bool Down)> Inputs = new();
        public readonly List<(int At, int Player, float X, float Y)> Teleports = new();
        public readonly List<(int At, string Out)> Captures = new();
        public readonly List<Check> Checks = new();
        public int TraceEvery, TraceFrom, TraceTo, TracePlayer = 1;
    }

    private sealed class Check
    {
        public string Type = "at"; // "at" (one frame) or "during" (window)
        public int Frame, From, To, Player = 1;
        public string Must = "all"; // during: "all" matching frames must pass, or "any"
        public Condition Where = new(); // during: filter frames
        public Condition Expect = new();
    }

    private sealed class Condition
    {
        public bool? Grounded;
        public string? State;
        public int? WallDir;
        public float? XLt, XLte, XGt, XGte, YLt, YLte, YGt, YGte, VxLt, VxLte, VxGt, VxGte, VyLt, VyLte, VyGt, VyGte;
        public float? XNear, YNear, Tol;
        public float[]? XBetween, YBetween;
    }

    private Arena _arena = null!;
    private Scenario _scenario = null!;

    public override void _Initialize()
    {
        string? path = null;
        foreach (var arg in OS.GetCmdlineUserArgs())
            if (arg.StartsWith("--scenario=")) path = arg["--scenario=".Length..];
        if (path is null) { GD.Print("usage: --scenario=<json path>"); Quit(2); return; }
        _scenario = Load(ProjectSettings.GlobalizePath(path));
        _arena = new Arena { Training = _scenario.Training };
        Root.AddChild(_arena);
        Run();
    }

    private Fighter Fighter(int player) => player == 2 ? _arena.P2 : _arena.P1;

    private static Sample Take(int frame, Fighter f) => new(
        frame, f.IsOnFloor(), f.Fsm.CurrentState.ToString(),
        f.Position.X, f.Position.Y, f.Velocity.X, f.Velocity.Y, f.WallDirection);

    private static string Fmt(float v) => v.ToString("0.##", CultureInfo.InvariantCulture);

    private static string? FirstMismatch(Condition c, in Sample s)
    {
        string Fail(string what, object actual) => $"{what} (actual {actual})";
        if (c.Grounded is { } gd && s.Grounded != gd) return Fail($"grounded=={gd}", s.Grounded);
        if (c.State is { } st && s.State != st) return Fail($"state=={st}", s.State);
        if (c.WallDir is { } wd && s.WallDir != wd) return Fail($"wallDir=={wd}", s.WallDir);
        if (c.XNear is { } xn && Math.Abs(s.X - xn) > (c.Tol ?? 5f)) return Fail($"x≈{xn}±{c.Tol ?? 5f}", Fmt(s.X));
        if (c.YNear is { } yn && Math.Abs(s.Y - yn) > (c.Tol ?? 5f)) return Fail($"y≈{yn}±{c.Tol ?? 5f}", Fmt(s.Y));
        if (c.XLt is { } v && !(s.X < v)) return Fail($"x<{Fmt(v)}", Fmt(s.X));
        if (c.XLte is { } v1 && !(s.X <= v1)) return Fail($"x<={Fmt(v1)}", Fmt(s.X));
        if (c.XGt is { } v2 && !(s.X > v2)) return Fail($"x>{Fmt(v2)}", Fmt(s.X));
        if (c.XGte is { } v3 && !(s.X >= v3)) return Fail($"x>={Fmt(v3)}", Fmt(s.X));
        if (c.YLt is { } v4 && !(s.Y < v4)) return Fail($"y<{Fmt(v4)}", Fmt(s.Y));
        if (c.YLte is { } v5 && !(s.Y <= v5)) return Fail($"y<={Fmt(v5)}", Fmt(s.Y));
        if (c.YGt is { } v6 && !(s.Y > v6)) return Fail($"y>{Fmt(v6)}", Fmt(s.Y));
        if (c.YGte is { } v7 && !(s.Y >= v7)) return Fail($"y>={Fmt(v7)}", Fmt(s.Y));
        if (c.VxLt is { } v8 && !(s.Vx < v8)) return Fail($"vx<{Fmt(v8)}", Fmt(s.Vx));
        if (c.VxLte is { } v9 && !(s.Vx <= v9)) return Fail($"vx<={Fmt(v9)}", Fmt(s.Vx));
        if (c.VxGt is { } va && !(s.Vx > va)) return Fail($"vx>{Fmt(va)}", Fmt(s.Vx));
        if (c.VxGte is { } vb && !(s.Vx >= vb)) return Fail($"vx>={Fmt(vb)}", Fmt(s.Vx));
        if (c.VyLt is { } vc && !(s.Vy < vc)) return Fail($"vy<{Fmt(vc)}", Fmt(s.Vy));
        if (c.VyLte is { } vd && !(s.Vy <= vd)) return Fail($"vy<={Fmt(vd)}", Fmt(s.Vy));
        if (c.VyGt is { } ve && !(s.Vy > ve)) return Fail($"vy>{Fmt(ve)}", Fmt(s.Vy));
        if (c.VyGte is { } vf && !(s.Vy >= vf)) return Fail($"vy>={Fmt(vf)}", Fmt(s.Vy));
        if (c.XBetween is { } xb && !(s.X >= xb[0] && s.X <= xb[1])) return Fail($"x in [{Fmt(xb[0])},{Fmt(xb[1])}]", Fmt(s.X));
        if (c.YBetween is { } yb && !(s.Y >= yb[0] && s.Y <= yb[1])) return Fail($"y in [{Fmt(yb[0])},{Fmt(yb[1])}]", Fmt(s.Y));
        return null;
    }

    private async void Run()
    {
        var failures = new List<string>();
        var duringSamples = new Dictionary<Check, List<Sample>>();
        foreach (var c in _scenario.Checks)
            if (c.Type == "during") duringSamples[c] = new List<Sample>();

        // First emission = pre-physics 1. Each loop iteration then brackets
        // exactly one physics frame: actions are injected pre-physics f, the
        // await resumes at pre-physics f+1 (physics f done), checks sample the
        // post-physics-f state. PhysicsFrame is emitted once per physics step;
        // idle frames without a physics tick emit nothing, so this stays exact
        // at any frame rate.
        await ToSignal(this, SignalName.PhysicsFrame);
        for (var f = 1; f <= _scenario.Frames; f++)
        {
            foreach (var (at, player, key, down) in _scenario.Inputs.Where(i => i.At == f))
                Inject(player, key, down);
            foreach (var (at, player, x, y) in _scenario.Teleports.Where(t => t.At == f))
                Fighter(player).Respawn(new Vector2(x, y), 0);
            foreach (var (at, outp) in _scenario.Captures.Where(c => c.At == f))
                CaptureSoon(outp);

            await ToSignal(this, SignalName.PhysicsFrame);

            foreach (var c in _scenario.Checks)
            {
                if (c.Player != 1 && c.Player != 2) continue;
                if (c.Type == "at" && c.Frame == f)
                {
                    var sample = Take(f, Fighter(c.Player));
                    var miss = FirstMismatch(c.Expect, sample);
                    if (miss != null)
                        failures.Add($"frame {f} P{c.Player}: {miss} | sample ({Fmt(sample.X)},{Fmt(sample.Y)}) v=({Fmt(sample.Vx)},{Fmt(sample.Vy)}) grounded={sample.Grounded} state={sample.State} physicsFrames={Engine.GetPhysicsFrames()} processFrames={Engine.GetProcessFrames()}");
                }
                else if (c.Type == "during" && f >= c.From && f <= c.To)
                {
                    var sample = Take(f, Fighter(c.Player));
                    if (FirstMismatch(c.Where, sample) == null)
                        duringSamples[c].Add(sample);
                }
            }

            if (_scenario.TraceEvery > 0 && f >= _scenario.TraceFrom && f <= _scenario.TraceTo
                && f % _scenario.TraceEvery == 0)
            {
                var s = Take(f, Fighter(_scenario.TracePlayer));
                GD.Print($"trace f={f} P{_scenario.TracePlayer}: ({Fmt(s.X)},{Fmt(s.Y)}) v=({Fmt(s.Vx)},{Fmt(s.Vy)}) grounded={s.Grounded} state={s.State} wallDir={s.WallDir}");
            }
        }

        foreach (var (c, samples) in duringSamples)
        {
            var label = $"frames {c.From}..{c.To} P{c.Player}";
            if (samples.Count == 0)
            { failures.Add($"{label}: no frames matched the where-conditions"); continue; }
            var bad = samples.Where(s => FirstMismatch(c.Expect, s) != null).ToList();
            if (c.Must == "all" && bad.Count > 0)
                failures.Add($"{label}: {bad.Count}/{samples.Count} frames failed expect, e.g. " +
                    string.Join(", ", bad.Take(12).Select(s => $"{s.Frame}:({Fmt(s.X)},{Fmt(s.Y)},{Fmt(s.Vx)},{Fmt(s.Vy)})")));
            if (c.Must == "any" && bad.Count == samples.Count)
                failures.Add($"{label}: no frame satisfied expect");
        }

        if (failures.Count == 0)
        {
            GD.Print($"SCENARIO PASS {_scenario.Name}");
            Quit(0);
        }
        else
        {
            foreach (var failure in failures) GD.Print($"SCENARIO FAIL {_scenario.Name}: {failure}");
            Quit(1);
        }
    }

    // Detached on purpose: awaiting the draw inside the main loop would stall
    // it and shift every subsequent timing in the scenario. The next physics
    // frame after scheduling means physics frame f has just completed; the
    // following draw presents it.
    private async void CaptureSoon(string outPath)
    {
        await ToSignal(this, SignalName.PhysicsFrame);
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        Root.GetViewport().GetTexture().GetImage().SavePng(outPath);
    }

    private static void Inject(int player, string keyName, bool down)
    {
        if (!Enum.TryParse<Key>(keyName, out var key))
            throw new ArgumentException($"unknown key '{keyName}' in scenario");
        Input.ParseInputEvent(new InputEventKey { PhysicalKeycode = key, Pressed = down });
    }

    // ---- scenario loading ----

    private static Scenario Load(string path)
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        var root = doc.RootElement;
        var sc = new Scenario
        {
            Name = root.GetProperty("name").GetString() ?? "unnamed",
            Frames = root.GetProperty("frames").GetInt32(),
        };
        if (root.TryGetProperty("training", out var tr)) sc.Training = tr.GetBoolean();

        foreach (var e in root.GetProperty("input").EnumerateArray())
            sc.Inputs.Add((e.GetProperty("at").GetInt32(),
                e.TryGetProperty("player", out var p) ? p.GetInt32() : 1,
                e.GetProperty("key").GetString()!, e.TryGetProperty("down", out var d) ? d.GetBoolean() : true));

        foreach (var e in root.GetProperty("teleport").EnumerateArray())
        {
            var to = e.GetProperty("to").EnumerateArray().ToArray();
            sc.Teleports.Add((e.GetProperty("at").GetInt32(),
                e.TryGetProperty("player", out var p) ? p.GetInt32() : 1,
                to[0].GetSingle(), to[1].GetSingle()));
        }

        foreach (var e in root.GetProperty("capture").EnumerateArray())
            sc.Captures.Add((e.GetProperty("at").GetInt32(), e.GetProperty("out").GetString()!));

        if (root.TryGetProperty("trace", out var trace))
        {
            sc.TraceEvery = trace.GetProperty("every").GetInt32();
            sc.TraceFrom = trace.GetProperty("from").GetInt32();
            sc.TraceTo = trace.GetProperty("to").GetInt32();
            if (trace.TryGetProperty("player", out var tp)) sc.TracePlayer = tp.GetInt32();
        }

        foreach (var e in root.GetProperty("checks").EnumerateArray())
        {
            var c = new Check
            {
                Type = e.GetProperty("type").GetString()!,
                Player = e.TryGetProperty("player", out var p) ? p.GetInt32() : 1,
                Must = e.TryGetProperty("must", out var m) ? m.GetString()! : "all",
            };
            if (c.Type == "at") c.Frame = e.GetProperty("frame").GetInt32();
            else { c.From = e.GetProperty("from").GetInt32(); c.To = e.GetProperty("to").GetInt32(); }
            if (e.TryGetProperty("where", out var w)) c.Where = ParseCondition(w);
            c.Expect = ParseCondition(e.GetProperty("expect"));
            sc.Checks.Add(c);
        }
        return sc;
    }

    private static Condition ParseCondition(JsonElement e)
    {
        var c = new Condition();
        foreach (var prop in e.EnumerateObject())
        {
            switch (prop.Name)
            {
                case "grounded": c.Grounded = prop.Value.GetBoolean(); break;
                case "state": c.State = prop.Value.GetString(); break;
                case "wallDir": c.WallDir = prop.Value.GetInt32(); break;
                case "xLt": c.XLt = prop.Value.GetSingle(); break;
                case "xLte": c.XLte = prop.Value.GetSingle(); break;
                case "xGt": c.XGt = prop.Value.GetSingle(); break;
                case "xGte": c.XGte = prop.Value.GetSingle(); break;
                case "yLt": c.YLt = prop.Value.GetSingle(); break;
                case "yLte": c.YLte = prop.Value.GetSingle(); break;
                case "yGt": c.YGt = prop.Value.GetSingle(); break;
                case "yGte": c.YGte = prop.Value.GetSingle(); break;
                case "vxLt": c.VxLt = prop.Value.GetSingle(); break;
                case "vxLte": c.VxLte = prop.Value.GetSingle(); break;
                case "vxGt": c.VxGt = prop.Value.GetSingle(); break;
                case "vxGte": c.VxGte = prop.Value.GetSingle(); break;
                case "vyLt": c.VyLt = prop.Value.GetSingle(); break;
                case "vyLte": c.VyLte = prop.Value.GetSingle(); break;
                case "vyGt": c.VyGt = prop.Value.GetSingle(); break;
                case "vyGte": c.VyGte = prop.Value.GetSingle(); break;
                case "xNear": c.XNear = prop.Value.GetSingle(); break;
                case "yNear": c.YNear = prop.Value.GetSingle(); break;
                case "tol": c.Tol = prop.Value.GetSingle(); break;
                case "xBetween": c.XBetween = new[] { prop.Value[0].GetSingle(), prop.Value[1].GetSingle() }; break;
                case "yBetween": c.YBetween = new[] { prop.Value[0].GetSingle(), prop.Value[1].GetSingle() }; break;
                default: throw new ArgumentException($"unknown condition field '{prop.Name}' in scenario");
            }
        }
        return c;
    }
}
