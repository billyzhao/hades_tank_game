namespace Game1;

/// <summary>单个协议贡献的属性修正声明。</summary>
public readonly record struct StatModifier(
    StatId Stat,
    float FlatAdd,
    float AdditivePercent,
    float MultiplicativePercent,
    string SourceProtocolId);
