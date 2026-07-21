namespace Game1;

/// <summary>单个竞技场的唯一阶段状态；波次、奖励和 Boss 衔接不得在 AppRoot 中另建判断。</summary>
public enum ArenaState
{
    Loading,
    Intro,
    WaveCombat,
    Cleanup,
    Reward,
    BossIntro,
    BossCombat,
    Completed,
    Failed
}
