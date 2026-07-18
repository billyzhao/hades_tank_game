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
        SaveData data = JsonSerializer.Deserialize<SaveData>(File.ReadAllBytes(path), _jsonOptions)
            ?? throw new InvalidDataException("存档 JSON 为空。");
        data.Validate();
        return data;
    }
}
