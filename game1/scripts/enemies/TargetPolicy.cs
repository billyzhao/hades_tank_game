namespace Game1;

public static class TargetPolicy
{
    /// <summary>巡逻与突击牵制玩家；攻城单位优先中继站，首选失效时才回退。</summary>
    public static TargetId SelectTarget(BehaviorId behavior, TargetSnapshot targets)
    {
        TargetId preferred = behavior == BehaviorId.Siege ? TargetId.Relay : TargetId.Player;
        if (preferred == TargetId.Player && targets.PlayerAvailable) return TargetId.Player;
        if (preferred == TargetId.Relay && targets.RelayAvailable) return TargetId.Relay;
        if (targets.PlayerAvailable) return TargetId.Player;
        return targets.RelayAvailable ? TargetId.Relay : TargetId.None;
    }
}
