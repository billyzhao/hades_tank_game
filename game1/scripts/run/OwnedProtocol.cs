namespace Game1;

/// <summary>单局已拥有协议的运行时事实；定义仍由 ContentCatalog 持有。</summary>
public sealed record OwnedProtocol(string ProtocolId, ProtocolRank Rank);
