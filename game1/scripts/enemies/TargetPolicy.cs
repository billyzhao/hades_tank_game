namespace Game1;

public static class TargetPolicy
{
    /// <summary>Alpha 02B 的所有现役敌军只以玩家坦克为战斗目标。</summary>
    public static TargetId SelectTarget(BehaviorId behavior, TargetSnapshot targets) =>
        targets.PlayerAvailable ? TargetId.Player : TargetId.None;
}
