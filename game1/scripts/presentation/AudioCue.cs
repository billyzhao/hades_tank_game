namespace Game1;

/// <summary>BC-04 表现层稳定音频语义；玩法代码只上报事实，不读取文件路径。</summary>
public enum AudioCue
{
    PlayerTrack,
    PlayerFire,
    PlayerDash,
    PlayerHit,
    ArmorLow,
    RebootStart,
    RebootComplete,
    EnemyScoutFire,
    EnemyPatrolFire,
    EnemyAssaultFire,
    EnemyMortarFire,
    SpawnWarning,
    EnemyDestroy,
    EliteOverdrive,
    BossIntro,
    BossBarrier,
    BossTurret,
    BossChargeWarning,
    BossCharge,
    BossWeakpoint,
    BossPhase,
    BossDestroy,
    UiMove,
    UiConfirm,
    UiLevelUp,
    UiMaintenance,
    UiFailure,
    UiVictory,
    Ambience,
    CombatBase,
    CombatIntensity,
    BossMusic
}
