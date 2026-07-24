namespace Game1;

/// <summary>一个移动核心下的软构筑方向；标签只影响识别和权重，不锁定奖励池。</summary>
public sealed record BuildRouteDefinition(
    CoreId CoreId,
    string Id,
    string DisplayName,
    string Tag);
