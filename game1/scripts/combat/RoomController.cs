using Godot;

namespace Game1;

public enum RoomState { Loading, Combat, Cleared, Failed }

/// <summary>房间生命周期的旧迁移适配器；失败统一由 RunController 根据玩家报废裁决。</summary>
public partial class RoomController : Node
{
    [Signal] public delegate void RoomClearedEventHandler();
    [Signal] public delegate void RoomFailedEventHandler();
    public RoomState State { get; private set; } = RoomState.Loading;

    public override void _Ready()
    {
        GetParent().GetNode<EnemyDirector>("EnemyDirector").AllWavesFinished += MarkCleared;
        State = RoomState.Combat;
    }
    private void MarkCleared() { if (State != RoomState.Combat) return; State = RoomState.Cleared; EmitSignal(SignalName.RoomCleared); }
}
