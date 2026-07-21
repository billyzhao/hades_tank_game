using System;
using System.Collections.Generic;
using System.Linq;

#nullable enable

namespace Game1;

/// <summary>波间奖励的唯一状态入口：生成候选、校验选择并将协议写入 BuildController。</summary>
public sealed class RewardController
{
    private readonly RunState _state;
    private readonly BuildController _build;
    private readonly ContentCatalog _catalog;
    private readonly RewardGenerator _protocolGenerator;
    private readonly MaintenanceRewardGenerator _maintenanceGenerator;

    public RewardController(
        RunState state,
        BuildController build,
        ContentCatalog catalog,
        RewardGenerator protocolGenerator,
        MaintenanceRewardGenerator maintenanceGenerator)
    {
        _state = state ?? throw new ArgumentNullException(nameof(state));
        _build = build ?? throw new ArgumentNullException(nameof(build));
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        _protocolGenerator = protocolGenerator ?? throw new ArgumentNullException(nameof(protocolGenerator));
        _maintenanceGenerator = maintenanceGenerator ?? throw new ArgumentNullException(nameof(maintenanceGenerator));
    }

    public RewardOffer? CurrentOffer { get; private set; }

    public RewardOffer Generate(RewardKind kind)
    {
        if (CurrentOffer is not null) throw new InvalidOperationException("当前奖励尚未选择，不能生成下一组候选。");
        RewardOffer offer = kind switch
        {
            RewardKind.NormalProtocol or RewardKind.RareProtocol => GenerateProtocolOffer(kind),
            RewardKind.Maintenance => _maintenanceGenerator.Generate(_state.Seed ^ _state.WaveIndex, _state.PlayerArmor, _state.MaximumArmor),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), "当前奖励类型尚未接入 Alpha 02E。")
        };
        CurrentOffer = offer;
        return offer;
    }

    public RewardChoice Choose(string choiceId)
    {
        if (CurrentOffer is null) throw new InvalidOperationException("当前不存在待选择的奖励。");
        RewardChoice choice = CurrentOffer.Choices.FirstOrDefault(item => string.Equals(item.Id, choiceId, StringComparison.Ordinal))
            ?? throw new ArgumentException("所选奖励不属于当前候选。", nameof(choiceId));

        if (CurrentOffer.Kind is RewardKind.NormalProtocol or RewardKind.RareProtocol)
        {
            _build.SelectProtocol(choice.Id);
        }
        else if (choice.Id == "maintenance_repair_25")
        {
            _state.RepairArmor((int)Math.Ceiling(_state.MaximumArmor * 0.25d));
        }

        CurrentOffer = null;
        return choice;
    }

    private RewardOffer GenerateProtocolOffer(RewardKind kind)
    {
        Dictionary<string, ProtocolRank> ranks = _state.OwnedProtocols
            .ToDictionary(protocol => protocol.ProtocolId, protocol => protocol.Rank, StringComparer.Ordinal);
        ProtocolOffer protocolOffer = _protocolGenerator.Generate(new RewardGenerationInput(
            _state.Seed,
            _state.ArenaIndex * 5 + _state.WaveIndex,
            _state.SelectedProtocolIds,
            _catalog.Version,
            ranks,
            kind,
            _state.SelectedCore), _catalog);
        RewardChoice[] choices = protocolOffer.ProtocolIds.Select(id =>
        {
            ProtocolDefinition definition = _catalog.GetProtocol(id);
            ProtocolRank next = _state.GetProtocolRank(id) + 1;
            return new RewardChoice(id, $"{definition.DisplayName} {next}", definition.Description, definition.Tags.ToArray());
        }).ToArray();
        return new RewardOffer(kind, choices);
    }
}
