using System.IO;
using NUnit.Framework;

namespace Game1.Tests.Enemies;

public sealed class CombatRoomGroupTests
{
    [Test]
    public void CombatRoom_DeclaresPlayerAndRelayGroupsInNodeHeaders()
    {
        string scenePath = FindRepositoryFile("game1", "scenes", "rooms", "mvp_combat_room.tscn");
        string scene = File.ReadAllText(scenePath);

        Assert.Multiple(() =>
        {
            Assert.That(scene, Does.Contain("groups=[\"player\"] instance="));
            Assert.That(scene, Does.Contain("type=\"StaticBody2D\" parent=\".\" groups=[\"relay\"]"));
            Assert.That(scene, Does.Not.Contain("\ngroups = [\"player\"]"));
            Assert.That(scene, Does.Not.Contain("\ngroups = [\"relay\"]"));
        });
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
