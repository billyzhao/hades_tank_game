using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;

namespace Game1;

/// <summary>MVP 持久化 DTO；仅保存设置、局外解锁和最近一局摘要，不保存战斗中途状态。</summary>
public sealed class SaveData
{
    public const int CurrentSchemaVersion = 2;

    [JsonPropertyName("schema_version")]
    public int SchemaVersion { get; set; } = CurrentSchemaVersion;

    [JsonPropertyName("settings")]
    public SaveSettings Settings { get; set; } = new();

    [JsonPropertyName("unlocked_ids")]
    public List<string> UnlockedIds { get; set; } = new();

    [JsonPropertyName("last_run")]
    public LastRunSummary LastRun { get; set; } = new();

    public static SaveData CreateDefault() => new();

    public void Validate()
    {
        if (SchemaVersion != CurrentSchemaVersion) throw new InvalidDataException($"不支持的存档版本：{SchemaVersion}。");
        if (Settings is null) throw new InvalidDataException("存档缺少设置数据。");
        if (UnlockedIds is null) throw new InvalidDataException("存档缺少解锁列表。");
        if (LastRun is null) throw new InvalidDataException("存档缺少最近一局摘要。");
        if (Settings.MasterVolume < 0f || Settings.MasterVolume > 1f) throw new InvalidDataException("主音量必须位于 0 到 1。");
        if (Settings.SfxVolume < 0f || Settings.SfxVolume > 1f) throw new InvalidDataException("音效音量必须位于 0 到 1。");
    }
}

public sealed class SaveSettings
{
    [JsonPropertyName("master_volume")]
    public float MasterVolume { get; set; } = 1f;

    [JsonPropertyName("sfx_volume")]
    public float SfxVolume { get; set; } = 1f;
}

public sealed class LastRunSummary
{
    [JsonPropertyName("seed")]
    public int Seed { get; set; }

    [JsonPropertyName("core_id")]
    public string CoreId { get; set; } = string.Empty;

    [JsonPropertyName("arena_index")]
    public int ArenaIndex { get; set; }

    [JsonPropertyName("wave_index")]
    public int WaveIndex { get; set; }

    [JsonPropertyName("level")]
    public int Level { get; set; } = 1;

    [JsonPropertyName("elapsed_seconds")]
    public double ElapsedSeconds { get; set; }

    [JsonPropertyName("result")]
    public string Result { get; set; } = string.Empty;
}
