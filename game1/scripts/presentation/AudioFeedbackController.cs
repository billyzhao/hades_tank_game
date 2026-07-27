using System;
using System.Collections.Generic;
using Godot;

namespace Game1;

/// <summary>
/// BC-04 封锁城区音频总控。它只订阅战斗与 UI 事实并播放 Batch 09，
/// 不参与伤害、刷新、奖励、暂停或 Boss 阶段裁决。
/// </summary>
public partial class AudioFeedbackController : Node
{
    private const string MasterBus = "Master";
    private const string MusicBus = "Music";
    private const string AmbienceBus = "Ambience";
    private const string SfxBus = "SFX";
    private const string UiBus = "UI";
    private const int MaximumOneShotVoices = 24;

    private readonly Dictionary<AudioCue, AudioStream[]> _cues = new();
    private readonly Queue<Node> _oneShotVoices = new();
    private AudioStreamPlayer _ambience = null!;
    private AudioStreamPlayer _combatBase = null!;
    private AudioStreamPlayer _combatIntensity = null!;
    private AudioStreamPlayer _bossMusic = null!;
    private AudioStreamPlayer _tracks = null!;
    private PlayerTank _player = null!;
    private HealthComponent _health = null!;
    private WaveDirector _waveDirector;
    private bool _muted;
    private int _variation;
    private float _lowArmorReminder;
    private int _lastArmor;
    private bool _uiTreeBound;

    public int LoadedCueCount => _cues.Count;
    public bool MusicLayersPlaying => _combatBase?.Playing == true && _combatIntensity?.Playing == true;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        EnsureAudioBuses();
        LoadCueLibrary();
        CreateContinuousPlayers();

        Node room = GetParent();
        _player = room.GetNode<PlayerTank>("PlayerTank");
        _health = _player.GetNode<HealthComponent>("HealthComponent");
        _lastArmor = _health.Armor;
        WeaponController weapon = _player.GetNode<WeaponController>("WeaponController");
        weapon.Fired += (_, _, _) =>
            PlayCue(AudioCue.PlayerFire, SfxBus, -7f, 0.96f + (_variation++ % 5) * 0.02f, _player.GlobalPosition);
        weapon.ProjectileImpacted += (position, destroyed, reflected) =>
        {
            // 普通敌军击毁由 WaveDirector 的唯一事实播放，避免同一击毁叠两次爆炸。
            if (!destroyed)
                PlayCue(AudioCue.BossBarrier, SfxBus, -13f, reflected ? 1.08f : 1f, position);
        };
        _player.GetNode<DashComponent>("DashComponent").DashStarted += () =>
            PlayCue(AudioCue.PlayerDash, SfxBus, -8f, 1f, _player.GlobalPosition);
        _health.ValueChanged += OnPlayerHealthChanged;
        RebootController reboot = room.GetNode<RebootController>("RebootController");
        reboot.RebootStarted += _ => PlayCue(AudioCue.RebootStart, SfxBus, -5f, 1f, _player.GlobalPosition);
        reboot.Rebooted += () => PlayCue(AudioCue.RebootComplete, SfxBus, -4f, 1f, _player.GlobalPosition);

