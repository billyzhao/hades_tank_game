namespace Game1;

public sealed class RunState
{
    public required int Seed { get; init; }

    public int RelayIntegrity { get; set; }

    public int PlayerArmor { get; set; }

    public int RebootsRemaining { get; set; }

    public int RoomIndex { get; set; }

    public static RunState CreateNew(
        int seed,
        int relayIntegrity = 100,
        int armor = 100,
        int reboots = 1)
    {
        return new RunState
        {
            Seed = seed,
            RelayIntegrity = relayIntegrity,
            PlayerArmor = armor,
            RebootsRemaining = reboots,
            RoomIndex = 0
        };
    }

    /// <summary>扣除中继站耐久；返回值表示中继站是否仍可维持本局。</summary>
    public bool ApplyRelayDamage(int amount)
    {
        RelayIntegrity = System.Math.Max(0, RelayIntegrity - System.Math.Max(0, amount));
        return RelayIntegrity > 0;
    }

    /// <summary>仅在仍有次数时消耗一次战场重启，绝不让计数变为负数。</summary>
    public bool TryConsumeReboot()
    {
        if (RebootsRemaining <= 0) return false;
        RebootsRemaining--;
        return true;
    }
}
