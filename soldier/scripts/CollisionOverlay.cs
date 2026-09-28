using Godot;

namespace UnnamedFightingGame;

/// <summary>F4 displays the actual collision geometry, including query-only attack circles.</summary>
public partial class CollisionOverlay : Node2D
{
    public override void _Ready()
    {
        ZIndex = 4096;
        ProcessMode = ProcessModeEnum.Always;
        Visible = false;
    }

    public override void _UnhandledKeyInput(InputEvent input)
    {
        if (input is InputEventKey { Pressed: true, Echo: false, PhysicalKeycode: Key.F4 })
        {
            Visible = !Visible;
            QueueRedraw();
            GetViewport().SetInputAsHandled();
        }
    }

    public override void _Process(double delta)
    {
        if (Visible) QueueRedraw();
    }

    public override void _Draw()
    {
        DrawCollisions(GetParent());
        DrawSetTransformMatrix(Transform2D.Identity);
    }

    private void DrawCollisions(Node node)
    {
        if (node == this) return;
        var color = node.GetParent() is Area2D
            ? new Color(.3f, 1f, .5f, .32f)
            : node.GetParent() is StaticBody2D
                ? new Color(.2f, .7f, 1f, .5f)
                : new Color(1f, .8f, .15f, .35f);
        if (node is CollisionShape2D { Disabled: false, Shape: not null } shape)
        {
            DrawSetTransformMatrix(GlobalTransform.AffineInverse() * shape.GlobalTransform);
            shape.Shape.Draw(GetCanvasItem(), color);
        }
        else if (node is CollisionPolygon2D { Disabled: false } polygon && polygon.Polygon.Length >= 2)
        {
            DrawSetTransformMatrix(GlobalTransform.AffineInverse() * polygon.GlobalTransform);
            if (polygon.BuildMode == CollisionPolygon2D.BuildModeEnum.Solids && polygon.Polygon.Length >= 3)
                DrawColoredPolygon(polygon.Polygon, color);
            for (int i = 0; i < polygon.Polygon.Length; i++)
                DrawLine(polygon.Polygon[i], polygon.Polygon[(i + 1) % polygon.Polygon.Length], color, 1);
        }
        if (node is SoldierCombat combat && combat.ActiveSocket is { } socket)
        {
            DrawSetTransformMatrix(Transform2D.Identity);
            DrawCircle(ToLocal(socket.GlobalPosition), 9, new Color(1f, .15f, .15f, .65f));
        }
        foreach (Node child in node.GetChildren()) DrawCollisions(child);
    }
}
