using System;
using Godot;

namespace Game1;

public readonly record struct EnemyMovementIntent(Vector2 Destination, bool ShouldMove);

/// <summary>不依赖场景树的职责意图计算，便于测试且不复制共享 A*。</summary>
public static class EnemyMovementPolicy
{
    public static EnemyMovementIntent Calculate(
        EnemyMovementMode mode,
        Vector2 self,
        Vector2 target,
        float attackRange,
        float retreatRange,
        float elapsedSeconds)
    {
        if (!float.IsFinite(attackRange) || attackRange <= 0f)
            throw new ArgumentOutOfRangeException(nameof(attackRange));
        if (!float.IsFinite(retreatRange) || retreatRange < 0f || retreatRange >= attackRange)
            throw new ArgumentOutOfRangeException(nameof(retreatRange));

        Vector2 toTarget = self.DirectionTo(target);
        float distance = self.DistanceTo(target);
        return mode switch
        {
            EnemyMovementMode.Strafe => CalculateStrafe(target, toTarget, attackRange, elapsedSeconds),
            EnemyMovementMode.StandOff => CalculateStandOff(self, target, toTarget, distance, attackRange, retreatRange),
            EnemyMovementMode.Pursuit or EnemyMovementMode.AggressivePursuit =>
                new EnemyMovementIntent(target, distance > attackRange * 0.78f),
            _ => throw new ArgumentOutOfRangeException(nameof(mode))
        };
    }

    private static EnemyMovementIntent CalculateStrafe(
        Vector2 target,
        Vector2 toTarget,
        float attackRange,
        float elapsedSeconds)
    {
        Vector2 tangent = new(-toTarget.Y, toTarget.X);
        if (((int)MathF.Floor(elapsedSeconds / 2f) & 1) == 1) tangent = -tangent;
        Vector2 orbitPoint = target - toTarget * attackRange * 0.72f + tangent * attackRange * 0.58f;
        return new EnemyMovementIntent(orbitPoint, true);
    }

    private static EnemyMovementIntent CalculateStandOff(
        Vector2 self,
        Vector2 target,
        Vector2 toTarget,
        float distance,
        float attackRange,
        float retreatRange)
    {
        if (distance < retreatRange)
        {
            Vector2 away = target.DirectionTo(self);
            if (away.IsZeroApprox()) away = Vector2.Left;
            return new EnemyMovementIntent(self + away * retreatRange, true);
        }
        if (distance <= attackRange * 0.92f) return new EnemyMovementIntent(self, false);
        return new EnemyMovementIntent(target, true);
    }
}
