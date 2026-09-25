using Godot;
using System;

namespace ByteBrawl.Combat;

/// <summary>Shared numeric physics from Fighter/FighterStateMachine/MatchRules; no node dependencies.</summary>
public static class FighterPhysics
{
    public static float GravityStep(float velocityY, float gravity, double delta, float terminalSpeed = float.PositiveInfinity)
        => Math.Min(velocityY + gravity * (float)delta, terminalSpeed);

    // Ground: immediate start/stop. Air: directional steering, retain momentum when neutral.
    public static float Locomotion(float velocityX, float axis, float runSpeed, bool grounded)
        => axis != 0 ? axis * runSpeed : grounded ? 0 : velocityX;

    public static Vector2 Knockback(float baseSpeed, float scaling, float damageBeforeHit, Vector2 direction, float facing)
    {
        float speed = baseSpeed + damageBeforeHit * scaling;
        return new Vector2(direction.X * (facing < 0 ? -1 : 1), direction.Y) * speed;
    }
}
