using System;
using System.IO;
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
        source.UnlockedIds.Add("boss_roadblock_commander");
        source.LastRun = new LastRunSummary { Seed = 42, RelayIntegrity = 68, Result = "victory" };
        SaveService service = new(_savePath);

        service.SaveAtomic(source);
        SaveData loaded = service.LoadOrDefault();

        Assert.Multiple(() =>
        {
            Assert.That(loaded.SchemaVersion, Is.EqualTo(SaveData.CurrentSchemaVersion));
            Assert.That(loaded.Settings.MasterVolume, Is.EqualTo(0.75f));
            Assert.That(loaded.UnlockedIds, Is.EqualTo(source.UnlockedIds));
            Assert.That(loaded.LastRun.Seed, Is.EqualTo(42));
            Assert.That(loaded.LastRun.Result, Is.EqualTo("victory"));
        });
    }

    [Test]
    public void Validate_RejectsUnsupportedSchemaAndInvalidVolume()
    {
        SaveData unsupported = SaveData.CreateDefault();
        unsupported.SchemaVersion = SaveData.CurrentSchemaVersion + 1;
        SaveData invalidVolume = SaveData.CreateDefault();
        invalidVolume.Settings.SfxVolume = 1.1f;

        Assert.That(() => unsupported.Validate(), Throws.TypeOf<InvalidDataException>());
        Assert.That(() => invalidVolume.Validate(), Throws.TypeOf<InvalidDataException>());
    }
}