        _ambience.Play();
        _combatBase.Play();
        _combatIntensity.Play();
        _tracks.Play();
    }

    public override void _Process(double delta)
    {
        if (_player is not null && IsInstanceValid(_player))
        {
            float targetDb = !GetTree().Paused && _player.GetRealVelocity().LengthSquared() > 16f ? -18f : -55f;
            _tracks.VolumeDb = Mathf.MoveToward(_tracks.VolumeDb, targetDb, (float)delta * 42f);
        }

        _lowArmorReminder = Mathf.Max(0f, _lowArmorReminder - (float)delta);
    }

    public void ApplySettings(SaveSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        SetBusLinear(MasterBus, settings.MasterVolume, -3f);
        SetBusLinear(SfxBus, settings.SfxVolume, -4f);
        SetBusLinear(MusicBus, settings.MusicVolume, -4f);
        SetBusLinear(AmbienceBus, settings.AmbienceVolume, -8f);
        SetBusLinear(UiBus, settings.UiVolume, -6f);
    }

    public void BindWaveDirector(WaveDirector director, int waveNumber)
    {
        ArgumentNullException.ThrowIfNull(director);
        if (_waveDirector is not null)
        {
            _waveDirector.SpawnWarningStarted -= OnSpawnWarning;
            _waveDirector.EnemyCreated -= OnEnemyCreated;
            _waveDirector.EnemyDefeated -= OnEnemyDefeated;
            _waveDirector.EliteStateChanged -= OnEliteStateChanged;
        }

        _waveDirector = director;
        _waveDirector.SpawnWarningStarted += OnSpawnWarning;
        _waveDirector.EnemyCreated += OnEnemyCreated;
        _waveDirector.EnemyDefeated += OnEnemyDefeated;
        _waveDirector.EliteStateChanged += OnEliteStateChanged;
        SetWaveIntensity(waveNumber);
    }

    public void BindBoss(RoadblockCommander boss, BossEncounterController encounter)
    {
        ArgumentNullException.ThrowIfNull(boss);
        ArgumentNullException.ThrowIfNull(encounter);
        boss.AttackFired += () => PlayCue(AudioCue.BossTurret, SfxBus, -7f, 0.9f, boss.GlobalPosition);
        boss.ChargeTelegraphStarted += () => PlayCue(AudioCue.BossChargeWarning, SfxBus, -4f, 1f, boss.GlobalPosition);
        boss.ChargeStarted += () => PlayCue(AudioCue.BossCharge, SfxBus, -4f, 1f, boss.GlobalPosition);
        boss.WeakpointExposed += () => PlayCue(AudioCue.BossWeakpoint, SfxBus, -5f, 1f, boss.GlobalPosition);
        boss.PhaseChanged += _ => PlayCue(AudioCue.BossPhase, SfxBus, -3f, 1f, boss.GlobalPosition);
        boss.Defeated += () => PlayCue(AudioCue.BossDestroy, SfxBus, -1f, 1f, boss.GlobalPosition);
        encounter.Barrier.Deployed += position => PlayCue(AudioCue.BossBarrier, SfxBus, -6f, 1f, position);
        encounter.Gun.TelegraphStarted += () => PlayCue(AudioCue.BossChargeWarning, SfxBus, -10f, 1.25f, encounter.Gun.GlobalPosition);
        encounter.Gun.ShotFired += () => PlayCue(AudioCue.BossTurret, SfxBus, -9f, 1.08f, encounter.Gun.GlobalPosition);
    }

    public void BeginBossMusic()
    {
        PlayCue(AudioCue.BossIntro, SfxBus, -2f);
        _bossMusic.Play();
        CrossFade(_combatBase, -45f, _bossMusic, -7f, 0.75f);
        CrossFade(_combatIntensity, -45f, null, 0f, 0.75f);
    }

    public void PlayUiMove() => PlayCue(AudioCue.UiMove, UiBus, -16f, 1f + (_variation++ % 3) * .025f);
    public void PlayUiConfirm() => PlayCue(AudioCue.UiConfirm, UiBus, -10f);
    public void PlayUiLevelUp() => PlayCue(AudioCue.UiLevelUp, UiBus, -7f);
    public void PlayUiMaintenance() => PlayCue(AudioCue.UiMaintenance, UiBus, -8f);
    public void PlayUiFailure() => PlayCue(AudioCue.UiFailure, UiBus, -4f);
    public void PlayUiVictory() => PlayCue(AudioCue.UiVictory, UiBus, -3f);

    public void BindUiTree(Node root)
    {
        ArgumentNullException.ThrowIfNull(root);
        BindUiBranch(root);
        if (_uiTreeBound) return;
        _uiTreeBound = true;
        GetTree().NodeAdded += BindUiNode;
    }

    public override void _Input(InputEvent inputEvent)
    {
        if (inputEvent is not InputEventKey key || !key.Pressed || key.Echo || key.Keycode != Key.M)
        {
            return;
        }

        _muted = !_muted;
        int index = AudioServer.GetBusIndex(MasterBus);
        if (index >= 0) AudioServer.SetBusMute(index, _muted);
    }

    private void OnPlayerHealthChanged(int armor, int _)
    {
        bool damaged = armor < _lastArmor;
        if (damaged) PlayCue(AudioCue.PlayerHit, SfxBus, -7f, 1f, _player.GlobalPosition);
        if (damaged && AudioMixPolicy.IsLowArmor(armor, _health.MaximumArmor) && _lowArmorReminder <= 0f)
        {
            PlayCue(AudioCue.ArmorLow, UiBus, -6f);
            _lowArmorReminder = 4f;
        }
        _lastArmor = armor;
    }

    private void OnSpawnWarning(BehaviorId _, bool elite) =>
        PlayCue(elite ? AudioCue.EliteOverdrive : AudioCue.SpawnWarning, SfxBus, elite ? -5f : -12f);

    private void OnEnemyCreated(EnemyTank enemy, BehaviorId behavior, bool _)
    {
        enemy.ProjectileFired += () =>
            PlayCue(AudioMixPolicy.EnemyFireCue(behavior), SfxBus, -10f, 1f, enemy.GlobalPosition);
        enemy.AttackTelegraphStarted += rawBehavior =>
        {
            if ((BehaviorId)rawBehavior == BehaviorId.Mortar)
                PlayCue(AudioCue.SpawnWarning, SfxBus, -8f, .74f, enemy.GlobalPosition);
        };
        enemy.EliteOverdriveChanged += active =>
        {
            if (active) PlayCue(AudioCue.EliteOverdrive, SfxBus, -6f, 1f, enemy.GlobalPosition);
        };
    }

    private void OnEnemyDefeated(Vector2 position, bool elite) =>
        PlayCue(AudioCue.EnemyDestroy, SfxBus, elite ? -3f : -9f, elite ? .8f : 1.05f, position);

    private void OnEliteStateChanged(bool alive)
    {
        if (alive) PlayCue(AudioCue.EliteOverdrive, SfxBus, -5f);
    }

    private void SetWaveIntensity(int waveNumber)
    {
        _combatBase.VolumeDb = -11f;
        CreateTween()
            .SetPauseMode(Tween.TweenPauseMode.Process)
            .TweenProperty(_combatIntensity, "volume_db", AudioMixPolicy.CombatIntensityDb(waveNumber), 0.65f);
    }

    private void LoadCueLibrary()
    {
        foreach (AudioCue cue in Enum.GetValues<AudioCue>())
            _cues[cue] = AudioCueCatalog.Load(cue);
    }

    private void CreateContinuousPlayers()
    {
        _ambience = CreateLoopPlayer("Ambience", AudioCue.Ambience, AmbienceBus, -14f);
        _combatBase = CreateLoopPlayer("CombatBase", AudioCue.CombatBase, MusicBus, -11f);
        _combatIntensity = CreateLoopPlayer("CombatIntensity", AudioCue.CombatIntensity, MusicBus, -30f);
        _bossMusic = CreateLoopPlayer("BossMusic", AudioCue.BossMusic, MusicBus, -45f);
        _tracks = CreateLoopPlayer("Tracks", AudioCue.PlayerTrack, SfxBus, -55f);
    }

    private AudioStreamPlayer CreateLoopPlayer(string name, AudioCue cue, string bus, float volumeDb)
    {
        AudioStreamPlayer player = new()
        {
            Name = name,
            Stream = AudioCueCatalog.LoadLoop(cue),
            Bus = bus,
            VolumeDb = volumeDb,
            ProcessMode = ProcessModeEnum.Always
        };
        AddChild(player);
        return player;
    }

    private void PlayCue(AudioCue cue, string bus, float volumeDb, float pitch = 1f, Vector2? position = null)
    {
        if (!_cues.TryGetValue(cue, out AudioStream[] streams) || streams.Length == 0) return;
        AudioStream stream = streams[_variation++ % streams.Length];
        TrimVoices();
        if (position is Vector2 worldPosition)
        {
            AudioStreamPlayer2D player = new()
            {
                Stream = stream,
                Bus = bus,
                VolumeDb = volumeDb,
                PitchScale = pitch,
                MaxDistance = 420f,
                Attenuation = .35f,
                PanningStrength = .55f
            };
            AddChild(player);
            player.GlobalPosition = worldPosition;
            _oneShotVoices.Enqueue(player);
            player.Finished += () => ReleaseVoice(player);
            player.Play();
            return;
        }

        AudioStreamPlayer global = new()
        {
            Stream = stream,
            Bus = bus,
            VolumeDb = volumeDb,
            PitchScale = pitch,
            ProcessMode = ProcessModeEnum.Always
        };
        AddChild(global);
        _oneShotVoices.Enqueue(global);
        global.Finished += () => ReleaseVoice(global);
        global.Play();
    }

    private void BindUiNode(Node node)
    {
        if (node is not BaseButton button || button.HasMeta("bc04_audio_bound")) return;
        button.SetMeta("bc04_audio_bound", true);
        button.FocusEntered += PlayUiMove;
        button.MouseEntered += PlayUiMove;
        button.Pressed += PlayUiConfirm;
    }

    private void BindUiBranch(Node root)
    {
        BindUiNode(root);
        foreach (Node child in root.GetChildren()) BindUiBranch(child);
    }

    private void TrimVoices()
    {
        while (_oneShotVoices.Count >= MaximumOneShotVoices)
        {
            Node oldest = _oneShotVoices.Dequeue();
            if (IsInstanceValid(oldest)) oldest.QueueFree();
        }
    }

    private void ReleaseVoice(Node voice)
    {
        if (IsInstanceValid(voice)) voice.QueueFree();
        while (_oneShotVoices.Count > 0 && !IsInstanceValid(_oneShotVoices.Peek()))
            _oneShotVoices.Dequeue();
    }

    private void CrossFade(AudioStreamPlayer from, float fromDb, AudioStreamPlayer to, float toDb, float seconds)
    {
        Tween tween = CreateTween().SetParallel().SetPauseMode(Tween.TweenPauseMode.Process);
        if (from is not null) tween.TweenProperty(from, "volume_db", fromDb, seconds);
        if (to is not null) tween.TweenProperty(to, "volume_db", toDb, seconds);
    }

    private static void EnsureAudioBuses()
    {
        EnsureBus(MusicBus, -4f);
        EnsureBus(AmbienceBus, -8f);
        EnsureBus(SfxBus, -4f);
        EnsureBus(UiBus, -6f);
    }

    private static void EnsureBus(string name, float volumeDb)
    {
        if (AudioServer.GetBusIndex(name) >= 0) return;
        AudioServer.AddBus();
        int index = AudioServer.BusCount - 1;
        AudioServer.SetBusName(index, name);
        AudioServer.SetBusSend(index, MasterBus);
        AudioServer.SetBusVolumeDb(index, volumeDb);
    }

    private static void SetBusLinear(string name, float linear, float trimDb)
    {
        int index = AudioServer.GetBusIndex(name);
        if (index >= 0) AudioServer.SetBusVolumeDb(index, AudioMixPolicy.LinearToDecibels(linear) + trimDb);
    }

    public override void _ExitTree()
    {
        if (_uiTreeBound && GetTree() is SceneTree tree) tree.NodeAdded -= BindUiNode;
        if (_waveDirector is not null)
        {
            _waveDirector.SpawnWarningStarted -= OnSpawnWarning;
            _waveDirector.EnemyCreated -= OnEnemyCreated;
            _waveDirector.EnemyDefeated -= OnEnemyDefeated;
            _waveDirector.EliteStateChanged -= OnEliteStateChanged;
        }
        int master = AudioServer.GetBusIndex(MasterBus);
        if (master >= 0 && _muted) AudioServer.SetBusMute(master, false);
    }
}
