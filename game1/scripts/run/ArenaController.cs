using System;
using System.Collections.Generic;
using System.Linq;

namespace Game1;

/// <summary>
/// 五波竞技场的唯一状态机。它只接收“刷新结束、敌军清空、奖励确认”等事实，
/// 不遍历场景树，也不直接生成或删除敌军。
/// </summary>
public sealed class ArenaController
{
    private readonly RunState _runState;
    private ArenaDefinition _definition;
    private IReadOnlyList<WaveSchedule> _schedules = Array.Empty<WaveSchedule>();

    public ArenaController(RunState runState)
    {
        _runState = runState ?? throw new ArgumentNullException(nameof(runState));
    }

    public ArenaState State { get; private set; } = ArenaState.Loading;
    public int CurrentWave { get; private set; }
    public RewardKind? CurrentRewardKind { get; private set; }

    public event Action<ArenaState> StateChanged;
    public event Action<WaveDefinition> WaveRequested;
    public event Action<RewardKind> RewardRequested;
    public event Action BossRequested;
    public event Action ArenaFailed;

    public void BeginArena(ArenaDefinition definition)
    {
        definition = definition ?? throw new ArgumentNullException(nameof(definition));
        definition.Validate();
        _definition = definition;
        BeginArena(
            definition.Waves
                .Select(wave => new WaveSchedule(
                    wave.WaveNumber,
                    wave.SpawnDurationSeconds,
                    wave.RewardKind,
                    wave.IncludesElite))
                .ToArray(),
            definition.ArenaIndex);
    }

    /// <summary>纯领域入口，供确定性模拟与独立测试使用；正常 Godot 流程使用 ArenaDefinition 重载。</summary>
    public void BeginArena(IReadOnlyList<WaveSchedule> schedules, int arenaIndex)
    {
        if (State != ArenaState.Loading)
            throw new InvalidOperationException("竞技场只能从 Loading 开始。");
        if (schedules is null) throw new ArgumentNullException(nameof(schedules));
        ValidateSchedules(schedules);
        if (arenaIndex != _runState.ArenaIndex)
            throw new InvalidOperationException("竞技场定义索引必须与当前 RunState 一致。");

        _schedules = schedules.ToArray();
        CurrentWave = 1;
        CurrentRewardKind = null;
        _runState.SetWaveIndex(0);
        SetState(ArenaState.Intro);
    }

    public void OnIntroFinished()
    {
        RequireState(ArenaState.Intro, "只有 Intro 可以开始首波。");
        StartCurrentWave();
    }

    public void OnWaveSpawnWindowEnded()
    {
        RequireState(ArenaState.WaveCombat, "只有 WaveCombat 可以结束刷新窗口。");
        SetState(ArenaState.Cleanup);
    }

    public void OnAllEnemiesCleared()
    {
        RequireState(ArenaState.Cleanup, "只有 Cleanup 且敌军确已清空时才能结算。");
        CurrentRewardKind = _schedules[CurrentWave - 1].RewardKind;
        SetState(ArenaState.Reward);
        RewardRequested?.Invoke(CurrentRewardKind.Value);
    }

    public void ConfirmReward(string rewardId)
    {
        RequireState(ArenaState.Reward, "只有 Reward 可以确认奖励。");
        if (string.IsNullOrWhiteSpace(rewardId))
            throw new ArgumentException("奖励确认 Id 不得为空。", nameof(rewardId));

        CurrentRewardKind = null;
        if (CurrentWave < 5)
        {
            CurrentWave++;
            _runState.SetWaveIndex(CurrentWave - 1);
            StartCurrentWave();
            return;
        }

        SetState(ArenaState.BossIntro);
        BossRequested?.Invoke();
    }

    public void OnPlayerRunFailed()
    {
        if (State is ArenaState.Completed or ArenaState.Failed)
            return;
        SetState(ArenaState.Failed);
        ArenaFailed?.Invoke();
    }

    /// <summary>Boss 实例已进入当前竞技场；只有五波奖励完成后才能切入战斗。</summary>
    public void OnBossStarted()
    {
        RequireState(ArenaState.BossIntro, "只有 BossIntro 可以开始 Boss 战斗。 ");
        SetState(ArenaState.BossCombat);
    }

    /// <summary>Boss 击败是竞技场完成事实，由 RunController 决定后续竞技场或完整单局结算。</summary>
    public void OnBossDefeated()
    {
        RequireState(ArenaState.BossCombat, "只有 BossCombat 可以完成竞技场。 ");
        SetState(ArenaState.Completed);
    }

    private void StartCurrentWave()
    {
        SetState(ArenaState.WaveCombat);
        if (_definition is not null) WaveRequested?.Invoke(_definition.GetWave(CurrentWave));
    }

    private static void ValidateSchedules(IReadOnlyList<WaveSchedule> schedules)
    {
        IReadOnlyList<WaveSchedule> approved = WaveSchedule.CreateApproved();
        if (schedules.Count != approved.Count)
            throw new ArgumentException("竞技场必须恰有五个波次日程。", nameof(schedules));
        for (int index = 0; index < approved.Count; index++)
        {
            if (schedules[index] != approved[index])
                throw new ArgumentException("竞技场波次日程必须匹配已确认的五波配置。", nameof(schedules));
        }
    }

    private void RequireState(ArenaState expected, string message)
    {
        if (State != expected) throw new InvalidOperationException(message);
    }

    private void SetState(ArenaState state)
    {
        State = state;
        StateChanged?.Invoke(state);
    }
}
