using System;
using System.Collections.Generic;
using System.Linq;

namespace Game1;

public sealed class RunState
{
    private readonly System.Collections.Generic.List<string> _selectedProtocolIds = new();

    public required int Seed { get; init; }

    public int RelayIntegrity { get; set; }

    public int PlayerArmor { get; set; }

    public int RebootsRemaining { get; set; }

    public int RoomIndex { get; set; }

    /// <summary>当前局已确认的协议，调用方只能读取，不能绕过 BuildController 直接写入。</summary>
    public System.Collections.Generic.IReadOnlyList<string> SelectedProtocolIds => _selectedProtocolIds.AsReadOnly();

    /// <summary>当前房间待确认的三选一候选；奖励阶段以外必须清空。</summary>
    public ProtocolOffer CurrentOffer { get; private set; }

    public static RunState CreateNew(
        int seed,
        int relayIntegrity = 100,
        int armor = 100,
        int reboots = 1)
    {
        return new RunState
        {
            Seed = seed,
            RelayIntegrity = relayIntegrity,
            PlayerArmor = armor,
            RebootsRemaining = reboots,
            RoomIndex = 0
        };
    }

    /// <summary>扣除中继站耐久；返回值表示中继站是否仍可维持本局。</summary>
    public bool ApplyRelayDamage(int amount)
    {
        RelayIntegrity = System.Math.Max(0, RelayIntegrity - System.Math.Max(0, amount));
        return RelayIntegrity > 0;
    }

    /// <summary>清场维修只能恢复中继站耐久，且永远不能超过其设计总血量。</summary>
    public int RestoreRelayIntegrity(int amount, int maximumIntegrity = 100)
    {
        if (maximumIntegrity <= 0) throw new System.ArgumentOutOfRangeException(nameof(maximumIntegrity));
        RelayIntegrity = System.Math.Min(maximumIntegrity, RelayIntegrity + System.Math.Max(0, amount));
        return RelayIntegrity;
    }

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
