using System;
using System.Collections.Generic;
using System.Linq;

namespace Game1;

public sealed class RunState
{
    private readonly System.Collections.Generic.List<string> _selectedProtocolIds = new();

    public required int Seed { get; init; }

    public int PlayerArmor { get; private set; }

    public int MaximumArmor { get; private set; }

    public int RebootsRemaining { get; set; }

    public int RoomIndex { get; set; }

    /// <summary>当前局已确认的协议，调用方只能读取，不能绕过 BuildController 直接写入。</summary>
    public System.Collections.Generic.IReadOnlyList<string> SelectedProtocolIds => _selectedProtocolIds.AsReadOnly();

    /// <summary>当前房间待确认的三选一候选；奖励阶段以外必须清空。</summary>
    public ProtocolOffer CurrentOffer { get; private set; }

    public static RunState CreateNew(
        int seed,
        int maximumArmor = 100,
        int reboots = 1)
    {
        if (maximumArmor <= 0) throw new System.ArgumentOutOfRangeException(nameof(maximumArmor));
        if (reboots < 0) throw new System.ArgumentOutOfRangeException(nameof(reboots));
        return new RunState
        {
            Seed = seed,
            PlayerArmor = maximumArmor,
            MaximumArmor = maximumArmor,
            RebootsRemaining = reboots,
            RoomIndex = 0
        };
    }

    /// <summary>把当前玩家实例的装甲同步为跨场景真值；所有入口都统一钳制到有效范围。</summary>
    public void SynchronizeArmor(int armor, int maximumArmor)
    {
        if (maximumArmor <= 0) throw new System.ArgumentOutOfRangeException(nameof(maximumArmor));
        MaximumArmor = maximumArmor;
        PlayerArmor = System.Math.Clamp(armor, 0, maximumArmor);
    }

    /// <summary>战场重启固定恢复向上取整的 50% 最大装甲。</summary>
    public void RestoreAfterReboot()
    {
        PlayerArmor = (MaximumArmor + 1) / 2;
    }

    public void RepairArmor(int amount)
    {
        if (amount < 0) throw new System.ArgumentOutOfRangeException(nameof(amount));
        PlayerArmor = System.Math.Min(MaximumArmor, PlayerArmor + amount);
    }

    /// <summary>击败竞技场 Boss 后只全修装甲，重启次数跨完整单局持续。</summary>
    public void RestoreArmorForNextArena() => PlayerArmor = MaximumArmor;

    /// <summary>仅在仍有次数时消耗一次战场重启，绝不让计数变为负数。</summary>
    public bool TryConsumeReboot()
    {
        if (RebootsRemaining <= 0) return false;
        RebootsRemaining--;
        return true;
    }

    public void SetCurrentOffer(ProtocolOffer offer)
    {
        if (offer is null || offer.ProtocolIds is null || offer.ProtocolIds.Count != 3 ||
            offer.ProtocolIds.Any(string.IsNullOrWhiteSpace) || offer.ProtocolIds.Distinct(System.StringComparer.Ordinal).Count() != 3)
        {
            throw new System.ArgumentException("奖励候选必须是三个唯一且非空的协议 Id。", nameof(offer));
        }

        CurrentOffer = offer;
    }

    public void ClearCurrentOffer() => CurrentOffer = null;

    internal void RecordSelectedProtocol(string protocolId)
    {
        if (string.IsNullOrWhiteSpace(protocolId))
        {
            throw new System.ArgumentException("协议 Id 不得为空。", nameof(protocolId));
        }

        _selectedProtocolIds.Add(protocolId);
    }

    internal void ClearBuildState()
    {
        _selectedProtocolIds.Clear();
        CurrentOffer = null;
    }
}
