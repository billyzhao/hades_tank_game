#nullable enable

namespace Game1;

/// <summary>确定性协议候选生成的完整输入。</summary>
public sealed record RewardGenerationInput(
    int RunSeed,
    int RoomIndex,
    System.Collections.Generic.IReadOnlyList<string> SelectedProtocolIds,
    string ContentCatalogVersion,
    System.Collections.Generic.IReadOnlyDictionary<string, ProtocolRank>? ProtocolRanks = null,
    RewardKind? RewardKind = null,
    CoreId? SelectedCore = null);
