using Godot;

namespace Game1;

public static class ProjectileMath
{
    // 使用镜面反射公式。方向与法线都由物理查询提供，因此先归一化，保证反射后速度只由武器数据决定。
    public static Vector2 Reflect(Vector2 direction, Vector2 normal)
    {
        return (direction - 2f * direction.Dot(normal) * normal).Normalized();
    }
}
