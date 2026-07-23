namespace Game1;

/// <summary>单局辅助槽的只读快照；槽位上限由 BuildController 统一保证。</summary>
public sealed record AuxiliarySlotState(string AuxiliaryId, int Rank);
