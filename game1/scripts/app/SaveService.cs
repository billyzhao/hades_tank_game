using System;
using System.IO;
using System.Text.Json;
using Godot;

namespace Game1;

/// <summary>版本化、可恢复的单槽存档服务；写入前回读临时文件，替换前保留上一份有效备份。</summary>
public sealed class SaveService
{
    private readonly string _savePath;
    private readonly bool _emitWarnings;
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    public SaveService(string savePath = "", bool emitWarnings = true)
    {
        _savePath = string.IsNullOrWhiteSpace(savePath)
            ? ProjectSettings.GlobalizePath("user://save.json")
            : Path.GetFullPath(savePath);
        _emitWarnings = emitWarnings;
    }

    public SaveData LoadOrDefault()
    {
        if (!File.Exists(_savePath)) return SaveData.CreateDefault();
        try
        {
            return ReadValidated(_savePath);
        }
        catch (Exception exception)
        {
            string brokenPath = _savePath + ".broken";
            if (File.Exists(brokenPath)) File.Delete(brokenPath);
            File.Move(_savePath, brokenPath);
            if (_emitWarnings) GD.PushWarning($"存档损坏，已保留为 {brokenPath} 并使用默认数据：{exception.Message}");
            return SaveData.CreateDefault();
        }
    }

    public void SaveAtomic(SaveData data)
    {
        if (data is null) throw new ArgumentNullException(nameof(data));
        data.Validate();
        string directory = Path.GetDirectoryName(_savePath)!;
        Directory.CreateDirectory(directory);
        string temporaryPath = _savePath + ".tmp";
        string backupPath = _savePath + ".bak";
        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(data, _jsonOptions);
        using (FileStream stream = new(temporaryPath, FileMode.Create, System.IO.FileAccess.Write, FileShare.None))
        {
            stream.Write(bytes);
            stream.Flush(true);
        }

        ReadValidated(temporaryPath);
        if (File.Exists(_savePath)) File.Replace(temporaryPath, _savePath, backupPath, true);
        else File.Move(temporaryPath, _savePath);
    }

    private SaveData ReadValidated(string path)
    {
        byte[] bytes = File.ReadAllBytes(path);
        using JsonDocument document = JsonDocument.Parse(bytes);
        if (!document.RootElement.TryGetProperty("schema_version", out JsonElement schemaElement))
            throw new InvalidDataException("存档缺少 schema_version。");

        int schemaVersion = schemaElement.GetInt32();
        if (schemaVersion == 1)
        {
            LegacySaveData legacy = JsonSerializer.Deserialize<LegacySaveData>(bytes, _jsonOptions)
                ?? throw new InvalidDataException("旧存档 JSON 为空。");
            return MigrateSchemaOne(legacy);
        }

        SaveData data = JsonSerializer.Deserialize<SaveData>(bytes, _jsonOptions)
            ?? throw new InvalidDataException("存档 JSON 为空。");
        data.Validate();
        return data;
    }

    private static SaveData MigrateSchemaOne(LegacySaveData legacy)
    {
        SaveData migrated = SaveData.CreateDefault();
        migrated.Settings = legacy.Settings ?? new SaveSettings();
        migrated.UnlockedIds = legacy.UnlockedIds ?? new System.Collections.Generic.List<string>();
        LegacyLastRunSummary summary = legacy.LastRun ?? new LegacyLastRunSummary();
        migrated.LastRun = new LastRunSummary
        {
            Seed = summary.Seed,
            CoreId = string.Empty,
            ArenaIndex = 0,
            WaveIndex = 0,
            Level = 1,
            ElapsedSeconds = summary.ElapsedSeconds,
            Result = summary.Result ?? string.Empty
        };
        migrated.Validate();
        return migrated;
    }

    private sealed class LegacySaveData
    {
        [System.Text.Json.Serialization.JsonPropertyName("settings")]
        public SaveSettings Settings { get; set; } = new();

        [System.Text.Json.Serialization.JsonPropertyName("unlocked_ids")]
        public System.Collections.Generic.List<string> UnlockedIds { get; set; } = new();

        [System.Text.Json.Serialization.JsonPropertyName("last_run")]
        public LegacyLastRunSummary LastRun { get; set; } = new();
    }

    private sealed class LegacyLastRunSummary
    {
        [System.Text.Json.Serialization.JsonPropertyName("seed")]
        public int Seed { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("relay_integrity")]
        public int RelayIntegrity { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("elapsed_seconds")]
        public double ElapsedSeconds { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("result")]
        public string Result { get; set; } = string.Empty;
    }
}
