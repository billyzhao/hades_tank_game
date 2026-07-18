using System.Collections.Generic;
using Godot;

namespace Game1;

public interface IEnemyPathProvider
{
    IReadOnlyList<Vector2> GetWorldPath(Vector2 fromWorld, Vector2 toWorld);
}
