using System;
using System.Linq;

namespace Game1;

/// <summary>06A 房间生命周期阶段。</summary>
public enum RoomPhase { Loading, Intro, Combat, Cleared, Reward, Exiting, Failed }

/// <summary>本局生命周期与协议选择的唯一状态机；场景/UI 只订阅状态，不直接改 RunState。</summary>
public sealed class RunController
{
    private const double IntroDurationSeconds = 0.6d;
    private readonly RunState _state;
    private readonly BuildController _build;
    private readonly RewardGenerator _rewards;
    private double _introElapsed;
    private bool _choiceCommitted;

    public RunController(RunState state, BuildController buildController, RewardGenerator rewardGenerator)
    {
        _state = state ?? throw new ArgumentNullException(nameof(state));
        _build = buildController ?? throw new ArgumentNullException(nameof(buildController));
        _rewards = rewardGenerator ?? throw new ArgumentNullException(nameof(rewardGenerator));
        Phase = RoomPhase.Loading;
    }

    public RoomPhase Phase { get; private set; }
    public ProtocolOffer CurrentOffer { get; private set; }
    public event Action<RoomPhase> PhaseChanged;

    public void BeginRoom()
    {
        if (Phase != RoomPhase.Loading && Phase != RoomPhase.Exiting)
            throw new InvalidOperationException("只能从 Loading 或 Exiting 开始房间。");
        _introElapsed = 0d;
        _choiceCommitted = false;
        CurrentOffer = null;
        _state.ClearCurrentOffer();
        SetPhase(RoomPhase.Intro);
    }

    public void Advance(double deltaSeconds)
    {
        if (!double.IsFinite(deltaSeconds) || deltaSeconds < 0d)
            throw new ArgumentOutOfRangeException(nameof(deltaSeconds));
        if (Phase != RoomPhase.Intro) return;
        _introElapsed += deltaSeconds;
        if (_introElapsed >= IntroDurationSeconds) SetPhase(RoomPhase.Combat);
    }

    public void OnCombatCleared()
    {
        if (Phase != RoomPhase.Combat) throw new InvalidOperationException("只有 Combat 可以清场。");
        SetPhase(RoomPhase.Cleared);
        _build.OnRoomCleared();
        CurrentOffer = _rewards.Generate(new RewardGenerationInput(
            _state.Seed, _state.RoomIndex, _state.SelectedProtocolIds, _build.CatalogVersion), _build.Catalog);
        _state.SetCurrentOffer(CurrentOffer);
        SetPhase(RoomPhase.Reward);
    }

    public void ChooseProtocol(string protocolId)
    {
        if (Phase == RoomPhase.Exiting && _choiceCommitted) return;
        if (Phase != RoomPhase.Reward) throw new InvalidOperationException("只有 Reward 可以选择协议。");
        if (string.IsNullOrWhiteSpace(protocolId) || CurrentOffer is null || !CurrentOffer.ProtocolIds.Contains(protocolId))
            throw new ArgumentException("选择必须来自当前三选一候选。", nameof(protocolId));
        _build.SelectProtocol(protocolId);
        _choiceCommitted = true;
        _state.ClearCurrentOffer();
        _state.RoomIndex++;
        SetPhase(RoomPhase.Exiting);
    }

    /// <summary>处理坦克耗尽：仅 Combat 可消耗一次重启，返回值表示本次是否允许战场重启。</summary>
    public bool OnTankDefeated()
    {
        if (Phase != RoomPhase.Combat) return false;
        if (_state.TryConsumeReboot()) return true;

        SetPhase(RoomPhase.Failed);
        return false;
    }

    private void SetPhase(RoomPhase phase)
    {
        Phase = phase;
        PhaseChanged?.Invoke(phase);
    }
}
