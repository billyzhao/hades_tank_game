using System;
using Godot;

namespace Game1;

/// <summary>
/// 最小战斗音频控制器。原型音色在运行时生成并统一路由到 SFX 总线，
/// 后续替换 WAV/OGG 时无需修改伤害、移动或波次逻辑。
/// </summary>
public partial class AudioFeedbackController : Node
{
    private const string SfxBus = "SFX";
    private AudioStreamWav _fire = null!;
    private AudioStreamWav _impact = null!;
    private AudioStreamWav _destroy = null!;
    private AudioStreamWav _dash = null!;
    private AudioStreamWav _relayHit = null!;
    private bool _muted;
    private int _variation;

    public override void _Ready()
    {
        EnsureSfxBus();
        _fire = CreateTone(95f, 42f, 0.08f, noise: 0.38f, seed: 11);
        _impact = CreateTone(680f, 210f, 0.07f, noise: 0.62f, seed: 22);
        _destroy = CreateTone(120f, 32f, 0.24f, noise: 0.75f, seed: 33);
        _dash = CreateTone(160f, 70f, 0.13f, noise: 0.52f, seed: 44);
        _relayHit = CreateTone(360f, 190f, 0.16f, noise: 0.18f, seed: 55);

        Node room = GetParent();
        PlayerTank player = room.GetNode<PlayerTank>("PlayerTank");
        WeaponController weapon = player.GetNode<WeaponController>("WeaponController");
        weapon.Fired += (_, _, _) => PlayOneShot(_fire, -7f, 0.97f + (_variation++ % 3) * 0.03f);
        weapon.ProjectileImpacted += (_, destroyed, _) => PlayOneShot(destroyed ? _destroy : _impact, destroyed ? -4f : -8f);
        player.GetNode<DashComponent>("DashComponent").DashStarted += () => PlayOneShot(_dash, -8f);
        room.GetNode<RelayStation>("RelayStation").Damaged += _ => PlayOneShot(_relayHit, -6f);
    }

    public override void _Input(InputEvent inputEvent)
    {
        if (inputEvent is not InputEventKey key || !key.Pressed || key.Echo || key.Keycode != Key.M)
        {
            return;
        }

        _muted = !_muted;
        int index = AudioServer.GetBusIndex(SfxBus);
        if (index >= 0) AudioServer.SetBusMute(index, _muted);
    }

    private static void EnsureSfxBus()
    {
        if (AudioServer.GetBusIndex(SfxBus) >= 0) return;
        AudioServer.AddBus();
        int index = AudioServer.BusCount - 1;
        AudioServer.SetBusName(index, SfxBus);
        AudioServer.SetBusSend(index, "Master");
        AudioServer.SetBusVolumeDb(index, -6f);
    }

    private void PlayOneShot(AudioStream stream, float volumeDb, float pitch = 1f)
    {
        AudioStreamPlayer player = new()
        {
            Stream = stream,
            Bus = SfxBus,
            VolumeDb = volumeDb,
            PitchScale = pitch
        };
        AddChild(player);
        player.Finished += player.QueueFree;
        player.Play();
    }

    private static AudioStreamWav CreateTone(float startHz, float endHz, float seconds, float noise, int seed)
    {
        const int sampleRate = 22050;
        int sampleCount = Mathf.Max(1, Mathf.RoundToInt(sampleRate * seconds));
        byte[] data = new byte[sampleCount * 2];
        Random random = new(seed);
        double phase = 0d;
        for (int sample = 0; sample < sampleCount; sample++)
        {
            float progress = sample / (float)sampleCount;
            float frequency = Mathf.Lerp(startHz, endHz, progress);
            phase += Math.Tau * frequency / sampleRate;
            float envelope = Mathf.Pow(1f - progress, 1.8f);
            float wave = Mathf.Sin((float)phase) * (1f - noise) + ((float)random.NextDouble() * 2f - 1f) * noise;
            short value = (short)Mathf.Clamp(wave * envelope * 14000f, short.MinValue, short.MaxValue);
            data[sample * 2] = (byte)(value & 0xff);
            data[sample * 2 + 1] = (byte)((value >> 8) & 0xff);
        }

        return new AudioStreamWav
        {
            Format = AudioStreamWav.FormatEnum.Format16Bits,
            MixRate = sampleRate,
            Stereo = false,
            Data = data
        };
    }
}
