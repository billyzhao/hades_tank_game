namespace Game1;

/// <summary>职责层只决定想去哪里，寻路层仍统一负责抵达目标点。</summary>
public enum EnemyMovementMode
{
    Strafe,
    Pursuit,
    AggressivePursuit,
    StandOff
}
