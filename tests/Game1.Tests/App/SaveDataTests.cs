using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;

namespace Game1.Tests.App;

[TestFixture]
public sealed class SaveDataTests
{
    private string _directory = null!;
    private string _savePath = null!;

    [SetUp]
    public void SetUp()
    {
        _directory = Path.Combine(Path.GetTempPath(), "game1-save-tests", Guid.NewGuid().ToString("N"));
        _savePath = Path.Combine(_directory, "save.json");
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_directory)) Directory.Delete(_directory, true);
    }

    [Test]
    public void SaveAtomic_RoundTripsVersionSettingsUnlocksAndLastRun()
    {
        SaveData source = SaveData.CreateDefault();
        source.Settings.MasterVolume = 0.75f;
        source.Settings.MusicVolume = 0.65f;
        source.Settings.AmbienceVolume = 0.55f;
        source.Settings.UiVolume = 0.85f;
        source.UnlockedIds.Add("boss_roadblock_commander");
        source.LastRun = new LastRunSummary { Seed = 42, Result = "victory" };
        SaveService service = new(_savePath);

        service.SaveAtomic(source);
        SaveData loaded = service.LoadOrDefault();

        Assert.Multiple(() =>
        {
            Assert.That(SaveData.CurrentSchemaVersion, Is.EqualTo(2));
            Assert.That(loaded.SchemaVersion, Is.EqualTo(SaveData.CurrentSchemaVersion));
            Assert.That(loaded.Settings.MasterVolume, Is.EqualTo(0.75f));
            Assert.That(loaded.Settings.MusicVolume, Is.EqualTo(0.65f));
            Assert.That(loaded.Settings.AmbienceVolume, Is.EqualTo(0.55f));
            Assert.That(loaded.Settings.UiVolume, Is.EqualTo(0.85f));
            Assert.That(loaded.UnlockedIds, Is.EqualTo(source.UnlockedIds));
            Assert.That(loaded.LastRun.Seed, Is.EqualTo(42));
            Assert.That(loaded.LastRun.Result, Is.EqualTo("victory"));
            Assert.That(typeof(LastRunSummary).GetProperty("RelayIntegrity"), Is.Null);
        });
    }

    [Test]
    public void LoadOrDefault_MigratesSchemaOneAndIgnoresLegacyRelayIntegrity()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(_savePath, """
        {
          "schema_version": 1,
          "settings": { "master_volume": 0.6, "sfx_volume": 0.4 },
          "unlocked_ids": ["legacy_unlock"],
          "last_run": {
            "seed": 77,
            "relay_integrity": 12,
            "elapsed_seconds": 9.5,
            "result": "failed"
          }
        }
        """);

        SaveData loaded = new SaveService(_savePath, emitWarnings: false).LoadOrDefault();
        PropertyInfo? coreId = typeof(LastRunSummary).GetProperty("CoreId");
        PropertyInfo? arenaIndex = typeof(LastRunSummary).GetProperty("ArenaIndex");
        PropertyInfo? waveIndex = typeof(LastRunSummary).GetProperty("WaveIndex");
        PropertyInfo? level = typeof(LastRunSummary).GetProperty("Level");

        Assert.Multiple(() =>
        {
            Assert.That(loaded.SchemaVersion, Is.EqualTo(2));
            Assert.That(loaded.Settings.MasterVolume, Is.EqualTo(0.6f));
            Assert.That(loaded.Settings.SfxVolume, Is.EqualTo(0.4f));
            Assert.That(loaded.Settings.MusicVolume, Is.EqualTo(0.82f));
            Assert.That(loaded.Settings.AmbienceVolume, Is.EqualTo(0.68f));
            Assert.That(loaded.Settings.UiVolume, Is.EqualTo(0.9f));
            Assert.That(loaded.UnlockedIds, Is.EqualTo(new[] { "legacy_unlock" }));
            Assert.That(loaded.LastRun.Seed, Is.EqualTo(77));
            Assert.That(coreId?.GetValue(loaded.LastRun), Is.EqualTo(string.Empty));
            Assert.That(arenaIndex?.GetValue(loaded.LastRun), Is.EqualTo(0));
            Assert.That(waveIndex?.GetValue(loaded.LastRun), Is.EqualTo(0));
            Assert.That(level?.GetValue(loaded.LastRun), Is.EqualTo(1));
            Assert.That(typeof(LastRunSummary).GetProperty("RelayIntegrity"), Is.Null);
        });
    }

    [Test]
    public void Validate_RejectsUnsupportedSchemaAndInvalidVolume()
    {
        SaveData unsupported = SaveData.CreateDefault();
        unsupported.SchemaVersion = SaveData.CurrentSchemaVersion + 1;
        SaveData invalidVolume = SaveData.CreateDefault();
        invalidVolume.Settings.SfxVolume = 1.1f;
        SaveData invalidMusic = SaveData.CreateDefault();
        invalidMusic.Settings.MusicVolume = -0.1f;

        Assert.That(() => unsupported.Validate(), Throws.TypeOf<InvalidDataException>());
        Assert.That(() => invalidVolume.Validate(), Throws.TypeOf<InvalidDataException>());
        Assert.That(() => invalidMusic.Validate(), Throws.TypeOf<InvalidDataException>());
    }
}
