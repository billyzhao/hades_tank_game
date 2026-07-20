using System.IO;
using NUnit.Framework;

namespace Game1.Tests.Enemies;

public sealed class CombatRoomGroupTests
{
    [Test]
    public void ProductionRooms_DeclarePlayerGroupWithoutRelayGroupOrNode()
    {
        string[] scenes =
        {
            "mvp_combat_room.tscn",
            "industrial_flank_room.tscn",
            "mvp_boss_room.tscn"
        };

        foreach (string filename in scenes)
        {
            string scenePath = FindRepositoryFile("game1", "scenes", "rooms", filename);
            string scene = File.ReadAllText(scenePath);
            Assert.Multiple(() =>
            {
                Assert.That(scene, Does.Contain("groups=[\"player\"] instance="), $"{filename} 必须注册玩家组。");
                Assert.That(scene, Does.Not.Contain("groups=[\"relay\"]"), $"{filename} 不得保留中继组。");
                Assert.That(scene, Does.Not.Contain("name=\"RelayStation\""), $"{filename} 不得保留中继节点。");
            });
        }
    }

    private static string FindRepositoryFile(params string[] parts)
    {
        DirectoryInfo? directory = new(TestContext.CurrentContext.TestDirectory);
        while (directory is not null)
        {
            string candidate = Path.Combine([directory.FullName, .. parts]);
            if (File.Exists(candidate)) return candidate;
            directory = directory.Parent;
        }

        throw new FileNotFoundException("无法从测试目录定位战斗房间场景。");
    }
}
