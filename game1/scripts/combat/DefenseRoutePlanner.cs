using Godot;

namespace Game1;

/// <summary>
/// MVP 灰盒战场的轻量导航：中央钢墙与砖墙形成一条防线，
/// 右侧来敌先走上/下通路，再进入目标区域，避免直接顶住障碍物。
/// 后续替换为 TileMap A* 时保留此接口即可。
/// </summary>
public static class DefenseRoutePlanner
{
    private const float RouteGateX = 230f;
    private const float CenterY = 135f;
    private static readonly Vector2 UpperLane = new(230f, 72f);
    private static readonly Vector2 LowerLane = new(230f, 198f);

    public static Vector2 GetNextPoint(Vector2 current, Vector2 target)
    {
        if (current.X <= RouteGateX)
        {
            return target;
        }

        return current.Y < CenterY ? UpperLane : LowerLane;
    }
}
