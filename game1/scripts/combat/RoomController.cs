using Godot;

namespace Game1;

public enum RoomState { Loading, Combat, Cleared, Failed }

/// <summary>房间生命周期的唯一裁决点；HUD 只读取此状态，不直接猜测敌人或中继站节点。</summary>
public partial class RoomController : Node
{
    [Signal] public delegate void RoomClearedEventHandler();
    [Signal] public delegate void RoomFailedEventHandler();
    public RoomState State { get; private set; } = RoomState.Loading;

    public override void _Ready()
    {
        GetParent().GetNode<EnemyDirector>("EnemyDirector").AllWavesFinished += MarkCleared;
        GetParent().GetNode<RelayStation>("RelayStation").Destroyed += MarkFailed;
        State = RoomState.Combat;
    }
    private void MarkCleared() { if (State != RoomState.Combat) return; State = RoomState.Cleared; EmitSignal(SignalName.RoomCleared); }
    private void MarkFailed() { if (State != RoomState.Combat) return; State = RoomState.Failed; EmitSignal(SignalName.RoomFailed); }
}
